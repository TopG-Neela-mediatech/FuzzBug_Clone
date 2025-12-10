using UnityEngine;

namespace TMKOC.FuzzBugClone
{
    public enum GameState
    {
        Init,
        Counting,
        Sorting,
        Comparing,
        Graphing
    }

    public class GameManager : GenericSingleton<GameManager>
    {
        public GameState CurrentState { get; private set; }

        private void Start()
        {
            ChangeState(GameState.Init);
        }

        public void ChangeState(GameState newState)
        {
            CurrentState = newState;
            Debug.Log($"GameManager: State changed to {newState}");

            switch (CurrentState)
            {
                case GameState.Init:
                    LevelDataManager.Instance.GenerateLevelData();
                    ChangeState(GameState.Counting); // Auto-progress for now
                    break;
                case GameState.Counting:
                    CharacterSpawner.Instance.SpawnCharacters();
                    break;
                // Other states will be handled later
            }
        }
    }
}
