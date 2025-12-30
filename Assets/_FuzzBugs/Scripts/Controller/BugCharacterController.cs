using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TMKOC.FuzzBugClone
{
    public class BugCharacterController : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private float _moveSpeed = 100f;
        [SerializeField] private Animator _animator;


        [SerializeField] private RectTransform _rectTransform;
        public RectTransform RectTransform => _rectTransform;


        private Vector2 _moveDirection;
        private RectTransform _parentRect;

        private const string ANIM_IDLE = "Idle";
        private const string ANIM_WALK = "Walk";
        private const string ANIM_PICKED_UP = "DragPose";
        private const string ANIM_DANCE = "Celebration";

        private void Awake()
        {
            if (_rectTransform == null)
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

            // Fix: Enforce facing direction based on movement
            if (startDirection.x != 0)
            {
                Vector3 scale = _rectTransform.localScale;
                
                // Sprite faces Right by default.
                // If moving Right (x > 0), Scale X should be positive.
                // If moving Left (x < 0), Scale X should be negative.
                scale.x = Mathf.Abs(scale.x) * Mathf.Sign(startDirection.x); 
                
                _rectTransform.localScale = scale;
            }
        }

        public BugColorType BugColor => _myColor;
        private bool _isDragging = false;
        private bool _isSorted = false;

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
                if (!_isSorted)
                    PlayAnimation(ANIM_WALK);
                else
                    PlayAnimation(ANIM_IDLE);
            }
        }

        private void UpdateMovement()
        {
            if (_rectTransform == null || _isDragging || _isSorted) return;

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
            _isSorted = true;
            PlayAnimation(ANIM_IDLE);
        }

        public void PlayDance()
        {
            PlayAnimation(ANIM_DANCE);
        }

        public void PlayStaticIdle()
        {
            PlayAnimation(ANIM_IDLE);
        }

        private void PlayAnimation(string stateName)
        {
            if (_animator != null && _animator.runtimeAnimatorController != null)
            {
                // Verify state exists? Hard to do efficiently without hashing. 
                // We'll trust the string exists or Animator ignores it.
                // Added check for runtimeAnimatorController to be safe.
                _animator.CrossFade(stateName, 0.05f);
            }
        }

        // --- Manual Counting Support ---
        public bool IsCounted { get; set; } = false;

        public void OnPointerClick(PointerEventData eventData)
        {
            // Check if interactions are blocked
            if (GameManager.Instance != null && GameManager.Instance.AreInteractionsBlocked)
            {
                Debug.Log("BugCharacterController: Interactions blocked, ignoring tap.");
                return;
            }

            // Forward interaction to Parent JarController
            // The JarController validates if the current GameState allows interaction
            if (GameManager.Instance != null)
            {
                JarController jar = GetComponentInParent<JarController>();
                if (jar != null)
                {
                    jar.OnManualBugTapped(this);
                }
            }
        }
    }
}
