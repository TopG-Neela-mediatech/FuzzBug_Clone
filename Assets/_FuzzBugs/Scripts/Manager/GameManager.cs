using UnityEngine;

namespace TMKOC.FuzzBugClone
{
    public enum GameState
    {
        Init,
        Sorting,
        Counting,
        FindLeast,
        FindMost,
        Comparing, // Ordering Phase
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
        public GameState CurrentState { get; private set; }

        private int _totalBugsToSort;
        private int _bugsSortedCount;

        private void Start()
        {
            ChangeState(GameState.Init);
        }

        private void OnEnable()
        {
            if (InteractionManager.Instance != null)
                InteractionManager.Instance.OnQuestionCorrect += HandleQuestionCorrect;
        }

        private void OnDisable()
        {
            if (InteractionManager.Instance != null && InteractionManager.Instance != null)
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
                    ChangeState(GameState.Comparing);
                    break;
                // Add future answers here
            }
        }

        public void ChangeState(GameState newState)
        {
            CurrentState = newState;
            Debug.Log($"GameManager: State changed to {newState}");

            switch (CurrentState)
            {
                case GameState.Init:
                    LevelDataManager.Instance.GenerateLevelData();
                    CalculateTotalBugs();
                    ChangeState(GameState.Sorting);
                    break;
                case GameState.Sorting:
                    CharacterSpawner.Instance.SpawnCharacters();
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
                case GameState.Comparing:
                    Debug.Log("GameManager: Ordering Enabled.");
                    break;
                // Other states will be handled later
            }
        }

        public void OnBugSorted()
        {
            if (CurrentState != GameState.Sorting) return;

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
