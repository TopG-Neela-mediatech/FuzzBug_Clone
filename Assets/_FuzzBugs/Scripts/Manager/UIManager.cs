using UnityEngine;
using UnityEngine.UI;

namespace TMKOC.FuzzBugClone
{
    public class UIManager : GenericSingleton<UIManager>
    {
        [SerializeField] private HorizontalLayoutGroup _jarsParent;


        private void OnEnable()
        {
            GameManager.OnStateChange += StateChangeActions;

        }

        private void OnDisable()
        {
            GameManager.OnStateChange -= StateChangeActions;

        }


        private void StateChangeActions(GameState gameState)
        {
            switch (gameState)
            {
                case GameState.Init:
                    break;
                case GameState.ColorSorting:
                    break;
                case GameState.Counting:
                    break;
                case GameState.FindLeast:
                    break;
                case GameState.FindMost:
                    break;
                case GameState.CountSorting:
                    ToggleJarsLayoutGroup(false);
                    break;
                case GameState.Graphing:
                    break;
                case GameState.FindLeft:
                    break;
                case GameState.FindRight:
                    break;
                case GameState.FindTop:
                    break;
                case GameState.FindBottom:
                    break;
                case GameState.FindLargest:
                    break;
                case GameState.FindSmallest:
                    break;
                case GameState.GameEnd:
                    break;
                default:
                    break;
            }
        }


        private void ToggleJarsLayoutGroup(bool toggle)
        {
            _jarsParent.enabled = toggle;
        }
    }
}
