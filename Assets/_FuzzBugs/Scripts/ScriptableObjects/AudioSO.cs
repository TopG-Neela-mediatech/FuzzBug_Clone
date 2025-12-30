using UnityEngine;

namespace TMKOC.FuzzBugClone
{
    public enum FuzzBugAudio
    {
        // Background Music
        BGMusic,

        // Question Audio - Jar Selection
        Question_FindLeast,         // "Which jar has the least number of bugs?"
        Question_FindMost,          // "Which jar has the most number of bugs?"

        // Instruction Audio
        Instruction_TapJarToCount,  // "Tap on a jar and count all its bugs"
        Instruction_TapJarToQuiz,   // "Tap on a jar to start the quiz"
        Instruction_DragJars,       // "Drag the jars to arrange them"

        // Question Audio - Spatial
        Question_FindLeft,          // "Tap the bug on the left"
        Question_FindRight,         // "Tap the bug on the right"
        Question_FindTop,           // "Tap the bug on top"
        Question_FindBottom,        // "Tap the bug on the bottom"

        // Question Audio - Size
        Question_FindLargest,       // "Tap the largest bug"
        Question_FindSmallest,      // "Tap the smallest bug"

        // Feedback Audio
        Feedback_Correct,           // Correct answer SFX/VO
        Feedback_Incorrect,         // Incorrect answer SFX/VO
        Feedback_Celebration,       // Celebration sound for correct answers

        // Transition Audio
        Transition_QuizStart,       // Short audio when quiz starts for a jar
        Transition_NextQuestion,    // Transition between questions

        // Game Events
        Event_GameEnd               // Game completion audio
    }

    [System.Serializable]
    public class AudioData
    {
        public FuzzBugAudio AudioType;
        public AudioClip[] Clip;
    }

    [CreateAssetMenu(fileName = "AudioSO", menuName = "TMKOC/FuzzBugClone/AudioSO")]
    public class AudioSO : ScriptableObject
    {
        public AudioData[] AudioList;

        public AudioClip GetRandomAudioClip(FuzzBugAudio audioType)
        {
            var data = GetAudioData(audioType);
            if (data == null || data.Clip.Length == 0) return null;

            return data.Clip[Random.Range(0, data.Clip.Length)];
        }

        public AudioClip GetSpecificAudioClip(FuzzBugAudio audioType, int index)
        {
            var data = GetAudioData(audioType);
            if (data == null || index < 0 || index >= data.Clip.Length) return null;

            return data.Clip[index];
        }

        private AudioData GetAudioData(FuzzBugAudio audioType)
        {
            foreach (var entry in AudioList)
                if (entry.AudioType == audioType)
                    return entry;

            Debug.LogWarning($"AudioData not found for: {audioType} in {name}");
            return null;
        }
    }
}
