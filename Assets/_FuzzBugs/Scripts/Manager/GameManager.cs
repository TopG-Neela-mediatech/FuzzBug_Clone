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
        // PROPERTIES

        public GameState CurrentState { get; private set; }


        // Events

        public static event Action<GameState> OnStateChange;

        private int _totalBugsToSort;
        private int _bugsSortedCount;

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

        /*
        private void OnEnable()
        {
            // Moving to Start to ensure Instance is ready
        }
        */

        private void OnDestroy()
        {
            if (InteractionManager.Instance != null)
                InteractionManager.Instance.OnQuestionCorrect -= HandleQuestionCorrect;
        }

        private void HandleQuestionCorrect()
        {
            Debug.Log("GameManager: Handling Correct Answer transition.");
            // Auto-advance state based on current? 
            // Or let InteractionManager handle specific logic?
            // Simple generic flow:
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
                    ChangeState(GameState.FindTop); // Go to Vertical Phase
                    break;
                case GameState.FindTop:
                    ChangeState(GameState.FindBottom);
                    break;
                case GameState.FindBottom:
                    ChangeState(GameState.FindLargest); // Go to Size Phase
                    break;
                case GameState.FindLargest:
                    ChangeState(GameState.FindSmallest);
                    break;
                case GameState.FindSmallest:
                    // Loop back to select another jar? or End?
                    // User said "ending with us showing the end panel"
                    Debug.Log("GameManager: Sequence Complete. Moving to End.");
                    ChangeState(GameState.GameEnd);
                    break;
            }
        }

        [Header("Scene References")]
        [SerializeField] private CharacterSpawner _characterSpawner; // Reference if needed
        [SerializeField] private Transform _jarSlotsContainer; // Parent of 4 JarSlots
        [SerializeField] private GameObject _endPanel; // Added per user request

        public void ChangeState(GameState newState)
        {
            CurrentState = newState;
            Debug.Log($"GameManager: State changed to {newState}");

            switch (CurrentState)
            {
                case GameState.Init:
                    LevelDataManager.Instance.GenerateLevelData();
                    CalculateTotalBugs();
                    // Ensure End Panel is hidden
                    if (_endPanel != null) _endPanel.SetActive(false);
                    ChangeState(GameState.ColorSorting);
                    break;
                case GameState.ColorSorting:
                    CharacterSpawner.Instance.SpawnCharacters();
                    break;
                case GameState.Counting:
                    Debug.Log("GameManager: All bugs sorted! Ready for Counting.");
                    break;
                case GameState.FindLeast:
                    //EnableJarRaycasts(); // Use helper to ensure interactions
                    InteractionManager.Instance.PlayQuestion(QuestionType.FindLeast);
                    break;
                case GameState.FindMost:
                    InteractionManager.Instance.PlayQuestion(QuestionType.FindMost);
                    break;
                case GameState.CountSorting: // (Ordering Phase)
                    Debug.Log("GameManager: Ordering Enabled.");
                    ActivateJarOrdering();
                    break;
                case GameState.SelectJarToQuiz:
                    Debug.Log("GameManager: Select a Jar to start Quiz.");
                    // Prompt?
                    break;
                case GameState.FindLeft:
                    if (_activeJar != null)
                    {
                        _activeJar.ArrangeBugsHorizontally(false); // Normal Size
                        InteractionManager.Instance.PlayQuestion(QuestionType.FindLeft);
                    }
                    else
                    {
                        Debug.LogWarning("No Active Jar for FindLeft!");
                    }
                    break;
                case GameState.FindRight:
                    InteractionManager.Instance.PlayQuestion(QuestionType.FindRight);
                    break;
                case GameState.FindTop:
                    if (_activeJar != null)
                    {
                        _activeJar.ArrangeBugsVertically();
                        InteractionManager.Instance.PlayQuestion(QuestionType.FindTop);
                    }
                    break;
                case GameState.FindBottom:
                    InteractionManager.Instance.PlayQuestion(QuestionType.FindBottom);
                    break;
                case GameState.FindLargest:
                    if (_activeJar != null)
                    {
                        _activeJar.ArrangeBugsHorizontally(true); // Randomize Size
                        InteractionManager.Instance.PlayQuestion(QuestionType.FindLargest);
                    }
                    break;
                case GameState.FindSmallest:
                    InteractionManager.Instance.PlayQuestion(QuestionType.FindSmallest);
                    break;
                case GameState.GameEnd:
                    Debug.Log("Game Over!");
                    if (_endPanel != null)
                    {
                        _endPanel.SetActive(true);
                    }
                    break;
            }

            OnStateChange?.Invoke(CurrentState);
        }



        private void ActivateJarOrdering()
        {
            // 1. Activate Shadow/Slot Container
            if (_jarSlotsContainer != null) 
            {
                _jarSlotsContainer.gameObject.SetActive(true);
            }
            
            // Draggable enabling is now handled by JarsManager to avoid FindObjectsByType
        }

        public void OnBugSorted()
        {
            if (CurrentState != GameState.ColorSorting) return;

            _bugsSortedCount++;
            // Debug.Log($"Progress: {_bugsSortedCount}/{_totalBugsToSort}");

            if (_bugsSortedCount >= _totalBugsToSort)
            {
                ChangeState(GameState.Counting);
            }
        }

        private int _jarsFinishedCount = 0;

        public void OnJarFinishedCounting()
        {
            if (CurrentState != GameState.Counting) return;

            _jarsFinishedCount++;
            Debug.Log($"GameManager: Jar Finished Counting. Progress: {_jarsFinishedCount}/{System.Enum.GetValues(typeof(BugColorType)).Length}");

            if (_jarsFinishedCount >= System.Enum.GetValues(typeof(BugColorType)).Length)
            {
                // Transition to FindLeast instead of Comparing
                ChangeState(GameState.FindLeast);
            }
        }

        private JarController _activeJar;

        public void SetActiveJar(JarController newJar)
        {
            if (_activeJar != null && _activeJar != newJar)
            {
                // Reset animations on the previous jar
                _activeJar.ResetBugAnimations();
            }
            _activeJar = newJar;
        }

        public void OnJarDroppedInSlot()
        {
            if (CurrentState != GameState.CountSorting) return;
            if (_jarSlotsContainer == null) return;

            // Check if all slots are filled and validate order
            int slotsCount = _jarSlotsContainer.childCount;
            JarController[] orderedJars = new JarController[slotsCount];
            bool allFilled = true;

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

            if (allFilled)
            {
                Debug.Log("GameManager: All Slots Filled! Checking Order...");
                bool isCorrectInfo = true;
                int previousCount = -1;

                for (int i = 0; i < orderedJars.Length; i++)
                {
                    int bugCount = LevelDataManager.Instance.GetCountForColor(orderedJars[i].JarColor);
                    Debug.Log($"Slot {i}: Jar {orderedJars[i].JarColor} (Count: {bugCount})");

                    if (bugCount < previousCount)
                    {
                        isCorrectInfo = false;
                    }
                    previousCount = bugCount;
                }

                if (isCorrectInfo)
                {
                    Debug.Log("<color=green>SUCCESS: Jars are sorted in Ascending Order!</color>");
                    // Transition to Selection Phase
                    ChangeState(GameState.SelectJarToQuiz);
                }
                else
                {
                    Debug.Log("<color=red>FAIL: Jars are NOT in Ascending Order.</color>");
                }
            }
        }

        public void StartQuizForJar(JarController jar)
        {
            if (CurrentState != GameState.SelectJarToQuiz) return;
            
            SetActiveJar(jar);
            Debug.Log($"GameManager: Starting Quiz for {jar.gameObject.name}");
            ChangeState(GameState.FindLeft);
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
    }
}
