using System;
using Unity.Netcode;
using UnityEngine;

namespace TarodevController
{
    /// <summary>
    /// Hey!
    /// Tarodev here. I built this controller as there was a severe lack of quality & free 2D controllers out there.
    /// I have a premium version on Patreon, which has every feature you'd expect from a polished controller. Link: https://www.patreon.com/tarodev
    /// You can play and compete for best times here: https://tarodev.itch.io/extended-ultimate-2d-controller
    /// If you hve any questions or would like to brag about your score, come to discord: https://discord.gg/tarodev
    ///
    /// ── NOTA SOBRE A ARQUITECTURA (Server Authoritative) ──────────────────────────
    /// O Client (dono) só faz UMA coisa em Update(): lê o teclado local e envia o
    /// resultado ao servidor via SubmitInputServerRpc. Nada de física acontece no Client.
    ///
    /// O Servidor é o único que corre CheckCollisions/HandleJump/HandleDirection/etc,
    /// porque só ele tem o _frameInput correcto (recebido via RPC) e só ele deve mover
    /// o Rigidbody2D — o NetworkTransform (Server Authoritative) propaga essa posição
    /// para todos os clientes, incluindo o dono.
    ///
    /// IMPORTANTE: nunca colocar [ServerRpc] em métodos chamados a cada frame que lêem
    /// Input.* — Input.GetAxisRaw só lê o teclado da máquina onde o código corre. Se o
    /// método com [ServerRpc] correr no servidor, está a ler o teclado do SERVIDOR, não
    /// o do cliente. Por isso o input tem de ser lido no Client e apenas os VALORES
    /// (já lidos) são enviados via RPC — nunca a leitura em si.
    /// ────────────────────────────────────────────────────────────────────────────
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class PlayerController : NetworkBehaviour, IPlayerController
    {
        [SerializeField] private ScriptableStats _stats; // Reference to the player's stats.
        [SerializeField] private PlayerStats _playerStats; // Reference to the PlayerStats component.
        private Rigidbody2D _rb; // Player's Rigidbody2D component.
        private CapsuleCollider2D _col; // Player's CapsuleCollider2D component.
        private FrameInput _frameInput; // Struct to hold the player's input for the current frame.
        private Vector2 _frameVelocity; // The velocity that will be applied to the player at the end of the frame
        private bool _cachedQueryStartInColliders, _facingRight;

        #region Interface

        // Implementation of the IPlayerController interface, allowing other scripts to access the player's input and subscribe to events without needing a direct reference to the PlayerController component.
        public Vector2 FrameInput => _frameInput.Move;
        public event Action<bool, float> GroundedChanged;
        public event Action Jumped;
        public event Action<bool> Dashed;
        public event Action<bool, bool> Attacked;

        #endregion

        private float _time; // A timer to track the game time, used for coyote time and jump buffering.
        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<CapsuleCollider2D>();

            _cachedQueryStartInColliders = Physics2D.queriesStartInColliders; // Cache the original value of queriesStartInColliders to reset it after collision checks
        }

        // ── CLIENT (dono): só lê input local e envia ao servidor ──────────────────
        private void Update()
        {
            _time += Time.deltaTime; // Save the time in deltatime (for FPS balancing)
            if (!IsOwner) return;

            var input = ReadLocalInput(); // Lê o teclado local
            SubmitInputServerRpc(input);  // Envia ao servidor para processamento
        }

        // ── SERVIDOR: única instância que corre física e lógica de jogo ────────────
        private void FixedUpdate()
        {
            if (!IsServer) return;

            CheckCollisions();
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

            ApplyMovement();
        }

        #region Inputs

        // Lê o input local (apenas no Client dono). Não toca em nenhum estado do jogo —
        // só devolve a leitura crua do teclado para ser enviada ao servidor.
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

        // Recebe o input do Client dono e actualiza o estado no SERVIDOR.
        [ServerRpc]
        private void SubmitInputServerRpc(FrameInput input, ServerRpcParams rpcParams = default)
        {
            _frameInput = input;

            if (_frameInput.JumpDown)
            {
                _jumpToConsume = true; // Bool to check if the player has a jump to use
                _timeJumpWasPressed = _time; // Save the time when the jump button was pressed for jump buffering
            }

            if (_frameInput.DashDown)
                _dashToConsume = true;

            if (_frameInput.AttackDown)
            {
                _attackToConsume = true;
                Debug.Log("Light attack input detected");
            }

            if (_frameInput.HeavyAttackDown)
            {
                _heavyAttackToConsume = true;
                Debug.Log("Heavy attack input detected");
            }

            //if _frameInput.Move.x is bigger than 0(moving right) then _facingRight is true,
            //but still check if _frameInput.Move.x is lower than 0(moving left), if that condition is true, then _facingRight is false
            // and if the condition is false then _facingRight is true(this works just like an if/elseif)
            _facingRight = _frameInput.Move.x > 0 ? true : _frameInput.Move.x < 0 ? false : _facingRight; // Update the facing direction based on horizontal input
        }
        #endregion

