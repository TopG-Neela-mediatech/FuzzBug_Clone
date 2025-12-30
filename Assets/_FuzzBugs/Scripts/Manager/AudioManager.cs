using System;
using System.Collections;
using UnityEngine;

namespace TMKOC.FuzzBugClone
{
    public class AudioManager : GenericSingleton<AudioManager>
    {
        [Header("Settings")]
        [SerializeField] private bool _isMute = false;
        [SerializeField] private float _shortDelay = 0.25f;
        [SerializeField] private float _mediumDelay = 0.5f;

        [Header("Audio Source")]
        [SerializeField] private AudioSO _audioSO;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource _bgSource;
        [SerializeField] private AudioSource _voiceSource;
        [SerializeField] private AudioSource _sfxSource;

        private Coroutine _voiceCoroutine;
        private Coroutine _completionCoroutine;

        protected override void Awake()
        {
            base.Awake();
            PlayBGMusic();
        }

        #region Public API

        /// <summary>
        /// Plays a voice audio clip and optionally calls a callback when complete.
        /// Blocks interactions via GameManager if specified.
        /// </summary>
        /// <param name="audioType">Type of audio to play</param>
        /// <param name="onComplete">Callback to invoke when audio finishes</param>
        /// <param name="blockInteractions">Whether to block game interactions during playback</param>
        /// <param name="index">Specific clip index, or -1 for random</param>
        /// <returns>Audio clip length in seconds, or NaN if muted/not found</returns>
        public float PlayVoice(FuzzBugAudio audioType, Action onComplete = null, bool blockInteractions = false, int index = -1)
        {
            if (_isMute || _audioSO == null)
            {
                onComplete?.Invoke();
                return float.NaN;
            }

            AudioClip clip = index == -1
                ? _audioSO.GetRandomAudioClip(audioType)
                : _audioSO.GetSpecificAudioClip(audioType, index);

            if (clip == null)
            {
                Debug.LogWarning($"AudioManager: No clip found for {audioType}");
                onComplete?.Invoke();
                return float.NaN;
            }

            // Stop any existing voice playback
            if (_voiceCoroutine != null) StopCoroutine(_voiceCoroutine);
            if (_completionCoroutine != null) StopCoroutine(_completionCoroutine);

            // Block interactions if requested
            if (blockInteractions && GameManager.Instance != null)
            {
                GameManager.Instance.BlockInteractions(true);
            }

            _voiceSource.clip = clip;
            _voiceSource.Play();

            // Start completion tracking
            if (onComplete != null || blockInteractions)
            {
                _completionCoroutine = StartCoroutine(WaitForAudioCompletion(clip.length, onComplete, blockInteractions));
            }

            return clip.length;
        }

        /// <summary>
        /// Plays a voice audio clip after a delay.
        /// </summary>
        public void PlayVoiceDelayed(FuzzBugAudio audioType, float delay, Action onComplete = null, bool blockInteractions = false, int index = -1)
        {
            if (_voiceCoroutine != null) StopCoroutine(_voiceCoroutine);
            _voiceCoroutine = StartCoroutine(PlayVoiceDelayedCoroutine(audioType, delay, onComplete, blockInteractions, index));
        }

        /// <summary>
        /// Plays a sequence of audio clips one after another.
        /// </summary>
        public void PlayVoiceSequence(FuzzBugAudio[] sequence, Action onComplete = null, bool blockInteractions = false)
        {
            if (_voiceCoroutine != null) StopCoroutine(_voiceCoroutine);
            _voiceCoroutine = StartCoroutine(PlaySequenceCoroutine(sequence, onComplete, blockInteractions));
        }

        /// <summary>
        /// Plays a one-shot sound effect (does not block or track completion).
        /// </summary>
        public void PlaySFX(FuzzBugAudio audioType, int index = -1)
        {
            if (_isMute || _audioSO == null) return;

            AudioClip clip = index == -1
                ? _audioSO.GetRandomAudioClip(audioType)
                : _audioSO.GetSpecificAudioClip(audioType, index);

            if (clip != null)
            {
                _sfxSource.PlayOneShot(clip);
            }
        }

        /// <summary>
        /// Stops all currently playing audio.
        /// </summary>
        public void StopAllAudio()
        {
            _bgSource.Stop();
            _voiceSource.Stop();
            _sfxSource.Stop();

            if (_voiceCoroutine != null)
            {
                StopCoroutine(_voiceCoroutine);
                _voiceCoroutine = null;
            }

            if (_completionCoroutine != null)
            {
                StopCoroutine(_completionCoroutine);
                _completionCoroutine = null;
            }

            // Unblock interactions if they were blocked
            if (GameManager.Instance != null)
            {
                GameManager.Instance.BlockInteractions(false);
            }
        }

        /// <summary>
        /// Checks if voice audio is currently playing.
        /// </summary>
        public bool IsVoicePlaying => _voiceSource.isPlaying;

        #endregion

        #region Background Music

        public void PlayBGMusic()
        {
            if (!_isMute && _audioSO != null)
            {
                var bgClip = _audioSO.GetRandomAudioClip(FuzzBugAudio.BGMusic);

                if (bgClip != null)
                {
                    _bgSource.clip = bgClip;
                    _bgSource.loop = true;
                    _bgSource.Play();
                }
            }
        }

        public void StopBGMusic()
        {
            _bgSource.Stop();
        }

        #endregion

        #region Coroutines

        private IEnumerator PlayVoiceDelayedCoroutine(FuzzBugAudio audioType, float delay, Action onComplete, bool blockInteractions, int index)
        {
            yield return new WaitForSeconds(delay);
            PlayVoice(audioType, onComplete, blockInteractions, index);
        }

        private IEnumerator PlaySequenceCoroutine(FuzzBugAudio[] sequence, Action onComplete, bool blockInteractions)
        {
            if (blockInteractions && GameManager.Instance != null)
            {
                GameManager.Instance.BlockInteractions(true);
            }

            foreach (var audioType in sequence)
            {
                if (_isMute || _audioSO == null) break;

                AudioClip clip = _audioSO.GetRandomAudioClip(audioType);
                if (clip != null)
                {
                    _voiceSource.clip = clip;
                    _voiceSource.Play();
                    yield return new WaitForSeconds(clip.length);
                }
            }

            if (blockInteractions && GameManager.Instance != null)
            {
                GameManager.Instance.BlockInteractions(false);
            }

            onComplete?.Invoke();
        }

        private IEnumerator WaitForAudioCompletion(float duration, Action onComplete, bool wasBlocking)
        {
            yield return new WaitForSeconds(duration);

            // Unblock interactions if they were blocked
            if (wasBlocking && GameManager.Instance != null)
            {
                GameManager.Instance.BlockInteractions(false);
            }

            onComplete?.Invoke();
        }

        #endregion

        #region Utility

        public void SetMute(bool mute)
        {
            _isMute = mute;
            if (_isMute)
            {
                StopAllAudio();
            }
            else
            {
                PlayBGMusic();
            }
        }

        #endregion
    }
}
