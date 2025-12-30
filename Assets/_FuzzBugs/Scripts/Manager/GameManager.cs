using System;
using UnityEngine;

namespace TMKOC.FuzzBugClone
{
    public enum GameState
    {
        Init,
        ColorSorting, // bugs are sorted by the color
        Counting, // bugs in one colored jar are counted
        FindLeast, // before sorting by count starts we first ask the user which one has least
        FindMost,  // and which has most, enforcing the ascending sort by count
        CountSorting, // Ordering Phase: Sorting jars in ascending order based on count
        SelectJarToQuiz, // User selects a jar to start the spatial quiz sequence
        Graphing,
        FindLeft,
        FindRight,
        FindTop,
        FindBottom,
        FindLargest,
        FindSmallest,
        GameEnd
    }

    public class GameManager : GenericSingleton<GameManager>
    {
        #region Fields & Properties

        public GameState CurrentState { get; private set; }

        public static event Action<GameState> OnStateChange;

        [Header("Scene References")]

        private int _totalBugsToSort;
        private int _bugsSortedCount;
        private int _jarsFinishedCount = 0;
        private JarController _activeJar;

        [Header("Lives Settings")]
        [SerializeField] private int _maxLives = 3;
        private int _currentLives;

        // Interaction Blocking
        private bool _interactionsBlocked = false;
        public bool AreInteractionsBlocked => _interactionsBlocked;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            if (InteractionManager.Instance != null)
            {
                InteractionManager.Instance.OnQuestionCorrect += HandleQuestionCorrect;
                Debug.Log("GameManager: Subscribed to OnQuestionCorrect");
            }
            else
            {
                Debug.LogError("GameManager: InteractionManager Instance is null in Start!");
            }

            ChangeState(GameState.Init);
        }

        private void OnDestroy()
        {
            if (InteractionManager.Instance != null)
                InteractionManager.Instance.OnQuestionCorrect -= HandleQuestionCorrect;
        }

        #endregion

        #region State Management

        public void ChangeState(GameState newState)
        {
            CurrentState = newState;
            Debug.Log($"GameManager: State changed to {newState}");

            switch (CurrentState)
            {
                case GameState.Init:
                    HandleInitState();
                    break;
                case GameState.ColorSorting:
                    HandleColorSortingState();
                    break;
                case GameState.Counting:
                    Debug.Log("GameManager: All bugs sorted! Ready for Counting.");
                    break;
                case GameState.FindLeast:
                    // Block interactions until question audio completes
                    BlockInteractions(true);
                    InteractionManager.Instance.PlayQuestion(QuestionType.FindLeast);
                    break;
                case GameState.FindMost:
                    // Block interactions until question audio completes
                    BlockInteractions(true);
                    InteractionManager.Instance.PlayQuestion(QuestionType.FindMost);
                    break;
                case GameState.CountSorting:
                    HandleCountSortingState();
                    break;
                case GameState.SelectJarToQuiz:
                    Debug.Log("GameManager: Select a Jar to start Quiz.");
                    break;
                case GameState.FindLeft:
                    HandleFindLeftState();
                    break;
                case GameState.FindRight:
                    BlockInteractions(true);
                    InteractionManager.Instance.PlayQuestion(QuestionType.FindRight);
                    break;
                case GameState.FindTop:
                    HandleFindTopState();
                    break;
                case GameState.FindBottom:
                    BlockInteractions(true);
                    InteractionManager.Instance.PlayQuestion(QuestionType.FindBottom);
                    break;
                case GameState.FindLargest:
                    HandleFindLargestState();
                    break;
                case GameState.FindSmallest:
                    BlockInteractions(true);
                    InteractionManager.Instance.PlayQuestion(QuestionType.FindSmallest);
                    break;
                case GameState.GameEnd:
                    HandleGameEndState();
                    break;
            }

            OnStateChange?.Invoke(CurrentState);
        }

        private void HandleInitState()
        {
            _jarsFinishedCount = 0;
            _bugsSortedCount = 0;

            // Reset Lives
            _currentLives = _maxLives;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateLives(_currentLives);
            }

