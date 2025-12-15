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
                // Add others later
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

        public void SubmitAnswer_Bug(FuzzBugController bug)
        {
            if (!_isQuestionActive) return;
            // Implementation for Future Phases (Left/Right/etc)
            Debug.Log($"Submitted Bug Answer: {bug.name}");
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
