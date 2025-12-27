using System.Collections;
using UnityEngine;

namespace TMKOC.FuzzBugClone
{
    public class JarsManager : GenericSingleton<JarsManager>
    {
        [SerializeField] private JarController[] _jarsList;

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
                    ToggleJarsRaycast(true);
                    break;
                case GameState.FindMost:
                    break;
                case GameState.CountSorting:
                    ToggleJarsRaycast(true); // Ensure raycasts are on for dragging
                    ToggleJarsDraggable(true);
                    break;
                case GameState.SelectJarToQuiz:
                    ToggleJarsDraggable(false);
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

        public void ToggleJarsRaycast(bool toggle)
        {
            foreach (var jar in _jarsList)
            {
                if (jar != null) jar.ToggleJarRaycast(toggle);
            }
        }

        public void ToggleJarsDraggable(bool toggle)
        {
            foreach (var jar in _jarsList)
            {
                if (jar != null)
                {
                    if (jar.TryGetComponent<Draggable>(out var draggable))
                    {
                        draggable.enabled = toggle;
                    }
                }
            }
        }
    }
}