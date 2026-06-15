using System;
using Unity.Netcode;
using UnityEngine;

namespace TarodevController
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class PlayerController : NetworkBehaviour, IPlayerController
    {
        [SerializeField] private ScriptableStats _stats;
        [SerializeField] private PlayerStats _playerStats;
        private Rigidbody2D _rb;
        private CapsuleCollider2D _col;
        private FrameInput _frameInput;
        private Vector2 _frameVelocity;
        private bool _cachedQueryStartInColliders, _facingRight;

        public Vector2 FrameInput => _frameInput.Move;
        public event Action<bool, float> GroundedChanged;
        public event Action Jumped;
        public event Action<bool> Dashed;
        public event Action<bool, bool> Attacked;

        private float _time;
        private bool _jumpToConsume;
        private bool _bufferedJumpUsable;
        private bool _endedJumpEarly;
        private bool _coyoteUsable;
        private float _timeJumpWasPressed;
        private bool _dashToConsume;
        private float _dashTimer = 0f;
        private bool _attackToConsume;
        private float _attackTimer = 0f;
        private bool _heavyAttackToConsume;
        private float _heavyAttackTimer = 0f;

        private float _frameLeftGrounded = float.MinValue;
        private bool _grounded;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<CapsuleCollider2D>();
            _cachedQueryStartInColliders = Physics2D.queriesStartInColliders;
        }

        private void Update()
        {
            _time += Time.deltaTime;
            if (!IsOwner||!IsHost) return;

            // Gather local input
            var input = new FrameInput
            {
                JumpDown = Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow),
                JumpHeld = Input.GetButton("Jump") || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow),
                Move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")),
                DashDown = Input.GetKeyDown(KeyCode.LeftShift),
                AttackDown = Input.GetKeyDown(KeyCode.J),
                HeavyAttackDown = Input.GetKeyDown(KeyCode.K)
            };

            UpdateInputServerRpc(input);
        }

        [ServerRpc]
        private void UpdateInputServerRpc(FrameInput input)
        {
            _frameInput = input;

            if (_frameInput.JumpDown)
            {
                _jumpToConsume = true;
                _timeJumpWasPressed = _time;
            }

            if (_frameInput.DashDown)
                _dashToConsume = true;

            if (_frameInput.AttackDown)
                _attackToConsume = true;

            if (_frameInput.HeavyAttackDown)
                _heavyAttackToConsume = true;

            _facingRight = _frameInput.Move.x > 0 ? true : (_frameInput.Move.x < 0 ? false : _facingRight);
        }

        private void FixedUpdate()
        {
            if (!IsServer||!IsHost) return;

            CheckCollisions();
            HandleJump();
            HandleDirection();
            HandleGravity();
            HandleDash();

            if (_attackToConsume || _heavyAttackToConsume)
                HandleAttack();

            ApplyMovement();
        }

        private void CheckCollisions()
        {
            Physics2D.queriesStartInColliders = false;

            bool groundHit = Physics2D.CapsuleCast(_col.bounds.center, _col.size, _col.direction, 0, Vector2.down, _stats.GrounderDistance, ~_stats.PlayerLayer);
            bool ceilingHit = Physics2D.CapsuleCast(_col.bounds.center, _col.size, _col.direction, 0, Vector2.up, _stats.GrounderDistance, ~_stats.PlayerLayer);

            if (ceilingHit)
            {
                _frameVelocity.y = Mathf.Min(0, _frameVelocity.y);
            }

            if (!_grounded && groundHit)
            {
                _grounded = true;
                _coyoteUsable = true;
                _bufferedJumpUsable = true;
                _endedJumpEarly = false;
                NotifyGroundedChangedClientRpc(true, Mathf.Abs(_frameVelocity.y));
            }
            else if (_grounded && !groundHit)
            {
                _grounded = false;
                _frameLeftGrounded = _time;
                NotifyGroundedChangedClientRpc(false, 0);
            }
            Physics2D.queriesStartInColliders = _cachedQueryStartInColliders;
        }

        [ClientRpc]
        private void NotifyGroundedChangedClientRpc(bool grounded, float impact) => GroundedChanged?.Invoke(grounded, impact);

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!IsServer) return;

            if (collision.collider.CompareTag("KillZone"))
            {
                var health = GetComponent<PlayerHealth>();
                if (health != null) health.FallDeath();
            }
        }

        private bool HasBufferedJump => _bufferedJumpUsable && _time < _timeJumpWasPressed + _stats.JumpBuffer;
        private bool CanUseCoyote => _coyoteUsable && !_grounded && _time < _frameLeftGrounded + _stats.CoyoteTime;

        private void HandleJump()
        {
            if (!_endedJumpEarly && !_grounded && !_frameInput.JumpHeld && _frameVelocity.y > 0) _endedJumpEarly = true;

            if (!_jumpToConsume && !HasBufferedJump) return;

            if (_grounded || CanUseCoyote) Jump();

            _jumpToConsume = false;
        }

        private void Jump()
        {
            _endedJumpEarly = false;
            _timeJumpWasPressed = 0;
            _bufferedJumpUsable = false;
            _coyoteUsable = false;
            _frameVelocity.y = _playerStats != null ? _playerStats.JumpHeight : _stats.JumpPower;
            NotifyJumpedClientRpc();
        }

        [ClientRpc]
        private void NotifyJumpedClientRpc() => Jumped?.Invoke();

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
            if (_grounded && _frameVelocity.y <= 0f)
            {
                _frameVelocity.y = _stats.GroundingForce;
            }
            else
            {
                float gravityScale = _playerStats != null ? Mathf.Abs(_playerStats.Gravity) / 30f : 1f;
                var inAirGravity = _stats.FallAcceleration * gravityScale;
                if (_endedJumpEarly && _frameVelocity.y > 0) inAirGravity *= _stats.JumpEndEarlyGravityModifier;
                _frameVelocity.y = Mathf.MoveTowards(_frameVelocity.y, -_stats.MaxFallSpeed * gravityScale, inAirGravity * Time.fixedDeltaTime);
            }
        }

        private void HandleDash()
        {
            _dashTimer += Time.fixedDeltaTime;
            if (!_dashToConsume)
            {
                return;
            }

            float cooldown = _playerStats != null ? _playerStats.DashCooldown : 1f;
            if (_dashTimer >= cooldown)
            {
                _dashTimer = 0f;
                Dash();
            }
            _dashToConsume = false;
        }

        private void Dash()
        {
            float dashForce = _stats.DashPower;
            if (_playerStats != null)
                dashForce *= (_playerStats.Knockback / 5f);
            _frameVelocity.x += _facingRight ? dashForce : -dashForce;
            NotifyDashedClientRpc(true);
        }

        [ClientRpc]
        private void NotifyDashedClientRpc(bool dashing) => Dashed?.Invoke(dashing);

        private void HandleAttack()
        {
            _attackTimer += Time.fixedDeltaTime;
            _heavyAttackTimer += Time.fixedDeltaTime;

            float attackInterval = _playerStats != null ? _playerStats.AttackSpeed : _stats.AttackSpeed;

            if (_attackToConsume && _attackTimer >= attackInterval)
            {
                _attackTimer = 0f;
                _attackToConsume = false;
                LightAttack();
                return;
            }

            if (_heavyAttackToConsume && _heavyAttackTimer >= attackInterval * 2f)
            {
                _heavyAttackTimer = 0f;
                _heavyAttackToConsume = false;
                HeavyAttack();
                return;
            }

            _attackToConsume = false;
            _heavyAttackToConsume = false;
        }

        private void LightAttack()
        {
            NotifyAttackedClientRpc(true, false);
        }

        private void HeavyAttack()
        {
            NotifyAttackedClientRpc(true, true);
        }

        [ClientRpc]
        private void NotifyAttackedClientRpc(bool active, bool heavy) => Attacked?.Invoke(active, heavy);

        private void ApplyMovement() => _rb.linearVelocity = _frameVelocity;
    }

    public struct FrameInput : INetworkSerializable
    {
        public bool JumpDown;
        public bool JumpHeld;
        public Vector2 Move;
        public bool DashDown;
        public bool AttackDown;
        public bool HeavyAttackDown;

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