            LevelDataManager.Instance.GenerateLevelData();
            CalculateTotalBugs();

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowEndPanel(false);
            }

            ChangeState(GameState.ColorSorting);
        }

        private void HandleColorSortingState()
        {
            if (CharacterSpawner.Instance != null)
                CharacterSpawner.Instance.SpawnCharacters();
        }

        private void HandleCountSortingState()
        {
            Debug.Log("GameManager: Ordering Enabled.");

            ActivateJarOrdering();
        }

        private void HandleFindLeftState()
        {
            if (_activeJar != null)
            {
                _activeJar.ArrangeBugsHorizontally(randomizeSize: false);
                BlockInteractions(true);
                InteractionManager.Instance.PlayQuestion(QuestionType.FindLeft);
            }
            else
            {
                Debug.LogWarning("No Active Jar for FindLeft!");
            }
        }

        private void HandleFindTopState()
        {
            if (_activeJar != null)
            {
                _activeJar.ArrangeBugsVertically();
                BlockInteractions(true);
                InteractionManager.Instance.PlayQuestion(QuestionType.FindTop);
            }
        }

        private void HandleFindLargestState()
        {
            if (_activeJar != null)
            {
                _activeJar.ArrangeBugsHorizontally(randomizeSize: true);
                BlockInteractions(true);
                InteractionManager.Instance.PlayQuestion(QuestionType.FindLargest);
            }
        }

        private void HandleGameEndState()
        {
            Debug.Log("Game Over!");
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowEndPanel(true);
            }
        }

        #endregion

        #region Event Handlers

        private void HandleQuestionCorrect()
        {
            Debug.Log("GameManager: Handling Correct Answer transition.");

            // Stop celebration animations for spatial/size questions before transitioning
            if (CurrentState == GameState.FindLeft ||
                CurrentState == GameState.FindRight ||
                CurrentState == GameState.FindTop ||
                CurrentState == GameState.FindBottom ||
                CurrentState == GameState.FindLargest ||
                CurrentState == GameState.FindSmallest)
            {
                if (_activeJar != null)
                {
                    _activeJar.ResetBugAnimations();
                }
            }

            switch (CurrentState)
            {
                case GameState.FindLeast:
                    ChangeState(GameState.FindMost);
                    break;
                case GameState.FindMost:
                    ChangeState(GameState.CountSorting);
                    break;
                case GameState.FindLeft:
                    ChangeState(GameState.FindRight);
                    break;
                case GameState.FindRight:
                    ChangeState(GameState.FindTop);
                    break;
                case GameState.FindTop:
                    ChangeState(GameState.FindBottom);
                    break;
                case GameState.FindBottom:
                    ChangeState(GameState.FindLargest);
                    break;
                case GameState.FindLargest:
                    ChangeState(GameState.FindSmallest);
                    break;
                case GameState.FindSmallest:
                    Debug.Log("GameManager: Sequence Complete. Moving to End.");
                    ChangeState(GameState.GameEnd);
                    break;
            }
        }

        public void OnBugSorted()
        {
            if (CurrentState != GameState.ColorSorting) return;

            _bugsSortedCount++;

            if (_bugsSortedCount >= _totalBugsToSort)
            {
                ChangeState(GameState.Counting);
            }
        }

        public void OnJarFinishedCounting()
        {
            if (CurrentState != GameState.Counting) return;

            _jarsFinishedCount++;
            Debug.Log($"GameManager: Jar Finished Counting. Progress: {_jarsFinishedCount}/{System.Enum.GetValues(typeof(BugColorType)).Length}");

            if (_jarsFinishedCount >= System.Enum.GetValues(typeof(BugColorType)).Length)
            {
                StartCoroutine(WaitAndFinishCountingRoutine());
            }
        }

        public void OnAllJarsCountedComplete()
        {
            // Play instruction audio without blocking interactions
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayVoice(
                    FuzzBugAudio.Instruction_TapJarToQuiz,
                    onComplete: null,
                    blockInteractions: false
                );
            }

            // Add small delay before transitioning to ensure jar raycasts are properly enabled
            StartCoroutine(DelayedStateTransition(GameState.FindLeast, 0.1f));
        }

        private System.Collections.IEnumerator DelayedStateTransition(GameState newState, float delay)
        {
            yield return new WaitForSeconds(delay);
            ChangeState(newState);
        }

        private System.Collections.IEnumerator WaitAndFinishCountingRoutine()
        {
            // Wait for celebration to play
            yield return new WaitForSeconds(1.5f);

            // Clean up animation on the active jar
            if (_activeJar != null)
            {
                _activeJar.ResetBugAnimations();
            }

            // Play instruction and proceed
            OnAllJarsCountedComplete();
        }

        public void OnJarDroppedInSlot()
        {
            if (CurrentState != GameState.CountSorting) return;
            if (UIManager.Instance == null || UIManager.Instance.JarSlotsContainer == null) return;

            CheckJarOrder();
        }

        #endregion

        #region Public Actions

        public void SetActiveJar(JarController newJar)
        {
            if (_activeJar != null && _activeJar != newJar)
            {
                _activeJar.ResetBugAnimations();
            }
            _activeJar = newJar;
        }

        public void StartQuizForJar(JarController jar)
        {
            if (CurrentState != GameState.SelectJarToQuiz) return;

            SetActiveJar(jar);
            Debug.Log($"GameManager: Starting Quiz for {jar.gameObject.name}");

            // Play transition audio with interaction blocking, then start quiz
            if (AudioManager.Instance != null)
            {
                BlockInteractions(true);
                AudioManager.Instance.PlayVoiceDelayed(
                    FuzzBugAudio.Transition_QuizStart,
                    delay: 0.3f,
                    onComplete: () => ChangeState(GameState.FindLeft),
                    blockInteractions: false // We already blocked above
                );
            }
            else
            {
                ChangeState(GameState.FindLeft);
            }
        }

        #endregion

        #region Helper Methods

        private void ActivateJarOrdering()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.EnableJarSlots(true);
            }
        }

        private void CheckJarOrder()
        {
            if (UIManager.Instance == null) return;

            int slotsCount = UIManager.Instance.JarSlotsContainer.childCount;
            JarController[] orderedJars = new JarController[slotsCount];
            bool allFilled = true;

            // 1. Checks if all slots are filled
            for (int i = 0; i < slotsCount; i++)
            {
                Transform slot = UIManager.Instance.JarSlotsContainer.GetChild(i);
                JarController jar = slot.GetComponentInChildren<JarController>();

                if (jar == null)
                {
                    allFilled = false;
                    break;
                }
                orderedJars[i] = jar;
            }

            // 2. Validate Order if filled
            if (allFilled)
            {
                Debug.Log("GameManager: All Slots Filled! Checking Order...");
                if (IsOrderCorrect(orderedJars))
                {
                    Debug.Log("<color=green>SUCCESS: Jars are sorted in Ascending Order!</color>");
                    // Add small delay to ensure jar raycasts are properly enabled
                    StartCoroutine(DelayedStateTransition(GameState.SelectJarToQuiz, 0.15f));
                }
                else
                {
                    Debug.Log("<color=red>FAIL: Jars are NOT in Ascending Order.</color>");
                    DecreaseLife();
                }
            }
        }

        private bool IsOrderCorrect(JarController[] orderedJars)
        {
            int previousCount = -1;
            bool isCorrect = true;

            for (int i = 0; i < orderedJars.Length; i++)
            {
                int bugCount = LevelDataManager.Instance.GetCountForColor(orderedJars[i].JarColor);
                Debug.Log($"Slot {i}: Jar {orderedJars[i].JarColor} (Count: {bugCount})");

                if (bugCount < previousCount)
                {
                    isCorrect = false;
                }
                previousCount = bugCount;
            }
            return isCorrect;
        }

        private void CalculateTotalBugs()
        {
            _totalBugsToSort = 0;
            _bugsSortedCount = 0;
            foreach (BugColorType color in System.Enum.GetValues(typeof(BugColorType)))
            {
                _totalBugsToSort += LevelDataManager.Instance.GetCountForColor(color);
            }
            Debug.Log($"GameManager: Total bugs to sort: {_totalBugsToSort}");
        }

        /// <summary>
        /// Blocks or unblocks all game interactions.
        /// </summary>
        public void BlockInteractions(bool block)
        {
            _interactionsBlocked = block;
            Debug.Log($"GameManager: Interactions {(block ? "BLOCKED" : "UNBLOCKED")}");
        }

        public void DecreaseLife()
        {
            if (CurrentState == GameState.GameEnd) return;

            _currentLives--;
            Debug.Log($"GameManager: Life Lost! Remaining: {_currentLives}");

            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateLives(_currentLives);
            }

            if (_currentLives <= 0)
            {
                Debug.Log("GameManager: No lives left! Game Over.");
                ChangeState(GameState.GameEnd);
            }
        }

        #endregion
    }
}
