using UnityEngine;
using System.Collections.Generic;

namespace TMKOC.FuzzBugClone
{
    public enum BugColorType
    {
        Red,
        Blue,
        Green,
        Yellow
    }

    public class LevelDataManager : GenericSingleton<LevelDataManager>
    {
        private Dictionary<BugColorType, int> _levelCounts = new Dictionary<BugColorType, int>();

        public void GenerateLevelData()
        {
            _levelCounts.Clear();
            List<int> uniqueCounts = new List<int>();

            // Generate unique counts for each color type
            foreach (BugColorType color in System.Enum.GetValues(typeof(BugColorType)))
            {
                int count;
                do
                {
                    count = Random.Range(3, 9); // Random count between 3 and 8
                } while (uniqueCounts.Contains(count));

                uniqueCounts.Add(count);
                _levelCounts[color] = count;
                Debug.Log($"LevelData: {color} will have {count} bugs.");
            }
        }

        public int GetCountForColor(BugColorType color)
        {
            if (_levelCounts.ContainsKey(color))
            {
                return _levelCounts[color];
            }
            return 0;
        }
    }
}
