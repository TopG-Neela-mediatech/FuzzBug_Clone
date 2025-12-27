using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using TMPro; // Added for TextMeshPro
using System.Collections;

namespace TMKOC.FuzzBugClone
{
    public class JarController : MonoBehaviour, IDropHandler, IPointerClickHandler
    {
        [SerializeField] private Image _jar;
        [SerializeField] private BugColorType _jarColorType;
        [SerializeField] private Transform _bugContainer;
        [SerializeField] private Transform _jarEntryPointLeft; // Renamed for clarity
        [SerializeField] private Transform _jarEntryPointRight; // Renamed for clarity
        [SerializeField] private Transform _horizontalLineStart; // Added for Horizontal Arrangement
        [SerializeField] private Transform _verticalLineStart; // Added for Vertical Arrangement
        [SerializeField] private float _dropVarianceX = 60f;
        [SerializeField] private float _droppedBugScale = 0.5f;
        [SerializeField] private float _arrangedBugScale = 0.5f;
        [SerializeField] private float _randomSizeYOffset = -15f;

        [Header("UI Components")]
        [SerializeField] private GameObject _countTextParent;
        [SerializeField] private TextMeshProUGUI _countText;

        [Header("Grid Layout Settings")]
        [SerializeField] private float _gridSpacingX = 70f;
        [SerializeField] private float _arrangementSpacingX = 120f;
        [SerializeField] private float _gridSpacingY = 60f;
        [SerializeField] private int _gridColumns = 3;
        [SerializeField] private float _startY = 0f;

        [Header("Animation Settings")]
        [SerializeField] private float _gridLayoutDuration = 0.5f;
        [SerializeField] private float _horizontalLayoutDuration = 0.5f;
        [SerializeField] private float _verticalLayoutDuration = 0.5f;

        private Material _jarMaterial;
        private bool _isCounting = false;

        public BugColorType JarColor { get { return _jarColorType; } }

        private void Start()
        {
            // Create runtime instance of the material
            _jarMaterial = _jar.materialForRendering;
            if (_countText != null)
            {
                _countText.text = ""; // Clear initially
                _countTextParent.gameObject.SetActive(false);
            }
        }

        private bool _isFinished = false; // Track if counting is fully complete
        public bool IsFinished { get { return _isFinished; } }

