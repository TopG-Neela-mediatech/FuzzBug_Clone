using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TMKOC.FuzzBugClone
{
    public class FuzzBugController : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private float _moveSpeed = 100f;
        [SerializeField] private Animator _animator;

        private RectTransform _rectTransform;
        private Vector2 _moveDirection;
        private RectTransform _parentRect;

        private const string ANIM_WALK = "Walk";
        private const string ANIM_IDLE = "Idle";
        private const string ANIM_PICKED_UP = "PickedUp";
        private const string ANIM_STATIC_IDLE = "StaticIdle";
        private const string ANIM_DANCE = "Dance";

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

        public BugColorType BugColor => _myColor;
        private bool _isDragging = false;

        public void SetDragging(bool isDragging)
        {
            _isDragging = isDragging;
            if (_isDragging)
            {
                // Changed from Idle to PickedUp as requested
                PlayAnimation(ANIM_PICKED_UP);
            }
            else
            {
                // If we stop dragging but aren't sorted yet, maybe go back to Walk? 
                // Or let the caller decide. For now, default to Walk if just dropping in void.
                // If dropped in Jar, JarController will override this to StaticIdle.
                PlayAnimation(ANIM_WALK);
            }
        }

        private void UpdateMovement()
        {
            if (_rectTransform == null || _isDragging) return;

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

        public void SetSorted()
        {
            PlayAnimation(ANIM_STATIC_IDLE);
        }

        public void PlayDance()
        {
            PlayAnimation(ANIM_DANCE);
        }

        public void PlayStaticIdle()
        {
            PlayAnimation(ANIM_STATIC_IDLE);
        }

        private void PlayAnimation(string stateName)
        {
            if (_animator != null && _animator.runtimeAnimatorController != null)
            {
                // Verify state exists? Hard to do efficiently without hashing. 
                // We'll trust the string exists or Animator ignores it.
                // Added check for runtimeAnimatorController to be safe.
                _animator.Play(stateName);
            }
        }

        // --- Manual Counting Support ---
        public bool IsCounted { get; set; } = false;

        public void OnPointerClick(PointerEventData eventData)
        {
            // Only interact if in Counting state
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Counting)
            {
                // Find parent JarController
                JarController jar = GetComponentInParent<JarController>();
                if (jar != null)
                {
                    jar.OnManualBugTapped(this);
                }
            }
        }
    }
}
