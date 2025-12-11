using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TMKOC.FuzzBugClone
{
    [RequireComponent(typeof(CanvasGroup))]
    public class Draggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private float _dragAlpha = 1.0f;
        [SerializeField] private float _returnSpeed = 10f;

        private RectTransform _rectTransform;
        private Canvas _canvas;
        private CanvasGroup _canvasGroup;
        private Vector2 _originalPosition;
        private Transform _originalParent;
        private int _originalSiblingIndex;
        private FuzzBugController _bugController;
        private bool _isConsumed = false;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();
            _bugController = GetComponent<FuzzBugController>();
            
            // Find the root canvas
            _canvas = GetComponentInParent<Canvas>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _isConsumed = false;
            if (_bugController != null)
            {
                _bugController.SetDragging(true);
            }

            _originalPosition = _rectTransform.anchoredPosition;
            _originalParent = _rectTransform.parent;
            _originalSiblingIndex = _rectTransform.GetSiblingIndex();

            // Bring to front for sorting
            // We set it as the last sibling of the ROOT canvas or current parent?
            // User requested robust sorting. Moving to root is safest, but scaling might change 
            // if nested in different layout groups.
            // Safest simple approach: Set as last sibling of current parent.
            _rectTransform.SetAsLastSibling();

            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.alpha = _dragAlpha;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_canvas == null) return;

            _rectTransform.anchoredPosition += eventData.delta / _canvas.scaleFactor;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.alpha = 1f;

            if (!_isConsumed)
            {
                // Return to original state if not consumed
                ReturnToStart();
                
                if (_bugController != null)
                {
                    _bugController.SetDragging(false);
                }
            }
        }

        public void Consume()
        {
            _isConsumed = true;
            // The Jar will handle the rest (parenting, animating)
        }

        private void ReturnToStart()
        {
            // Simple snap for now, or could be a coroutine for smooth return
            // User requested "return to original position".
            // Restoring sibling index prevents it from staying on top forever if not dropped on a jar.
             _rectTransform.SetSiblingIndex(_originalSiblingIndex);
             
             // Smooth return could be implemented here, but for now let's snap or simple move
             // Since we are in OnEndDrag, we can just start a coroutine to move it back
             StartCoroutine(MoveBackRoutine());
        }

        private System.Collections.IEnumerator MoveBackRoutine()
        {
            float t = 0;
            Vector2 startPos = _rectTransform.anchoredPosition;
            while (t < 1f)
            {
                t += Time.deltaTime * _returnSpeed;
                _rectTransform.anchoredPosition = Vector2.Lerp(startPos, _originalPosition, t);
                yield return null;
            }
            _rectTransform.anchoredPosition = _originalPosition;
        }
    }
}
