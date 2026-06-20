using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace TarodevController
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(NetworkAnimator))]
    public class PlayerController : NetworkBehaviour, IPlayerController
    {
        [SerializeField] private ScriptableStats _stats;
        [SerializeField] private PlayerStats _playerStats;
        
        [Header("Audio Settings")]
        [SerializeField] private AudioClip _jumpSound;
        [SerializeField] private AudioClip[] _lightAttackSounds;
        [SerializeField] private AudioClip[] _heavyAttackSounds;
        private AudioSource _audioSource;

        private Rigidbody2D _rb;
        private CapsuleCollider2D _col;
        private NetworkAnimator _networkAnim;
        private FrameInput _frameInput;
        private Vector2 _frameVelocity;
        private bool _cachedQueryStartInColliders, _facingRight;

        public NetworkVariable<bool> NetFacingRight = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<float> NetMoveSpeed = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<bool> NetGrounded = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public bool IsGrounded => IsOwner ? _grounded : NetGrounded.Value;

        public Vector2 FrameInput => _frameInput.Move;
        public event Action<bool, float> GroundedChanged;
        public event Action Jumped;
        public event Action<bool> Dashed;
        public event Action<bool, bool> Attacked;

        private float _time;
        private bool _grounded;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<CapsuleCollider2D>();
            _networkAnim = GetComponent<NetworkAnimator>();
            _cachedQueryStartInColliders = Physics2D.queriesStartInColliders;
            _audioSource = GetComponentInChildren<AudioSource>();
        }

        private void Update()
        {
            _time += Time.deltaTime;
            if (!IsOwner) return;

            var input = ReadLocalInput();
            SubmitInputServerRpc(input);
        }

        private void FixedUpdate()
        {
            if (IsServer || IsOwner)
            {
                CheckCollisions();
            }

            if (!IsServer) return;

            HandleJump();
            HandleDirection();
            HandleGravity();
            HandleDash();

            _attackTimer += Time.fixedDeltaTime;
            _heavyAttackTimer += Time.fixedDeltaTime;

            float attackInterval = _playerStats != null ? _playerStats.AttackSpeed : _stats.AttackSpeed;

            if (_attackToConsume && _attackTimer >= attackInterval)
                HandleAttack();
            else if (_heavyAttackToConsume && _heavyAttackTimer >= attackInterval * 2f)
                HandleAttack();

            NetMoveSpeed.Value = Mathf.Abs(_frameInput.Move.x);
            ApplyMovement();
        }

        private FrameInput ReadLocalInput()
        {
            return new FrameInput
            {
                JumpDown = Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow),
                JumpHeld = Input.GetButton("Jump") || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow),
                Move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")),
                DashDown = Input.GetKeyDown(KeyCode.LeftShift),
                AttackDown = Input.GetKeyDown(KeyCode.J),
                HeavyAttackDown = Input.GetKeyDown(KeyCode.K)
            };
        }

        [ServerRpc]
        private void SubmitInputServerRpc(FrameInput input, ServerRpcParams rpcParams = default)
        {
            _frameInput = input;

            if (_frameInput.JumpDown)
            {
                _jumpToConsume = true;
                _timeJumpWasPressed = _time;
            }

            if (_frameInput.DashDown)
                _dashToConsume = true;

            if (_frameInput.AttackDown) _attackToConsume = true;
            if (_frameInput.HeavyAttackDown) _heavyAttackToConsume = true;

            _facingRight = _frameInput.Move.x > 0 ? true : _frameInput.Move.x < 0 ? false : _facingRight;
            if (NetFacingRight.Value != _facingRight) NetFacingRight.Value = _facingRight;
        }

        private void CheckCollisions()
        {
            Physics2D.queriesStartInColliders = false;
            bool groundHit = Physics2D.CapsuleCast(_col.bounds.center, _col.size, _col.direction, 0, Vector2.down, _stats.GrounderDistance, ~_stats.PlayerLayer);
            
            if (!_grounded && groundHit)
            {
                _grounded = true;
                NetGrounded.Value = true;
                _coyoteUsable = true;
                _bufferedJumpUsable = true;
                _endedJumpEarly = false;
                GroundedChanged?.Invoke(true, Mathf.Abs(_frameVelocity.y));
            }
            else if (_grounded && !groundHit)
            {
                _grounded = false;
                NetGrounded.Value = false;
                _frameLeftGrounded = _time;
                GroundedChanged?.Invoke(false, 0);
            }
            Physics2D.queriesStartInColliders = _cachedQueryStartInColliders;
        }

        private bool _jumpToConsume, _bufferedJumpUsable, _endedJumpEarly, _coyoteUsable;
        private float _timeJumpWasPressed, _frameLeftGrounded;

        private void HandleJump()
        {
            if (!_endedJumpEarly && !_grounded && !_frameInput.JumpHeld && _rb.linearVelocity.y > 0) _endedJumpEarly = true;
            if (!_jumpToConsume && !(_bufferedJumpUsable && _time < _timeJumpWasPressed + _stats.JumpBuffer)) return;
            if (_grounded || (_coyoteUsable && !_grounded && _time < _frameLeftGrounded + _stats.CoyoteTime)) Jump();
            _jumpToConsume = false;
        }

        private void Jump()
        {
            _endedJumpEarly = false;
            _timeJumpWasPressed = 0;
            _bufferedJumpUsable = false;
            _coyoteUsable = false;
            _frameVelocity.y = _playerStats != null ? _playerStats.JumpHeight : _stats.JumpPower;
            
            if (_networkAnim != null) _networkAnim.SetTrigger("Jump");

            Jumped?.Invoke();
            PlayJumpSoundClientRpc();
        }

        private void HandleDirection()
        {
            if (_frameInput.Move.x == 0)
            {
                var deceleration = _grounded ? _stats.GroundDeceleration : _stats.AirDeceleration;
                _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, 0, deceleration * Time.fixedDeltaTime);
            }
            else
            {
                float speed = _playerStats != null ? _playerStats.MoveSpeed : 10f;
                _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, _frameInput.Move.x * speed, _stats.Acceleration * Time.fixedDeltaTime);
            }
        }

        private void HandleGravity()
        {
            if (_grounded && _frameVelocity.y <= 0f) _frameVelocity.y = _stats.GroundingForce;
            else
            {
                float gravityScale = _playerStats != null ? Mathf.Abs(_playerStats.Gravity) / 30f : 1f;
                var inAirGravity = _stats.FallAcceleration * gravityScale;
                if (_endedJumpEarly && _frameVelocity.y > 0) inAirGravity *= _stats.JumpEndEarlyGravityModifier;
                _frameVelocity.y = Mathf.MoveTowards(_frameVelocity.y, -_stats.MaxFallSpeed * gravityScale, inAirGravity * Time.fixedDeltaTime);
            }
        }

        private bool _dashToConsume;
        private float _dashTimer = 0f;

        private void HandleDash()
        {
            _dashTimer += Time.fixedDeltaTime;
            if (!_dashToConsume) { Dashed?.Invoke(false); return; }
            if (_frameInput.DashDown && _dashTimer >= (_playerStats != null ? _playerStats.DashCooldown : 0.5f))
            {
                _dashTimer = 0f;
                Dash();
            }
            _dashToConsume = false;
        }

        private void Dash()
        {
            float dashForce = _stats.DashPower;
            if (_playerStats != null) dashForce *= (_playerStats.Knockback / 5f);
            _frameVelocity.x += _facingRight ? dashForce : -dashForce;
            
            if (_networkAnim != null) _networkAnim.SetTrigger("Dash");
            
            Dashed?.Invoke(true);
        }

        private bool _attackToConsume, _heavyAttackToConsume;
        private float _attackTimer = 0f, _heavyAttackTimer = 0f;

        private void HandleAttack()
        {
            float attackInterval = _playerStats != null ? _playerStats.AttackSpeed : _stats.AttackSpeed;
            if (_attackToConsume && _attackTimer >= attackInterval)
            {
                _attackTimer = 0f; _attackToConsume = false;
                if (_networkAnim != null) _networkAnim.SetTrigger("Attack");
                Attacked?.Invoke(true, false);
                PlayAttackSoundClientRpc(false);
            }
            else if (_heavyAttackToConsume && _heavyAttackTimer >= attackInterval * 2f)
            {
                _heavyAttackTimer = 0f; _heavyAttackToConsume = false;
                if (_networkAnim != null) _networkAnim.SetTrigger("Attack");
                Attacked?.Invoke(true, true);
                PlayAttackSoundClientRpc(true);
            }
        }

        [ClientRpc]
        private void PlayJumpSoundClientRpc()
        {
            if (_audioSource != null && _jumpSound != null)
            {
                _audioSource.PlayOneShot(_jumpSound);
            }
        }

        [ClientRpc]
        private void PlayAttackSoundClientRpc(bool isHeavy)
        {
            if (_audioSource == null) return;
            AudioClip[] clips = isHeavy ? _heavyAttackSounds : _lightAttackSounds;
            if (clips != null && clips.Length > 0)
            {
                AudioClip randomClip = clips[UnityEngine.Random.Range(0, clips.Length)];
                _audioSource.PlayOneShot(randomClip);
            }
        }

        private void ApplyMovement() => _rb.linearVelocity = _frameVelocity;
    }

    public struct FrameInput : INetworkSerializable
    {
        public bool JumpDown, JumpHeld, DashDown, AttackDown, HeavyAttackDown;
        public Vector2 Move;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref JumpDown);
            serializer.SerializeValue(ref JumpHeld);
            serializer.SerializeValue(ref Move);
            serializer.SerializeValue(ref DashDown);
            serializer.SerializeValue(ref AttackDown);
            serializer.SerializeValue(ref HeavyAttackDown);
        }
    }

    public interface IPlayerController
    {
        public event Action<bool, float> GroundedChanged;
        public event Action Jumped;
        public event Action<bool> Dashed;
        public event Action<bool, bool> Attacked;
        public Vector2 FrameInput { get; }
    }
}