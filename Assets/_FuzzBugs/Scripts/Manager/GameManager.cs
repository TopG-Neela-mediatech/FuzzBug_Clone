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
        [SerializeField] private CharacterSpawner _characterSpawner;
        [SerializeField] private Transform _jarSlotsContainer;
        [SerializeField] private GameObject _endPanel;

        private int _totalBugsToSort;
        private int _bugsSortedCount;
        private int _jarsFinishedCount = 0;
        private JarController _activeJar;

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
                    InteractionManager.Instance.PlayQuestion(QuestionType.FindLeast);
                    break;
                case GameState.FindMost:
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
                    InteractionManager.Instance.PlayQuestion(QuestionType.FindRight);
                    break;
                case GameState.FindTop:
                    HandleFindTopState();
                    break;
                case GameState.FindBottom:
                    InteractionManager.Instance.PlayQuestion(QuestionType.FindBottom);
                    break;
                case GameState.FindLargest:
                    HandleFindLargestState();
                    break;
                case GameState.FindSmallest:
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
            LevelDataManager.Instance.GenerateLevelData();
            CalculateTotalBugs();
            
            if (_endPanel != null) 
                _endPanel.SetActive(false);
            
            ChangeState(GameState.ColorSorting);
        }

        private void HandleColorSortingState()
        {
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
                InteractionManager.Instance.PlayQuestion(QuestionType.FindTop);
            }
        }

        private void HandleFindLargestState()
        {
            if (_activeJar != null)
            {
                _activeJar.ArrangeBugsHorizontally(randomizeSize: true);
                InteractionManager.Instance.PlayQuestion(QuestionType.FindLargest);
            }
        }

        private void HandleGameEndState()
        {
            Debug.Log("Game Over!");
            if (_endPanel != null)
            {
                _endPanel.SetActive(true);
            }
        }

        #endregion

        #region Event Handlers

        private void HandleQuestionCorrect()
        {
            Debug.Log("GameManager: Handling Correct Answer transition.");
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
                ChangeState(GameState.FindLeast);
            }
        }

        public void OnJarDroppedInSlot()
        {
            if (CurrentState != GameState.CountSorting) return;
            if (_jarSlotsContainer == null) return;

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
            ChangeState(GameState.FindLeft);
        }

        #endregion

        #region Helper Methods

        private void ActivateJarOrdering()
        {
            if (_jarSlotsContainer != null)
            {
                _jarSlotsContainer.gameObject.SetActive(true);
            }
        }

        private void CheckJarOrder()
        {
            int slotsCount = _jarSlotsContainer.childCount;
            JarController[] orderedJars = new JarController[slotsCount];
            bool allFilled = true;

            // 1. Checks if all slots are filled
            for (int i = 0; i < slotsCount; i++)
            {
                Transform slot = _jarSlotsContainer.GetChild(i);
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
                    ChangeState(GameState.SelectJarToQuiz);
                }
                else
                {
                    Debug.Log("<color=red>FAIL: Jars are NOT in Ascending Order.</color>");
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

        #endregion
    }
}