        public void OnDrop(PointerEventData eventData)
        {
            Debug.Log($"OnDrop called on {gameObject.name}");
            if (eventData.pointerDrag != null)
            {
                // check if the dropped item is a Jar (for Swapping)
                JarController droppedJar = eventData.pointerDrag.GetComponent<JarController>();
                if (droppedJar != null)
                {
                    // Pass the drop event to the parent Slot to handle the swap
                    JarSlot parentSlot = GetComponentInParent<JarSlot>();
                    if (parentSlot != null)
                    {
                        parentSlot.OnDrop(eventData);
                    }
                    return;
                }

                BugCharacterController bug = eventData.pointerDrag.GetComponent<BugCharacterController>();
                Draggable draggable = eventData.pointerDrag.GetComponent<Draggable>();

                if (bug != null && draggable != null)
                {
                    Debug.Log($"Dropped Bug Color: {bug.BugColor}, Jar Color: {_jarColorType}");
                    if (bug.BugColor == _jarColorType)
                    {
                        Debug.Log($"<color=green>Correct Bug! {bug.BugColor} matches Jar.</color>");

                        // Notify draggable it's handled
                        draggable.Consume();

                        // Parenting to Jar Container
                        if (_bugContainer != null)
                        {
                            bug.transform.SetParent(_bugContainer);
                        }
                        else
                        {
                            bug.transform.SetParent(transform);
                            Debug.LogWarning("Bug Container not assigned in JarController, parenting to Jar root.");
                        }

                        // Disable interactions
                        if (bug.TryGetComponent<CanvasGroup>(out var cg))
                        {
                            cg.blocksRaycasts = false;
                        }

                        // Calculate Random Target Position inside the jar
                        // Variance in X relative to the container's center (0,0,0)
                        float randomX = Random.Range(-_dropVarianceX, _dropVarianceX);
                        Vector3 targetPos = new Vector3(randomX, 0, 0);

                        // DOTween Sequence for Entry
                        Sequence dropSequence = DOTween.Sequence();

                        // If Entry Point is assigned, Jump there first
                        // We prioritize Left point, but logic handles single or double points
                        if (_jarEntryPointLeft != null)
                        {
                            Vector3 entryPos = _jarEntryPointLeft.position;

                            // If Right point exists, pick random point between Left and Right
                            if (_jarEntryPointRight != null)
                            {
                                entryPos = Vector3.Lerp(_jarEntryPointLeft.position, _jarEntryPointRight.position, Random.value);
                            }

                            // Jump to the entry point (World Position)
                            // Power 1-2f, 1 jump, duration 0.6s, Linear Ease
                            float jumpPower = Random.Range(1f, 2f);
                            dropSequence.Append(bug.transform.DOJump(entryPos, jumpPower, 1, 0.6f).SetEase(Ease.Linear));

                            // Then Fall/Move to final spot
                            // Duration 0.5s, Linear/InQuad for falling effect
                            dropSequence.Append(bug.transform.DOLocalMove(targetPos, 0.5f).SetEase(Ease.InQuad));
                        }
                        else
                        {
                            // Fallback if no entry point: Just Jump directly to target
                            dropSequence.Append(bug.transform.DOLocalJump(targetPos, 300f, 1, 0.5f).SetEase(Ease.OutQuad));
                        }

                        // Concurrent Scaling
                        // Scale down slightly but preserve facing direction (Sign of X)
                        float currentSignX = Mathf.Sign(bug.transform.localScale.x);
                        Vector3 targetScale = new Vector3(_droppedBugScale * currentSignX, _droppedBugScale, _droppedBugScale);

                        // Scale happens during the whole sequence or just the fall? 
                        if (_jarEntryPointLeft != null)
                        {
                            // Start scaling slightly before the fall starts (during the end of the jump)
                            dropSequence.Insert(0.5f, bug.transform.DOScale(targetScale, 0.5f).SetEase(Ease.OutQuad));
                        }
                        else
                        {
                            dropSequence.Join(bug.transform.DOScale(targetScale, 0.5f).SetEase(Ease.OutQuad));
                        }

                        // Callback when sequence complete
                        dropSequence.OnComplete(() =>
                        {
                            // ANIMATION: Set Sorted State (StaticIdle)
                            bug.SetSorted();
                        });

                        // Notify Manager
                        if (GameManager.Instance != null)
                        {
                            GameManager.Instance.OnBugSorted();
                        }
                    }
                    else
                    {
                        Debug.Log($"<color=red>Wrong Bug! {bug.BugColor} does not match {_jarColorType} Jar.</color>");
                        // Do NOT consume. Let it return to start via Draggable.cs OnEndDrag
                    }
                }
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // Question Phase Interaction
            if (GameManager.Instance.CurrentState == GameState.FindLeast ||
                GameManager.Instance.CurrentState == GameState.FindMost)
            {
                if (InteractionManager.Instance != null)
                {
                    InteractionManager.Instance.SubmitAnswer_Jar(this);
                }
                return;
            }

            // Selection Phase
            if (GameManager.Instance.CurrentState == GameState.SelectJarToQuiz)
            {
                GameManager.Instance.StartQuizForJar(this);
                return;
            }

            // Only start layout if we are in Counting phase and not already counting this jar (or finished)
            if (GameManager.Instance.CurrentState == GameState.Counting && !_isCounting && !_isFinished)
            {
                StartCountingSequence();
            }
        }

        private void StartCountingSequence()
        {
            _isCounting = true;

            // Notify GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetActiveJar(this);
            }

            // Disable Jar raycast so we can click the bugs
            ToggleJarRaycast(false);

            ArrangeBugsInGrid();

            // Initialize count text
            // If we are resuming? No, request says "return to original position". 
            // So every time we start, we start from 0 if it wasn't finished?
            // "Only if all... are counted... then i will flag as... counted... otherwise... return to original position"
            // So yes, reset count to 0.
            if (_countText != null)
            {

                _countTextParent.gameObject.SetActive(true);
                _countText.text = "0";
                _countText.color = Color.white; // Reset color
            }

            // Reset bugs "IsCounted" status? 
            foreach (Transform child in _bugContainer)
            {
                var bug = child.GetComponent<BugCharacterController>();
                if (bug != null) bug.IsCounted = false;
            }
        }

