using UnityEngine;
using UnityEngine.UI;

namespace TMKOC.FuzzBugClone
{
    public class FuzzBugController : MonoBehaviour
    {
        [SerializeField] private float _moveSpeed = 100f;
        [SerializeField] private Animator _animator;

        private RectTransform _rectTransform;
        private Vector2 _moveDirection;
        private RectTransform _parentRect;

        private const string ANIM_WALK = "Walk";
        private const string ANIM_IDLE = "Idle";

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }
        }

        private void Start()
        {
            if (transform.parent != null)
            {
                _parentRect = transform.parent.GetComponent<RectTransform>();
            }
            
            // Start walking immediately as we move by default
            PlayAnimation(ANIM_WALK);
        }

        private void Update()
        {
            UpdateMovement();
            CheckBounds();
        }

        [SerializeField] private Image _image;
        [SerializeField] private float _speedVariance = 20f;
        private BugColorType _myColor;

        public void Initialize(Vector2 startDirection, float speed, Sprite sprite, BugColorType colorType)
        {
            _moveDirection = startDirection.normalized;
            
            // Apply random variance to the base speed
            float randomSpeedDelta = Random.Range(-_speedVariance, _speedVariance);
            _moveSpeed = speed + randomSpeedDelta;
            
            _myColor = colorType;

            if (_image != null && sprite != null)
            {
                _image.sprite = sprite;
            }
        }

        private void UpdateMovement()
        {
            if (_rectTransform == null) return;

            _rectTransform.anchoredPosition += _moveDirection * _moveSpeed * Time.deltaTime;
        }

        private void CheckBounds()
        {
            if (_parentRect == null) return;

            float halfWidth = _parentRect.rect.width / 2f;
            // Assuming the anchor is center-center. 
            // If the character goes beyond the right edge
            if (_rectTransform.anchoredPosition.x >= halfWidth && _moveDirection.x > 0)
            {
                _moveDirection.x = -1;
                FlipCharacter();
            }
            // If the character goes beyond the left edge
            else if (_rectTransform.anchoredPosition.x <= -halfWidth && _moveDirection.x < 0)
            {
                _moveDirection.x = 1;
                FlipCharacter();
            }
        }

        private void FlipCharacter()
        {
            Vector3 scale = _rectTransform.localScale;
            scale.x *= -1;
            _rectTransform.localScale = scale;
        }

        private void PlayAnimation(string stateName)
        {
            if (_animator != null)
            {
                _animator.Play(stateName);
            }
        }
    }
}