        #region Collisions

        private float _frameLeftGrounded = float.MinValue; // The time when the player left the ground, used for coyote time calculations
        private bool _grounded; // Bool to check if the player is currently grounded or not

        // Corre apenas no servidor (chamado por FixedUpdate, que já filtra IsServer)
        private void CheckCollisions()
        {
            Physics2D.queriesStartInColliders = false;

            // Ground and Ceiling-----------------------------------------------------------------------------------------------------------|->/*This operator makes it so that the cast checks for all layers except the player's*/
            bool groundHit = Physics2D.CapsuleCast(_col.bounds.center, _col.size, _col.direction, 0, Vector2.down, _stats.GrounderDistance, ~_stats.PlayerLayer);
            bool ceilingHit = Physics2D.CapsuleCast(_col.bounds.center, _col.size, _col.direction, 0, Vector2.up, _stats.GrounderDistance, ~_stats.PlayerLayer);

            // Hit a Ceiling
            if (ceilingHit)
            {
                // if the player hits a ceiling, send them downwards and cancel any upwards velocity
                _frameVelocity.y = Mathf.Min(0, _frameVelocity.y);
            }

            // Landed on the Ground
            if (!_grounded && groundHit)
            {
                _grounded = true;
                _coyoteUsable = true;
                _bufferedJumpUsable = true;
                _endedJumpEarly = false;

                // 
                GroundedChanged?.Invoke(true, Mathf.Abs(_frameVelocity.y));
            }
            // Left the Ground
            else if (_grounded && !groundHit)
            {
                _grounded = false;
                _frameLeftGrounded = _time;
                GroundedChanged?.Invoke(false, 0);
            }
            Physics2D.queriesStartInColliders = _cachedQueryStartInColliders; // Reset the queriesStartInColliders setting to its original value
        }

        #endregion

        #region Jumping

        private bool _jumpToConsume; // Bool to check if the player has a jump to use, set to true when the jump button is pressed
        private bool _bufferedJumpUsable; // Bool to check if the player has a buffered jump available.
        private bool _endedJumpEarly; // Bool to check if the player ended their jump
        private bool _coyoteUsable; // Bool to check if the player has a coyote jump available, set to true when the player leaves the ground and becomes false when they jump or the coyote time expires
        private float _timeJumpWasPressed; // The time when the jump button was pressed, used for jump buffering calculations

        // A buffered jump allows the player to still jump if they pressed the jump button shortly before landing.
        private bool HasBufferedJump => _bufferedJumpUsable && _time < _timeJumpWasPressed + _stats.JumpBuffer;

        // A coyote jump allows the player to still jump if they pressed the jump button shortly after leaving a ledge.
        private bool CanUseCoyote => _coyoteUsable && !_grounded && _time < _frameLeftGrounded + _stats.CoyoteTime;

        // Check if the player has a jump to consume and if they are either grounded or can use coyote time. If the player is in the air and releases the jump button while still moving upwards, they will end their jump early, which applies extra gravity to make them fall faster.
        private void HandleJump()
        {
            if (!_endedJumpEarly && !_grounded && !_frameInput.JumpHeld && _rb.linearVelocity.y > 0) _endedJumpEarly = true;

            if (!_jumpToConsume && !HasBufferedJump) return;

            if (_grounded || CanUseCoyote) Jump();

            _jumpToConsume = false;
        }
        // When the player jumps, reset all jump-related bools and timers, apply an immediate vertical velocity based on the jump power stat, and invoke the Jumped event to notify any subscribers that the player has jumped.
        private void Jump()
        {
            _endedJumpEarly = false;
            _timeJumpWasPressed = 0;
            _bufferedJumpUsable = false;
            _coyoteUsable = false;
            // JumpHeight vem do PlayerStats para respeitar buffs/debuffs de cartas
            _frameVelocity.y = _playerStats != null ? _playerStats.JumpHeight : _stats.JumpPower;
            Jumped?.Invoke();
        }

        #endregion

        #region Horizontal