        public void OnManualBugTapped(BugCharacterController bug)
        {
            // Spatial Question Interactions
            if (GameManager.Instance.CurrentState == GameState.FindLeft ||
                GameManager.Instance.CurrentState == GameState.FindRight ||
                GameManager.Instance.CurrentState == GameState.FindTop ||
                GameManager.Instance.CurrentState == GameState.FindBottom ||
                GameManager.Instance.CurrentState == GameState.FindLargest ||
                GameManager.Instance.CurrentState == GameState.FindSmallest)
            {
                if (InteractionManager.Instance != null)
                {
                    InteractionManager.Instance.SubmitAnswer_Bug(bug);
                }
                return;
            }

            if (!_isCounting) return;
            if (bug.IsCounted) return;

            // Mark as counted
            bug.IsCounted = true;

            // Animate Punch
            bug.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f, 10, 1);
            bug.PlayDance();

            // Increment Count
            int currentCount = 0;
            if (int.TryParse(_countText.text, out int parsed)) currentCount = parsed;
            currentCount++;
            _countText.text = currentCount.ToString();

            // Check if all bugs are counted
            int totalBugsForJar = LevelDataManager.Instance.GetCountForColor(_jarColorType);
            if (currentCount >= totalBugsForJar)
            {
                _isFinished = true;
                // if (_countText != null) _countText.color = Color.green; // Removed per user request

                if (GameManager.Instance != null)
                {
                    GameManager.Instance.OnJarFinishedCounting();
                }
            }
        }

        // Called by GameManager when another Jar starts counting
        public void ResetCountingState()
        {
            if (_isFinished) return; // If finished, stay arranged

            _isCounting = false;
            ToggleJarRaycast(true); // Re-enable click
            if (_countText != null) _countText.text = ""; // Hide count

            if (_bugContainer == null) return;

            foreach (Transform child in _bugContainer)
            {
                BugCharacterController bug = child.GetComponent<BugCharacterController>();
                if (bug != null)
                {
                    bug.PlayStaticIdle();
                    bug.IsCounted = false;

                    // Return to random position (Messy)
                    // Calculate Random Target Position similar to OnDrop
                    float randomX = Random.Range(-_dropVarianceX, _dropVarianceX);
                    Vector3 targetPos = new Vector3(randomX, 0, 0);

                    bug.transform.DOLocalMove(targetPos, 0.5f).SetEase(Ease.OutQuad);
                    bug.transform.DOScale(Vector3.one * _droppedBugScale, 0.5f); // Reset scale
                    bug.transform.localRotation = Quaternion.identity; // Reset rotation
                }
            }
        }

        public void ResetBugAnimations()
        {
            if (_isFinished)
            {
                // If finished, just stop dancing, but keep position
                if (_bugContainer != null)
                {
                    foreach (Transform child in _bugContainer)
                    {
                        BugCharacterController bug = child.GetComponent<BugCharacterController>();
                        if (bug != null)
                        {
                            bug.PlayStaticIdle();
                        }
                    }
                }
            }
            else
            {
                // If not finished, full reset (messy pile)
                ResetCountingState();
            }
        }

        private void ArrangeBugsInGrid()
        {
            if (_bugContainer == null || _bugContainer.childCount == 0) return;

            int childCount = _bugContainer.childCount;

            // Calculate centering offset
            float totalWidth = (_gridColumns - 1) * _gridSpacingX;
            float startX = -totalWidth / 2f;

            for (int i = 0; i < childCount; i++)
            {
                Transform child = _bugContainer.GetChild(i);

                int col = i % _gridColumns;
                int row = i / _gridColumns;

                float targetX = startX + (col * _gridSpacingX);
                float targetY = _startY + (row * _gridSpacingY);

                Vector3 targetPos = new Vector3(targetX, targetY, 0);

                // Re-enable Raycasts for interaction
                if (child.TryGetComponent<CanvasGroup>(out var cg))
                {
                    cg.blocksRaycasts = true;
                }

                // Disable Draggable to prevent accidental drags
                if (child.TryGetComponent<Draggable>(out var draggable))
                {
                    draggable.enabled = false;
                }


                // Animate to grid position
                child.DOLocalMove(targetPos, _gridLayoutDuration).SetEase(Ease.OutBack);
                child.DORotate(Vector3.zero, 0.3f);
                child.DOScale(Vector3.one * _arrangedBugScale, _gridLayoutDuration);
            }
        }

