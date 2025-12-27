using UnityEngine;
using System;

namespace TMKOC.FuzzBugClone
{
    public enum QuestionType
    {
        FindLeast,
        FindMost,
        FindLeft,
        FindRight,
        FindTop,
        FindBottom,
        FindLargest,
        FindSmallest
    }

    public class InteractionManager : GenericSingleton<InteractionManager>
    {
        [Header("Audio (Placeholders)")]
        [SerializeField] private AudioClip _audioLeast;
        [SerializeField] private AudioClip _audioMost;
        [SerializeField] private AudioClip _audioYay;
        [SerializeField] private AudioClip _audioNay;

        public event Action OnQuestionCorrect;
        public event Action OnQuestionIncorrect;

        private QuestionType _currentQuestion;
        private bool _isQuestionActive = false;

        public void PlayQuestion(QuestionType type)
        {
            _currentQuestion = type;
            _isQuestionActive = true;
            Debug.Log($"InteractionManager: Playing Question - {type}");

            // Prompt Logic (since no audio)
            string promptText = "";
            switch (type)
            {
                case QuestionType.FindLeast:
                    promptText = ">>> QUESTION: Tap the Jar with the LEAST number of Fuzz Bugs! <<<";
                    break;
                case QuestionType.FindMost:
                    promptText = ">>> QUESTION: Tap the Jar with the MOST number of Fuzz Bugs! <<<";
                    break;
                case QuestionType.FindLeft:
                    promptText = ">>> QUESTION: Tap the Fuzz Bug on the LEFT! <<<";
                    break;
                case QuestionType.FindRight:
                    promptText = ">>> QUESTION: Tap the Fuzz Bug on the RIGHT! <<<";
                    break;
                case QuestionType.FindTop:
                    promptText = ">>> QUESTION: Tap the Fuzz Bug on the TOP! <<<";
                    break;
                case QuestionType.FindBottom:
                    promptText = ">>> QUESTION: Tap the Fuzz Bug on the BOTTOM! <<<";
                    break;
                case QuestionType.FindLargest:
                    promptText = ">>> QUESTION: Tap the LARGEST Fuzz Bug! <<<";
                    break;
                case QuestionType.FindSmallest:
                    promptText = ">>> QUESTION: Tap the SMALLEST Fuzz Bug! <<<";
                    break;
            }
            if (!string.IsNullOrEmpty(promptText)) Debug.Log($"<color=yellow>{promptText}</color>");

            // TODO: Play actual Audio Clip based on type
        }

        public void SubmitAnswer_Jar(JarController jar)
        {
            if (!_isQuestionActive) return;

            bool isCorrect = false;

            switch (_currentQuestion)
            {
                case QuestionType.FindLeast:
                    isCorrect = CheckLeast(jar);
                    break;
                case QuestionType.FindMost:
                    isCorrect = CheckMost(jar);
                    break;
                default:
                    Debug.LogWarning("SubmitAnswer_Jar called for non-jar question.");
                    break;
            }

            HandleResult(isCorrect, jar);
        }

        public void SubmitAnswer_Bug(CharacterController bug)
        {
            if (!_isQuestionActive) return;
            
            bool isCorrect = false;
            switch (_currentQuestion)
            {
                case QuestionType.FindLeft:
                    isCorrect = CheckLeft(bug);
                    break;
                case QuestionType.FindRight:
                    isCorrect = CheckRight(bug);
                    break;
                case QuestionType.FindTop:
                    isCorrect = CheckTop(bug);
                    break;
                case QuestionType.FindBottom:
                    isCorrect = CheckBottom(bug);
                    break;
                case QuestionType.FindLargest:
                    isCorrect = CheckLargest(bug);
                    break;
                case QuestionType.FindSmallest:
                    isCorrect = CheckSmallest(bug);
                    break;
                default:
                    Debug.LogWarning("SubmitAnswer_Bug called for non-bug question type.");
                    break;
            }

            if (isCorrect)
            {
                Debug.Log("<color=green>Correct Bug Tapped!</color>");
                // Animate correct bug?
                bug.PlayDance();
                _isQuestionActive = false;
                OnQuestionCorrect?.Invoke();
            }
            else
            {
                Debug.Log("<color=red>Incorrect Bug Tapped!</color>");
                OnQuestionIncorrect?.Invoke();
            }
        }

