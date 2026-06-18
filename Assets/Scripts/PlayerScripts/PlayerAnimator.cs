using UnityEngine;

namespace TarodevController
{
    public class PlayerAnimator : MonoBehaviour
    {
        [Header("References")] 
        [SerializeField] private Animator _anim;
        [SerializeField] private SpriteRenderer _sprite;

        [Header("Settings")] 
        [Range(1f, 3f)] [SerializeField] private float _maxIdleSpeed = 2;
        [SerializeField] private float _maxTilt = 5;
        [SerializeField] private float _tiltSpeed = 20;

        [Header("Particles")] 
        [SerializeField] private ParticleSystem _jumpParticles, _launchParticles, _moveParticles, _landParticles, _dashParticles, _attackParticles;

        [Header("Attack Points")]
        [SerializeField] private Transform _lightAttackPoint;
        [SerializeField] private Transform _heavyAttackPoint;

        private PlayerController _player;
        private static readonly int IdleSpeedKey = Animator.StringToHash("IdleSpeed");
        private static readonly int SpeedKey = Animator.StringToHash("Speed");
        private static readonly int IsGroundedKey = Animator.StringToHash("IsGrounded");

        private float _lightX, _heavyX;

        private void Awake()
        {
            _player = GetComponentInParent<PlayerController>();
            if (_lightAttackPoint != null) _lightX = _lightAttackPoint.localPosition.x;
            if (_heavyAttackPoint != null) _heavyX = _heavyAttackPoint.localPosition.x;
        }

        private void Update()
        {
           
            if (_player == null || _anim == null) return;

            // Flip Sprite (Sync via NetworkVariable, but use local for Owner)
            bool facingRight = _player.IsOwner ? (_player.FrameInput.x > 0 ? true : _player.FrameInput.x < 0 ? false : _player.NetFacingRight.Value) : _player.NetFacingRight.Value;
            _sprite.flipX = !facingRight;

            // Flip Attack Points
            if (_lightAttackPoint != null) {
                Vector3 p = _lightAttackPoint.localPosition;
                p.x = facingRight ? _lightX : -_lightX;
                _lightAttackPoint.localPosition = p;
            }
            if (_heavyAttackPoint != null) {
                Vector3 p = _heavyAttackPoint.localPosition;
                p.x = facingRight ? _heavyX : -_heavyX;
                _heavyAttackPoint.localPosition = p;
            }

            // Speed and Grounded Sync
            float rawSpeed = _player.IsOwner ? Mathf.Abs(_player.FrameInput.x) : _player.NetMoveSpeed.Value;
            bool isGrounded = _player.IsGrounded;

            // Use a thresholded speed for the animator to avoid precision issues
            float animSpeed = rawSpeed > 0.1f ? rawSpeed : 0f;

            _anim.SetFloat(SpeedKey, animSpeed);
            _anim.SetBool(IsGroundedKey, isGrounded);
            
            // Still set IdleSpeed for legacy compatibility
            _anim.SetFloat(IdleSpeedKey, Mathf.Lerp(1, _maxIdleSpeed, animSpeed));

            // Move Particles
            if (isGrounded && animSpeed > 0.1f)
            {
                if (_moveParticles != null && !_moveParticles.isPlaying) _moveParticles.Play();
            }
            else
            {
                if (_moveParticles != null && _moveParticles.isPlaying) _moveParticles.Stop();
            }
            Debug.Log($"NetMoveSpeed: {_player.NetMoveSpeed.Value}, animSpeed: {animSpeed}");
            // Tilt
            var runningTilt = isGrounded ? Quaternion.Euler(0, 0, _maxTilt * (_player.NetFacingRight.Value ? 1 : -1) * animSpeed) : Quaternion.identity;
            _anim.transform.up = Vector3.RotateTowards(_anim.transform.up, runningTilt * Vector2.up, _tiltSpeed * Time.deltaTime, 0f);
        }
}
}