        public void PlayCelebration()
        {
            if (_bugContainer == null) return;
            foreach (Transform child in _bugContainer)
            {
                BugCharacterController bug = child.GetComponent<BugCharacterController>();
                if (bug != null)
                {
                    bug.PlayDance();
                }
            }
        }

        public void ArrangeBugsHorizontally(bool randomizeSize = false)
        {
            if (_bugContainer == null || _horizontalLineStart == null) return;

            int childCount = _bugContainer.childCount;
            if (childCount == 0) return;

            // Centered Horizontal Layout
            // Total width = (count - 1) * spacing
            // Start X = CenterX - TotalWidth / 2
            float totalWidth = (childCount - 1) * _arrangementSpacingX; // Use new Spacing
            float startX = _horizontalLineStart.localPosition.x - (totalWidth / 2f);
            float fixedY = _horizontalLineStart.localPosition.y;

            for (int i = 0; i < childCount; i++)
            {
                Transform child = _bugContainer.GetChild(i);

                float targetX = startX + (i * _arrangementSpacingX); // Use new Spacing
                float currentFixedY = fixedY;

                // Size Logic Pre-calculation for Position
                if (randomizeSize)
                {
                   currentFixedY += _randomSizeYOffset;
                }

                Vector3 targetPos = new Vector3(targetX, currentFixedY, 0);

                // We need to convert this targetPos...
                Vector3 worldTarget = _horizontalLineStart.parent.TransformPoint(new Vector3(targetX, currentFixedY, _horizontalLineStart.localPosition.z));
                Vector3 localTargetInContainer = _bugContainer.InverseTransformPoint(worldTarget);

                child.DOLocalMove(localTargetInContainer, _horizontalLayoutDuration).SetEase(Ease.OutBack);
                child.DORotate(Vector3.zero, 0.3f);

                // Size Logic
                if (randomizeSize)
                {
                    // Adjust pivot to bottom so scaling grows from feet up
                    if (child.TryGetComponent<RectTransform>(out var rt))
                    {
                        rt.pivot = new Vector2(0.5f, 0f);
                    }

                    float randomScale = Random.Range(0.4f, 1.1f);
                    child.DOScale(Vector3.one * randomScale, _horizontalLayoutDuration).SetEase(Ease.OutBack);
                }
                else
                {
                    // Reset to standard size if needed, or keep current? 
                    // Usually we want to reset to standard "sorted" size which seemed to be 0.6f or 0.5f
                    child.DOScale(Vector3.one * _arrangedBugScale, _horizontalLayoutDuration).SetEase(Ease.OutBack);
                }
            }
        }

        public void ArrangeBugsVertically()
        {
            if (_bugContainer == null || _verticalLineStart == null) return;

            int childCount = _bugContainer.childCount;
            if (childCount == 0) return;

            // Centered Vertical Layout (matching Horizontal logic)
            // Total height = (count - 1) * spacing
            // Start Y = CenterY - TotalHeight / 2
            float totalHeight = (childCount - 1) * _gridSpacingY;
            // Removed centering: Start from the anchor directly
            float startY = _verticalLineStart.localPosition.y;
            float fixedX = _verticalLineStart.localPosition.x;

            for (int i = 0; i < childCount; i++)
            {
                Transform child = _bugContainer.GetChild(i);

                float targetY = startY + (i * _gridSpacingY);
                Vector3 targetPos = new Vector3(fixedX, targetY, 0);

                // Convert to Parent (Jar) space -> Container space
                Vector3 worldTarget = _verticalLineStart.parent.TransformPoint(new Vector3(fixedX, targetY, _verticalLineStart.localPosition.z));
                Vector3 localTargetInContainer = _bugContainer.InverseTransformPoint(worldTarget);

                child.DOLocalMove(localTargetInContainer, _verticalLayoutDuration).SetEase(Ease.OutBack);
                child.DORotate(Vector3.zero, 0.3f);

                // Force scale 0.6f for uniformity
                child.DOScale(Vector3.one * _arrangedBugScale, _verticalLayoutDuration).SetEase(Ease.OutBack);
            }
        }


        public void ToggleJarRaycast(bool value)
        {
            if (_jar != null)
                _jar.raycastTarget = value;
        }
    }
}