        private bool CheckLeft(CharacterController clickedBug)
        {
            // Find the bug with minimum X (world position) in the active jar
            // We need access to the active jar's bugs. 
            // InteractionManager doesn't track active jar directly, GameManager does.
            // Or we check all bugs in the clicked bug's parent? 
            // The clicked bug is in the active jar's container.
            
            Transform container = clickedBug.transform.parent;
            if (container == null) return false;

            float minX = float.MaxValue;
            Transform leftMost = null;

            foreach (Transform t in container)
            {
                if (t.position.x < minX)
                {
                    minX = t.position.x;
                    leftMost = t;
                }
            }

            return leftMost == clickedBug.transform;
        }

        private bool CheckRight(CharacterController clickedBug)
        {
            Transform container = clickedBug.transform.parent;
            if (container == null) return false;

            float maxX = float.MinValue;
            Transform rightMost = null;

            foreach (Transform t in container)
            {
                if (t.position.x > maxX)
                {
                    maxX = t.position.x;
                    rightMost = t;
                }
            }

            return rightMost == clickedBug.transform;
        }

        private bool CheckTop(CharacterController clickedBug)
        {
            Transform container = clickedBug.transform.parent;
            if (container == null) return false;

            float maxY = float.MinValue;
            Transform topMost = null;

            foreach (Transform t in container)
            {
                if (t.position.y > maxY)
                {
                    maxY = t.position.y;
                    topMost = t;
                }
            }
            return topMost == clickedBug.transform;
        }

        private bool CheckBottom(CharacterController clickedBug)
        {
            Transform container = clickedBug.transform.parent;
            if (container == null) return false;

            float minY = float.MaxValue;
            Transform bottomMost = null;

            foreach (Transform t in container)
            {
                if (t.position.y < minY)
                {
                    minY = t.position.y;
                    bottomMost = t;
                }
            }
            return bottomMost == clickedBug.transform;
        }

        private bool CheckLargest(CharacterController clickedBug)
        {
            Transform container = clickedBug.transform.parent;
            if (container == null) return false;

            float maxScale = float.MinValue;
            Transform largest = null;

            foreach (Transform t in container)
            {
                // Assuming uniform scale, check X
                float scale = t.localScale.x; 
                // Note: localScale might be negative if flipped, so take Abs
                scale = Mathf.Abs(scale);

                if (scale > maxScale)
                {
                    maxScale = scale;
                    largest = t;
                }
            }
            // Add tolerance?
            return largest == clickedBug.transform;
        }

        private bool CheckSmallest(CharacterController clickedBug)
        {
            Transform container = clickedBug.transform.parent;
            if (container == null) return false;

            float minScale = float.MaxValue;
            Transform smallest = null;

            foreach (Transform t in container)
            {
                float scale = Mathf.Abs(t.localScale.x);

                if (scale < minScale)
                {
                    minScale = scale;
                    smallest = t;
                }
            }
            return smallest == clickedBug.transform;
        }

        private bool CheckLeast(JarController jar)
        {
            // Logic to find if this jar has the least bugs compared to others
            // Only counting active jars? Or all Level Color types?
            // "Least" among the current jars using LevelDataManager
            
            int minVal = int.MaxValue;
            foreach(BugColorType color in System.Enum.GetValues(typeof(BugColorType)))
            {
                int count = LevelDataManager.Instance.GetCountForColor(color);
                if (count < minVal) minVal = count;
            }

            int jarCount = LevelDataManager.Instance.GetCountForColor(jar.JarColor);
            return jarCount == minVal;
        }

        private bool CheckMost(JarController jar)
        {
            int maxVal = int.MinValue;
            foreach(BugColorType color in System.Enum.GetValues(typeof(BugColorType)))
            {
                int count = LevelDataManager.Instance.GetCountForColor(color);
                if (count > maxVal) maxVal = count;
            }

            int jarCount = LevelDataManager.Instance.GetCountForColor(jar.JarColor);
            return jarCount == maxVal;
        }

        private void HandleResult(bool isCorrect, JarController jar = null)
        {
            if (isCorrect)
            {
                Debug.Log("<color=green>Correct Answer!</color>");
                
                // Play Celebration on Jar
                if (jar != null) jar.PlayCelebration();

                // Play Yay Audio
                
                _isQuestionActive = false; // Stop accepting answers
                OnQuestionCorrect?.Invoke();
            }
            else
            {
                Debug.Log("<color=red>Incorrect Answer!</color>");
                // Play Nay Audio
                OnQuestionIncorrect?.Invoke();
            }
        }
    }
}
