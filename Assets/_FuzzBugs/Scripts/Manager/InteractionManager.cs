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
        public event Action OnQuestionCorrect;
        public event Action OnQuestionIncorrect;

        private QuestionType _currentQuestion;
        private bool _isQuestionActive = false;

        [Header("Audio Delays")]
        [SerializeField] private float _celebrationDelay = 1.0f;
        [SerializeField] private float _transitionDelay = 0.5f;

        public void PlayQuestion(QuestionType type)
        {
            _currentQuestion = type;
            _isQuestionActive = true;
            Debug.Log($"InteractionManager: Playing Question - {type}");

            // Map QuestionType to FuzzBugAudio
            FuzzBugAudio audioType = GetAudioForQuestion(type);

            // Play audio with completion callback to unblock interactions
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayVoice(
                    audioType,
                    onComplete: () =>
                    {
                        // Unblock interactions after question audio completes
                        if (GameManager.Instance != null)
                        {
                            GameManager.Instance.BlockInteractions(false);
                        }
                    },
                    blockInteractions: false // GameManager already blocked it
                );
            }
            else
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.BlockInteractions(false);
                }
            }

        }

        public void SubmitAnswer_Jar(JarController jar)
        {
            if (!_isQuestionActive) return;

            // Check if interactions are blocked
            if (GameManager.Instance != null && GameManager.Instance.AreInteractionsBlocked)
            {
                Debug.Log("InteractionManager: Interactions blocked, ignoring jar tap.");
                return;
            }

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

        public void SubmitAnswer_Bug(BugCharacterController bug)
        {
            if (!_isQuestionActive) return;

            // Check if interactions are blocked
            if (GameManager.Instance != null && GameManager.Instance.AreInteractionsBlocked)
            {
                Debug.Log("InteractionManager: Interactions blocked, ignoring bug tap.");
                return;
            }

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

                // Animate correct bug
                bug.PlayDance();

                // Play celebration SFX
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX(FuzzBugAudio.Feedback_Correct);
                }

                // Play FX
                if (FXManager.Instance != null)
                    FXManager.Instance.PlayCorrectAnswerFX(bug.transform.position);

                _isQuestionActive = false;

                // Delay before triggering state transition
                StartCoroutine(DelayedCallback(_transitionDelay, () => OnQuestionCorrect?.Invoke()));
            }
            else
            {
                Debug.Log("<color=red>Incorrect Bug Tapped!</color>");

                // Play incorrect SFX
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX(FuzzBugAudio.Feedback_Incorrect);
                }

                OnQuestionIncorrect?.Invoke();
            }
        }

        private bool CheckLeft(BugCharacterController clickedBug)
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

        private bool CheckRight(BugCharacterController clickedBug)
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

        private bool CheckTop(BugCharacterController clickedBug)
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

        private bool CheckBottom(BugCharacterController clickedBug)
        {
            Transform container = clickedBug.transform.parent;
            if (container == null) return false;

            float minY = float.MaxValue;
            Transform bottomMost = null;

            foreach (Transform t in container)
            {
                // Ignore bugs that have been moved off-screen (Vertical Limit feature)
                // Assuming off-screen is significantly lower than visible area (e.g., < -500, user set to -800)
                // Using localPosition or position?
                // The bugs are moved using DOLocalJump to a local Y of ~-800.
                // t.position is World Space. We should check localPosition if threshold is local,
                // or just verify if it's within a reasonable visible range.
                // The container is centered? Let's check localPosition.y.
                if (t.localPosition.y < -400f) continue;

                if (t.position.y < minY)
                {
                    minY = t.position.y;
                    bottomMost = t;
                }
            }
            return bottomMost == clickedBug.transform;
        }

        private bool CheckLargest(BugCharacterController clickedBug)
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

        private bool CheckSmallest(BugCharacterController clickedBug)
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
            foreach (BugColorType color in System.Enum.GetValues(typeof(BugColorType)))
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
            foreach (BugColorType color in System.Enum.GetValues(typeof(BugColorType)))
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

                // Play celebration audio
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX(FuzzBugAudio.Feedback_Correct);
                }

                // Play FX
                if (FXManager.Instance != null && jar != null)
                    FXManager.Instance.PlayCorrectAnswerFX(jar.transform.position);

                _isQuestionActive = false;

                // Delay before triggering state transition
                StartCoroutine(DelayedCallback(_celebrationDelay, () => OnQuestionCorrect?.Invoke()));
            }
            else
            {
                Debug.Log("<color=red>Incorrect Answer!</color>");

                // Play incorrect SFX
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX(FuzzBugAudio.Feedback_Incorrect);
                }

                OnQuestionIncorrect?.Invoke();
            }
        }

        private System.Collections.IEnumerator DelayedCallback(float delay, System.Action callback)
        {
            yield return new WaitForSeconds(delay);
            callback?.Invoke();
        }

        private FuzzBugAudio GetAudioForQuestion(QuestionType type)
        {
            switch (type)
            {
                case QuestionType.FindLeast: return FuzzBugAudio.Question_FindLeast;
                case QuestionType.FindMost: return FuzzBugAudio.Question_FindMost;
                case QuestionType.FindLeft: return FuzzBugAudio.Question_FindLeft;
                case QuestionType.FindRight: return FuzzBugAudio.Question_FindRight;
                case QuestionType.FindTop: return FuzzBugAudio.Question_FindTop;
                case QuestionType.FindBottom: return FuzzBugAudio.Question_FindBottom;
                case QuestionType.FindLargest: return FuzzBugAudio.Question_FindLargest;
                case QuestionType.FindSmallest: return FuzzBugAudio.Question_FindSmallest;
                default: return FuzzBugAudio.Question_FindLeft;
            }
        }

    }
}

