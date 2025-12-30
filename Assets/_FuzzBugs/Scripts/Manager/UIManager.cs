using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace TMKOC.FuzzBugClone
{
    public class UIManager : GenericSingleton<UIManager>
    {
        [SerializeField] private HorizontalLayoutGroup _jarsParent;

        [Header("Lives UI")]
        [SerializeField] private GameObject[] _lifeIcons; // Array of 3 hearts/lives objects
        [SerializeField] private GameObject _livesContainer;
        [SerializeField] private GameObject _endPanel;
        [SerializeField] private Transform _jarSlotsContainer;

        public Transform JarSlotsContainer => _jarSlotsContainer;

        [Header("Visual Cues")]
        [SerializeField] private RectTransform _visualCueBanner;
        [SerializeField] private TextMeshProUGUI _visualCueText;
        [SerializeField] private float _cueEntryY = -150f;
        [SerializeField] private float _cueExitY = 150f;
        [SerializeField] private float _cueAnimDuration = 0.5f;

        public void UpdateLives(int currentLives)
        {
            if (_livesContainer != null) _livesContainer.SetActive(true);

            for (int i = 0; i < _lifeIcons.Length; i++)
            {
                if (_lifeIcons[i] != null)
                {
                    _lifeIcons[i].SetActive(i < currentLives);
                }
            }
        }

        public void ShowVisualCue(string text)
        {   
            if (_visualCueBanner == null) return;

            if (_visualCueText != null)
                _visualCueText.text = text;

            _visualCueBanner.DOAnchorPosY(_cueEntryY, _cueAnimDuration).SetEase(Ease.OutBack);
        }

        public void HideVisualCue()
        {
             if (_visualCueBanner == null) return;
             _visualCueBanner.DOAnchorPosY(_cueExitY, _cueAnimDuration).SetEase(Ease.InBack);
        }

        public void ShowEndPanel(bool show)
        {
            if (_endPanel != null)
                _endPanel.SetActive(show);
        }

        public void EnableJarSlots(bool enable)
        {
            if (_jarSlotsContainer != null)
                _jarSlotsContainer.gameObject.SetActive(enable);
        }

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
                    ShowVisualCue("Welcome to FuzzBugs!");
                    break;
                case GameState.ColorSorting:
                    ShowVisualCue("Sort the bugs by Color!");
                    break;
                case GameState.Counting:
                    ShowVisualCue("Tap on a jar to count the bugs!");
                    break;
                case GameState.FindLeast:
                    ShowVisualCue("Tap the jar with the LEAST bugs!");
                    break;
                case GameState.FindMost:
                    ShowVisualCue("Tap the jar with the MOST bugs!");
                    break;
                case GameState.CountSorting:
                    ShowVisualCue("Order the Jars by Count!");
                    ToggleJarsLayoutGroup(false);
                    break;
                case GameState.SelectJarToQuiz:
                    ShowVisualCue("Select a Jar to Start Quiz!");
                    break;
                case GameState.Graphing:
                    break;
                case GameState.FindLeft:
                    ShowVisualCue("Tap the bug on the LEFT!");
                    break;
                case GameState.FindRight:
                    ShowVisualCue("Tap the bug on the RIGHT!");
                    break;
                case GameState.FindTop:
                    ShowVisualCue("Tap the bug on the TOP!");
                    break;
                case GameState.FindBottom:
                    ShowVisualCue("Tap the bug on the BOTTOM!");
                    break;
                case GameState.FindLargest:
                    ShowVisualCue("Tap the LARGEST bug!");
                    break;
                case GameState.FindSmallest:
                    ShowVisualCue("Tap the SMALLEST bug!");
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
