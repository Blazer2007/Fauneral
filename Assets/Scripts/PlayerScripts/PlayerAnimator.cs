using UnityEngine;

namespace TarodevController
{
    /// <summary>
    /// VERY primitive animator example.
    /// This script was also provided by tarodev
    /// </summary>
    public class PlayerAnimator : MonoBehaviour
    {
        [Header("References")] 
        [SerializeField] private Animator _anim; // Reference to the player's animator

        [SerializeField] private SpriteRenderer _sprite; // Reference to the player's sprite

        [Header("Settings")] [Range(1f, 3f)]
        [SerializeField] private float _maxIdleSpeed = 2;

        [SerializeField] private float _maxTilt = 5; // Tilt applied to the player's sprite
        [SerializeField] private float _tiltSpeed = 20; // How fast the player gets in and out of a tilt motion

        [Header("Particles")] 
        [SerializeField] private ParticleSystem _jumpParticles; // particles for the player's jump
        [SerializeField] private ParticleSystem _launchParticles; // particles for the player's launch of the ground
        [SerializeField] private ParticleSystem _moveParticles; // particles for the player's movement
        [SerializeField] private ParticleSystem _landParticles; // particles for when the player lands on the ground
        [SerializeField] private ParticleSystem _dashParticles; // particles for the player's dash

        [Header("Audio Clips")] [SerializeField]
        private AudioClip[] _footsteps; // Audio for the player's footsteps

        private AudioSource _source; // Player's audio source to play its main audios
        private IPlayerController _player; // Player's interface(Requirements for a player script)
        private bool _grounded; // Bool to check if the player is currently on the ground
        private ParticleSystem.MinMaxGradient _currentGradient; // Gradient color for the player's particles color 

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _player = GetComponentInParent<IPlayerController>();
        }

        private void OnEnable()
        {
            // Call actions
            _player.Dashed += OnDashed;
            _player.Jumped += OnJumped;
            _player.GroundedChanged += OnGroundedChanged;
             
            // Play the move particles
            _moveParticles.Play();
        }

        private void OnDisable()
        {
            // Remove actions
            _player.Dashed -= OnDashed;
            _player.Jumped -= OnJumped;
            _player.GroundedChanged -= OnGroundedChanged;

            // Stop the move particles
            _moveParticles.Stop();
        }

        private void Update()
        {
            if (_player == null) return;
            
            // Method to check the ground's color ( to switch the particle's color to that color)
            DetectGroundColor();

            // Method to flip the player's sprite when looking at different directions
            HandleSpriteFlip();

            // Method to handle the player's speed(for animations)
            HandleIdleSpeed();

            // Method to change the player's sprite tilt when moving
            HandleCharacterTilt();
        }

        private void HandleSpriteFlip()
        {
            if (_player.FrameInput.x != 0) _sprite.flipX = _player.FrameInput.x < 0;
        }

        private void HandleIdleSpeed()
        {
            var inputStrength = Mathf.Abs(_player.FrameInput.x);
            _anim.SetFloat(IdleSpeedKey, Mathf.Lerp(1, _maxIdleSpeed, inputStrength));
            _moveParticles.transform.localScale = Vector3.MoveTowards(_moveParticles.transform.localScale, Vector3.one * inputStrength, 2 * Time.deltaTime);
        }

        private void HandleCharacterTilt()
        {
            var runningTilt = _grounded ? Quaternion.Euler(0, 0, _maxTilt * _player.FrameInput.x) : Quaternion.identity;
            _anim.transform.up = Vector3.RotateTowards(_anim.transform.up, runningTilt * Vector2.up, _tiltSpeed * Time.deltaTime, 0f);
        }

        private void OnJumped() // change jump animation variables
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
            _anim.SetTrigger(DashKey);
            _anim.ResetTrigger(CanUseDash);

            if (dashed)
            {
                _dashParticles.Play();
            }
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

        private void SetColor(ParticleSystem ps)
        {
            var main = ps.main;
            main.startColor = _currentGradient;
        }

        private static readonly int GroundedKey = Animator.StringToHash("Grounded");
        private static readonly int IdleSpeedKey = Animator.StringToHash("IdleSpeed");
        private static readonly int JumpKey = Animator.StringToHash("Jump");
        private static readonly int DashKey = Animator.StringToHash("Dash");
        private static readonly int CanUseDash = Animator.StringToHash("CanDash");
    }
}