        // If there is no horizontal input, apply deceleration to slow the player down. If there is horizontal input, apply acceleration towards the target speed based on the player's input and max speed stat.
        private void HandleDirection()
        {
            if (_frameInput.Move.x == 0)
            {
                var deceleration = _grounded ? _stats.GroundDeceleration : _stats.AirDeceleration;
                _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, 0, deceleration * Time.fixedDeltaTime);
            }
            else
            {
                _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, _frameInput.Move.x * _playerStats.MoveSpeed, _stats.Acceleration * Time.fixedDeltaTime);
            }
        }

        #endregion

        #region Gravity

        private void HandleGravity()
        {
            if (_grounded && _frameVelocity.y <= 0f)
            {
                _frameVelocity.y = _stats.GroundingForce;
            }
            else
            {
                // FallAcceleration e MaxFallSpeed escalados pelo Gravity do PlayerStats
                // Gravity base = -30 no PlayerStats; valores mais negativos = mais pesado
                float gravityScale = _playerStats != null ? Mathf.Abs(_playerStats.Gravity) / 30f : 1f;
                var inAirGravity = _stats.FallAcceleration * gravityScale;
                if (_endedJumpEarly && _frameVelocity.y > 0) inAirGravity *= _stats.JumpEndEarlyGravityModifier;
                _frameVelocity.y = Mathf.MoveTowards(_frameVelocity.y, -_stats.MaxFallSpeed * gravityScale, inAirGravity * Time.fixedDeltaTime);
            }
        }

        #endregion

        #region Dash
        // New dash idea -> held dash. If the player taps the dash button, they will dash a short distance. If they hold the dash button, they will dash further.

        private bool _dashToConsume; // Bool to check if the player has a dash to use 
        private float _dashTimer = 0f; // Timer to track the time since the last dash, used for dash cooldowns

        // Check if the player has a dash to consume and if the dash button is pressed. If the dash button is pressed and the dash timer is greater than or equal to the dash interval, the player will dash and the dash timer will be reset. The dashToConsume bool is then set to false until the next time the player presses the dash button.
        private void HandleDash()
        {
            _dashTimer += Time.fixedDeltaTime;
            if (!_dashToConsume)
            {
                Dashed?.Invoke(false); // Invoke the Dashed event with false to indicate that the player is not currently dashing
                return;
            }

            if (_frameInput.DashDown && _dashTimer >= _playerStats.DashCooldown)
            {
                _dashTimer = 0f;
                _dashToConsume = true;
                Dash();
            }
            _dashToConsume = false;
        }

        // Apply an immediate velocity in the direction the player is facing based on the dash power stat. The Dashed event is then invoked to notify any subscribers that the player has dashed.
        private void Dash()
        {
            // DashPower base do ScriptableStats, escalado pelo Knockback do PlayerStats
            float dashForce = _stats.DashPower;
            if (_playerStats != null)
                dashForce *= (_playerStats.Knockback / 5f); // 5f = valor base de Knockback no PlayerStats
            _frameVelocity.x += _facingRight ? dashForce : -dashForce;
            Dashed?.Invoke(true);
        }
        #endregion

        #region Attack

        private bool _attackToConsume; // Similar to dashToConsume, this bool checks if the player has an attack to use
        private float _attackTimer = 0f; // Timer to track the time since the last attack, used for attack cooldowns
        private bool _heavyAttackToConsume; // Bool to check if the player is performing a heavy attack
        private float _heavyAttackTimer = 0f; // Timer to track the time since the last heavy attack, used for heavy attack cooldowns

        // Same logic as handleDash, but for the player's attacks
        private void HandleAttack()
        {
            Debug.Log("HandleAttack called");

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

            if (!_attackToConsume) _attackToConsume = false;
            if (!_heavyAttackToConsume) _heavyAttackToConsume = false;

            Attacked?.Invoke(false, false);
        }
        // Invoke the Attacked event to notify any subscribers that the player has attacked.
        private void LightAttack()
        {
            Attacked?.Invoke(true, false);
        }

        private void HeavyAttack()
        {
            Attacked?.Invoke(true, true);
        }
        #endregion

        private void ApplyMovement() => _rb.linearVelocity = _frameVelocity; // Apply the calculated velocity to the Rigidbody2D component at the end of the frame

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_stats == null)
                Debug.LogWarning("Please assign a ScriptableStats asset to the Player Controller's Stats slot", this);
        }
#endif
    }

    // Struct to hold the player's input for the current frame, including jump, movement, dash, and attack inputs
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

    // Interface to define the player's input and events for grounded status, jumping, dashing, and attacking. This allows other scripts to subscribe to these events and access the player's input without needing a direct reference to the PlayerController component.
    public interface IPlayerController
    {
        public event Action<bool, float> GroundedChanged;

        public event Action Jumped;

        public event Action<bool> Dashed;

        public event Action<bool, bool> Attacked;
        public Vector2 FrameInput { get; }
    }
}