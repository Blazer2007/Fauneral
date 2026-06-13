using UnityEngine;

namespace TarodevController
{
    public class PlayerAnimator : MonoBehaviour
    {
        [Header("References")] 
        [SerializeField] private Animator _anim;
        [SerializeField] private ScriptableStats _stats;

        [SerializeField] private SpriteRenderer _sprite; // Reference to the player's sprite

        [Header("Settings")] [Range(1f, 3f)]
        [SerializeField] private float _maxIdleSpeed = 2; // How much the idle animation should speed up at max input

        [SerializeField] private float _maxTilt = 5; // Tilt applied to the player's sprite
        [SerializeField] private float _tiltSpeed = 20; // How fast the player gets in and out of a tilt motion

        [Header("Particles")] 
        [SerializeField] private ParticleSystem _jumpParticles;
        [SerializeField] private ParticleSystem _launchParticles;
        [SerializeField] private ParticleSystem _moveParticles;
        [SerializeField] private ParticleSystem _landParticles;
        [SerializeField] private ParticleSystem _dashParticles;
        [SerializeField] private ParticleSystem _attackParticles;

        [Header("Audio Clips")] [SerializeField]
        private AudioClip[] _footsteps; // Audio for the player's footsteps

        private AudioSource _source;
        private IPlayerController _player; // Reference to the player controller to read its state and subscribe to its events
        private bool _grounded;
        private ParticleSystem.MinMaxGradient _currentGradient; // The current color gradient for particles, based on the ground color

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _player = GetComponentInParent<IPlayerController>();
        }

        private void OnEnable()
        {
            // Subscribe to the player's events to react to actions
            _player.Dashed += OnDashed; 
            _player.Jumped += OnJumped;
            _player.GroundedChanged += OnGroundedChanged;
            _player.Attacked += OnAttacked;

            _moveParticles.Play();
        }

        private void OnDisable()
        {
            // Unsubscribe from events to avoid memory leaks
            _player.Dashed -= OnDashed;
            _player.Jumped -= OnJumped;
            _player.GroundedChanged -= OnGroundedChanged;
            _player.Attacked -= OnAttacked;

            // Stop the move particles
            _moveParticles.Stop();
        }

        private void Update()
        {
            if (_player == null) return;

            DetectGroundColor(); // Update ground color every frame to handle moving between different colored grounds

            HandleSpriteFlip(); // Flip the sprite based on movement direction

            HandleIdleSpeed(); // Speed up the idle animation and move particles based on input strength

            HandleCharacterTilt(); // Tilt the character when moving for a more dynamic feel
        }

        private void HandleSpriteFlip()
        {
            if (_player.FrameInput.x != 0) _sprite.flipX = _player.FrameInput.x < 0;
        }

        private void HandleIdleSpeed()
        {
            var inputStrength = Mathf.Abs(_player.FrameInput.x);
            _anim.SetFloat(IdleSpeedKey, Mathf.Lerp(1, _maxIdleSpeed, inputStrength));

            // Scale the move particles based on input strength for a more dynamic effect.
            _moveParticles.transform.localScale = Vector3.MoveTowards(_moveParticles.transform.localScale, Vector3.one * inputStrength, 2 * Time.deltaTime);
        }

        private void HandleCharacterTilt()
        {
            var runningTilt = _grounded ? Quaternion.Euler(0, 0, _maxTilt * _player.FrameInput.x) : Quaternion.identity;
            _anim.transform.up = Vector3.RotateTowards(_anim.transform.up, runningTilt * Vector2.up, _tiltSpeed * Time.deltaTime, 0f);
        }

        // Event handlers for player actions, triggering animations and particles
        private void OnJumped()
        {
            _anim.SetTrigger(JumpKey);
            _anim.ResetTrigger(GroundedKey);


            if (_grounded) // Avoid coyote
            {
                SetColor(_jumpParticles);
                SetColor(_launchParticles);
                _jumpParticles.Play();
            }
        }
        private void OnDashed(bool dashed) // change dash animation variables
        {
            _anim.SetBool(DashKey, dashed);
            _anim.SetBool(CanUseDash, dashed);

            if (dashed)
            {
                _dashParticles.Play();
            }
        }
        private void OnAttacked(bool attacked, bool isHeavy)
        {
            _anim.SetTrigger(AttackKey);
            _anim.SetBool(CanUseAttack, true);
        }

        // handle animations, sounds and particles by checking when the player is grounded |
        //                                                                                 V
        private void OnGroundedChanged(bool grounded, float impact)
        {
            _grounded = grounded;

            if (grounded)
            {
                DetectGroundColor();
                SetColor(_landParticles);

                _anim.SetTrigger(GroundedKey);
                //_source.PlayOneShot(_footsteps[Random.Range(0, _footsteps.Length)]);
                _moveParticles.Play();

                // Scale the land particles based on the impact velocity for a more dynamic effect.
                _landParticles.transform.localScale = Vector3.one * Mathf.InverseLerp(0, 40, impact);
                _landParticles.Play();
            }
            else
            {
                _moveParticles.Stop();
            }
        }
        

        private void DetectGroundColor()
        {
            var hit = Physics2D.Raycast(transform.position, Vector3.down, 2);

            if (!hit || hit.collider.isTrigger || !hit.transform.TryGetComponent(out SpriteRenderer r)) return;
            var color = r.color;
            _currentGradient = new ParticleSystem.MinMaxGradient(color * 0.9f, color * 1.2f);
            SetColor(_moveParticles);
        }

        // Helper method to set the start color of a particle system to the current gradient
        private void SetColor(ParticleSystem ps)
        {
            var main = ps.main;
            main.startColor = _currentGradient;
        }

        /// Animator keys as hashes for optimization
        private static readonly int GroundedKey = Animator.StringToHash("Grounded");
        private static readonly int IdleSpeedKey = Animator.StringToHash("IdleSpeed");
        private static readonly int JumpKey = Animator.StringToHash("Jump");
        private static readonly int DashKey = Animator.StringToHash("Dash");
        private static readonly int CanUseDash = Animator.StringToHash("CanDash");
        private static readonly int AttackKey = Animator.StringToHash("Attack");
        private static readonly int CanUseAttack = Animator.StringToHash("CanAttack");
    }
}