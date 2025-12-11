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
        [SerializeField] private float _dropVarianceX = 50f;
        
        [Header("UI Components")]
        [SerializeField] private TMP_Text _countText; // Reference to the counter text

        [Header("Grid Layout Settings")]
        [SerializeField] private float _gridSpacingX = 80f;
        [SerializeField] private float _gridSpacingY = 80f;
        [SerializeField] private int _gridColumns = 3;
        [SerializeField] private float _startY = 0f;

        private Material _jarMaterial;
        private bool _isCounting = false;
        private bool _hasCounted = false;

        private void Start()
        {
            // Create runtime instance of the material
            _jarMaterial = _jar.materialForRendering;
            if (_countText != null) _countText.text = ""; // Clear initially
        }

        public void OnDrop(PointerEventData eventData)
        {
            Debug.Log($"OnDrop called on {gameObject.name}");
            if (eventData.pointerDrag != null)
            {
                FuzzBugController bug = eventData.pointerDrag.GetComponent<FuzzBugController>();
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

                        // Calculate Random Target Position
                        // Variance in X relative to the container's center (0,0,0)
                        float randomX = Random.Range(-_dropVarianceX, _dropVarianceX);
                        Vector3 targetPos = new Vector3(randomX, 0, 0);

                        // DOTween Jump Animation
                        // Jump to local target with power 300, 1 jump, duration 0.5s
                        bug.transform.DOLocalJump(targetPos, 300f, 1, 0.5f).SetEase(Ease.OutQuad);
                        
                        // Scale down slightly but preserve facing direction (Sign of X)
                        float currentSignX = Mathf.Sign(bug.transform.localScale.x);
                        Vector3 targetScale = new Vector3(0.5f * currentSignX, 0.5f, 0.5f);
                        
                        bug.transform.DOScale(targetScale, 0.5f).SetEase(Ease.OutQuad);

                        // ANIMATION: Set Sorted State (StaticIdle)
                        bug.SetSorted();

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
            // Only start layout if we are in Counting phase and haven't started counting interaction yet
            if (GameManager.Instance.CurrentState == GameState.Counting && !_isCounting && !_hasCounted)
            {
                StartCountingSequence();
            }
        }

        private void StartCountingSequence()
        {
            _isCounting = true;
            
            // Notify GameManager that this Jar is now Active for counting
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetActiveJar(this);
            }

            // Disable Jar raycast so we can click the bugs inside/behind it
            if (_jar != null) _jar.raycastTarget = false;

            ArrangeBugsInGrid();
            
            // Initialize count text
            if (_countText != null) _countText.text = "0";
        }

        public void OnManualBugTapped(FuzzBugController bug)
        {
            if (!_isCounting) return;
            if (bug.IsCounted) return;

            // Mark as counted
            bug.IsCounted = true;

            // Animate Punch
            bug.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f, 10, 1);
            
            // ANIMATION: Play Dance
            bug.PlayDance();

            // Increment Count
            int currentCount = 0;
            if (int.TryParse(_countText.text, out int parsed))
            {
                currentCount = parsed;
            }
            currentCount++;
            _countText.text = currentCount.ToString();

            // Check if all bugs are counted
            int totalBugsForJar = LevelDataManager.Instance.GetCountForColor(_jarColorType);
            if (currentCount >= totalBugsForJar)
            {
                _countText.color = Color.green; // Visual feedback
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.OnJarFinishedCounting();
                }
            }
        }
        
        // Called by GameManager when another Jar starts counting
        public void ResetBugAnimations()
        {
            if (_bugContainer == null) return;
            
            // Stop counting? Should we allow multiple jars to be "counting" state?
            // User requested "previous jar's fuzz bugs should stop dancing and revert back to static idle"
            // We don't necessarily reset the "Count Info", just the animation.
            
            foreach(Transform child in _bugContainer)
            {
                FuzzBugController bug = child.GetComponent<FuzzBugController>();
                if (bug != null)
                {
                    bug.PlayStaticIdle();
                }
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
                child.DOLocalMove(targetPos, 0.5f).SetEase(Ease.OutBack);
                child.DORotate(Vector3.zero, 0.3f); 
                child.DOScale(Vector3.one * 0.6f, 0.5f); 
            }
        }
    }
}