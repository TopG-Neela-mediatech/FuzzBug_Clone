using UnityEngine;

using System.Collections.Generic;

namespace TMKOC.FuzzBugClone
{
    [System.Serializable]
    public struct BugConfig
    {
        public BugColorType color;
        public Sprite sprite;
    }

    public class CharacterSpawner : GenericSingleton<CharacterSpawner>
    {
        [SerializeField] private CharacterController _characterPrefab;
        [SerializeField] private RectTransform _spawnArea;
        [SerializeField] private float _moveSpeed = 100f;
        [SerializeField] private List<BugConfig> _bugConfigs;

        public void SpawnCharacters()
        {
            if (_characterPrefab == null || _spawnArea == null)
            {
                Debug.LogError("CharacterSpawner: Prefab or SpawnArea is missing!");
                return;
            }

            // 1. Build a list of all bugs to spawn
            List<BugColorType> spawnQueue = new List<BugColorType>();

            foreach (var config in _bugConfigs)
            {
                int count = LevelDataManager.Instance.GetCountForColor(config.color);
                for (int i = 0; i < count; i++)
                {
                    spawnQueue.Add(config.color);
                }
            }

            // 2. Shuffle the list
            Shuffle(spawnQueue);

            // 3. Spawn them
            foreach (var colorType in spawnQueue)
            {
                Sprite spriteToUse = GetSpriteForColor(colorType);
                SpawnSingleCharacter(colorType, spriteToUse);
            }
        }

        private void SpawnSingleCharacter(BugColorType colorType, Sprite sprite)
        {
            var newCharacter = Instantiate(_characterPrefab, _spawnArea);

            // Random position within the spawn area (X only)
            float randomX = Random.Range(-_spawnArea.rect.width / 2f, _spawnArea.rect.width / 2f);

            // Keep Y at 0 (center of SpawnArea) as requested
            newCharacter.RectTransform.anchoredPosition = new Vector2(randomX, 0f);

            // Randomize direction: Left or Right
            Vector2 randomDirection = (Random.value > 0.5f) ? Vector2.right : Vector2.left;
            newCharacter.Initialize(randomDirection, _moveSpeed, sprite, colorType);
        }

        private Sprite GetSpriteForColor(BugColorType color)
        {
            foreach (var config in _bugConfigs)
            {
                if (config.color == color) return config.sprite;
            }
            return null;
        }

        private void Shuffle<T>(List<T> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                T temp = list[i];
                int randomIndex = Random.Range(i, list.Count);
                list[i] = list[randomIndex];
                list[randomIndex] = temp;
            }
        }
    }
}
