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
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class PlayerController : NetworkBehaviour , IPlayerController
    {
        [SerializeField] private ScriptableStats _stats; // Reference to the player's stats.
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
        public event Action<bool> Attacked;

        #endregion

        private float _time; // A timer to track the game time, used for coyote time and jump buffering.
        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<CapsuleCollider2D>();

            _cachedQueryStartInColliders = Physics2D.queriesStartInColliders; // Cache the original value of queriesStartInColliders to reset it after collision checks
        }

        private void Update()
        {
            _time += Time.deltaTime; // Save the time in deltatime (for FPS balancing)
            //if (!IsOwner) return;
            GatherInput(); // Store the player's input for the current frame
        }

        private void FixedUpdate()
        {
            //if(!IsOwner) return;
            CheckCollisions(); // Check for collisions and update grounded status

            HandleJump(); // Handle jumping logic, including coyote time and jump buffering
            HandleDirection(); // Handle horizontal movement based on player input
            HandleGravity(); // Handle gravity and falling logic, including variable jump height
            HandleDash(); // Handle dashing logic, including dash cooldowns
            HandleAttack(); // Handle attacking logic, including attack cooldowns and damage

            ApplyMovement(); // Apply the calculated velocity to the Rigidbody2D component
        }
        #region Inputs

        // Gather the player's input for the current frame and store it in the _frameInput struct. This method also updates the facing direction based on horizontal input and sets bools to check if the player has a jump, dash, or attack to consume.
        private void GatherInput()
        {
            _frameInput = new FrameInput
            {
                JumpDown = Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow),
                JumpHeld = Input.GetButton("Jump") || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow),
                Move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")),
                DashDown = Input.GetKeyDown(KeyCode.LeftShift),
                AttackDown = Input.GetKeyDown(KeyCode.J)
            };

            if (_frameInput.JumpDown)
            {
                _jumpToConsume = true; // Bool to check if the player has a jump to use
                _timeJumpWasPressed = _time; // Save the time when the jump button was pressed for jump buffering
            }

            if (_frameInput.DashDown)
                _dashToConsume = true;
            
            if (_frameInput.AttackDown) 
                _attackToConsume = true;

            //if _frameInput.Move.x is bigger than 0(moving right) then _facingRight is true,
            //but still check if _frameInput.Move.x is lower than 0(moving left), if that condition is true, then _facingRight is false
            // and if the condition is false then _facingRight is true(this works just like an if/elseif)
            _facingRight = _frameInput.Move.x > 0 ? true : _frameInput.Move.x < 0 ? false : _facingRight; // Update the facing direction based on horizontal input
        }
        #endregion

        #region Collisions

        private float _frameLeftGrounded = float.MinValue; // The time when the player left the ground, used for coyote time calculations
        private bool _grounded; // Bool to check if the player is currently grounded or not

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
        private void OnCollisionEnter(Collision collision)
        {
            if(collision.collider.tag == "KillZone")
            {
                // Handle player death or respawn
                Destroy(gameObject);
            }
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
            _frameVelocity.y = _stats.JumpPower;
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
                _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, _frameInput.Move.x * _stats.MaxSpeed, _stats.Acceleration * Time.fixedDeltaTime);
            }
        }

        #endregion

        #region Gravity

        private void HandleGravity()
        {
            // If the player is grounded and their vertical velocity is less than or equal to 0, set their vertical velocity to the grounding force.
            if (_grounded && _frameVelocity.y <= 0f)
            {
                _frameVelocity.y = _stats.GroundingForce;
            }
            else
            {
                // If the player is in the air, apply gravity to their vertical velocity. If they ended their jump early and are still moving upwards, apply the jump end early gravity modifier to make them fall faster.
                var inAirGravity = _stats.FallAcceleration;
                if (_endedJumpEarly && _frameVelocity.y > 0) inAirGravity *= _stats.JumpEndEarlyGravityModifier;
                _frameVelocity.y = Mathf.MoveTowards(_frameVelocity.y, -_stats.MaxFallSpeed, inAirGravity * Time.fixedDeltaTime);
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

            if (_frameInput.DashDown && _dashTimer >= _stats.DashInterval)
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
            _frameVelocity.x += _facingRight ? _stats.DashPower : -_stats.DashPower;
            Dashed?.Invoke(true);
        }
        #endregion

        #region Attack

        private bool _attackToConsume; // Similar to dashToConsume, this bool checks if the player has an attack to use
        private float _attackTimer = 0f; // Timer to track the time since the last attack, used for attack cooldowns
        
        // Same logic as handleDash, but for the player's attacks
        private void HandleAttack()
        {
            _attackTimer += Time.fixedDeltaTime;
            if (!_attackToConsume) 
                return;

            if (_frameInput.AttackDown && _attackTimer >= _stats.AttackSpeed)
            {
                _attackTimer = 0f;
                _attackToConsume = true;
                Attack();
            }
            _attackToConsume = false;
            Attacked?.Invoke(false); // Invoke the Attacked event with false to indicate that the player is not currently attacking
        }
        // Invoke the Attacked event to notify any subscribers that the player has attacked.
        private void Attack()
        {
            // Implement attack logic here, such as detecting enemies in range and applying damage
            Attacked?.Invoke(true);
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
    public struct FrameInput
    {
        public bool JumpDown;
        public bool JumpHeld;
        public Vector2 Move;
        public bool DashDown;
        public bool AttackDown;
    }

    // Interface to define the player's input and events for grounded status, jumping, dashing, and attacking. This allows other scripts to subscribe to these events and access the player's input without needing a direct reference to the PlayerController component.
    public interface IPlayerController
    {
        public event Action<bool, float> GroundedChanged;

        public event Action Jumped;

        public event Action<bool> Dashed;

        public event Action<bool> Attacked;
        public Vector2 FrameInput { get; }
    }
}