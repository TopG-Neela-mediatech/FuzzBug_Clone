using UnityEngine;

namespace TMKOC.FuzzBugClone
{
    public class FXManager : GenericSingleton<FXManager>
    {
        [Header("Particle Prefabs")]
        [SerializeField] private ParticleSystem _dropBugFX; // When bug dropped in correct jar
        [SerializeField] private ParticleSystem _countTapFX; // When tapping bug to count
        [SerializeField] private ParticleSystem _correctAnswerFX; // When selecting correct bug/jar in quiz

        public void PlayDropFX(Vector3 position, Color color)
        {
            PlayFX(_dropBugFX, position, color);
        }

        public void PlayCountFX(Vector3 position)
        {
            PlayFX(_countTapFX, position, Color.white);
        }

        public void PlayCorrectAnswerFX(Vector3 position)
        {
            PlayFX(_correctAnswerFX, position, Color.green); // Or specific color
        }

        private void PlayFX(ParticleSystem prefab, Vector3 position, Color color)
        {
            if (prefab != null)
            {
                ParticleSystem instance = Instantiate(prefab, position, Quaternion.identity);
                var main = instance.main;
                main.startColor = color; // Tint if applicable
                instance.Play();
                
                // Auto destroy
                Destroy(instance.gameObject, main.duration + main.startLifetime.constantMax);
            }
        }
    }
}
