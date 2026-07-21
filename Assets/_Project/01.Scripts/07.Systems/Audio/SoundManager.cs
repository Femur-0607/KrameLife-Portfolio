using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace TownLife.Systems
{
    public sealed class SoundManager : MonoBehaviour
    {
        public const string MasterVolumePrefsKey = "Audio.MasterVolume";
        public const string BgmVolumePrefsKey = "Audio.BgmVolume";
        public const string SfxVolumePrefsKey = "Audio.SfxVolume";
        public const string UiVolumePrefsKey = "Audio.UiVolume";

        private const float MutedDecibel = -80f;
        private const float DefaultVolume = 1f;

        [Header("Database")]
        [SerializeField]
        [Tooltip("sound.* ID로 SoundCueSO를 찾는 사운드 데이터베이스입니다.")]
        private SoundDatabase soundDatabase;

        [Header("Mixer")]
        [SerializeField]
        [Tooltip("Master, BGM, SFX, UI 그룹이 설정된 Audio Mixer입니다.")]
        private AudioMixer audioMixer;

        [SerializeField]
        [Tooltip("BGM AudioSource가 출력할 Mixer Group입니다.")]
        private AudioMixerGroup bgmMixerGroup;

        [SerializeField]
        [Tooltip("SFX AudioSource가 출력할 Mixer Group입니다.")]
        private AudioMixerGroup sfxMixerGroup;

        [SerializeField]
        [Tooltip("UI AudioSource가 출력할 Mixer Group입니다.")]
        private AudioMixerGroup uiMixerGroup;

        [Header("Exposed Parameters")]
        [SerializeField]
        [Tooltip("Master 그룹 볼륨 Exposed Parameter 이름입니다.")]
        private string masterVolumeParameter = "MasterVolume";

        [SerializeField]
        [Tooltip("BGM 그룹 볼륨 Exposed Parameter 이름입니다.")]
        private string bgmVolumeParameter = "BgmVolume";

        [SerializeField]
        [Tooltip("SFX 그룹 볼륨 Exposed Parameter 이름입니다.")]
        private string sfxVolumeParameter = "SfxVolume";

        [SerializeField]
        [Tooltip("UI 그룹 볼륨 Exposed Parameter 이름입니다.")]
        private string uiVolumeParameter = "UiVolume";

        [Header("Sources")]
        [SerializeField]
        [Tooltip("BGM 재생용 AudioSource입니다. 비어 있으면 런타임에 생성합니다.")]
        private AudioSource bgmSource;

        [SerializeField]
        [Tooltip("SFX 재생용 AudioSource입니다. 비어 있으면 런타임에 생성합니다.")]
        private AudioSource sfxSource;

        [SerializeField]
        [Tooltip("UI 사운드 재생용 AudioSource입니다. 비어 있으면 런타임에 생성합니다.")]
        private AudioSource uiSource;

        /// <summary>
        /// 현재 씬에서 유지되는 전역 SoundManager 인스턴스입니다.
        /// </summary>
        public static SoundManager Instance { get; private set; }

        private Coroutine bgmFadeCoroutine;
        private float masterVolume = DefaultVolume;
        private float bgmVolume = DefaultVolume;
        private float sfxVolume = DefaultVolume;
        private float uiVolume = DefaultVolume;
        private readonly Dictionary<string, float> lastPlayedTimes = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureAudioSources();
            LoadVolumes();
            ApplyAllVolumes();
        }

        /// <summary>
        /// soundId에 해당하는 사운드를 채널 설정에 맞게 재생합니다.
        /// </summary>
        public void Play(string soundId)
        {
            SoundCueSO cue = ResolveCue(soundId);

            if (cue == null || !CanPlay(cue))
            {
                return;
            }

            switch (cue.Channel)
            {
                case SoundChannel.Bgm:
                    PlayBgmCue(cue);
                    break;
                case SoundChannel.Sfx:
                    PlaySfxCue(cue);
                    break;
                case SoundChannel.Ui:
                    PlayUiCue(cue);
                    break;
                default:
                    Debug.LogWarning($"지원하지 않는 사운드 채널입니다. id={cue.Id}, channel={cue.Channel}", this);
                    break;
            }
        }

        /// <summary>
        /// soundId에 해당하는 BGM을 즉시 재생합니다.
        /// </summary>
        public void PlayBgm(string soundId)
        {
            SoundCueSO cue = ResolveCue(soundId);

            if (cue == null || !CanPlay(cue))
            {
                return;
            }

            PlayBgmCue(cue);
        }

        /// <summary>
        /// soundId에 해당하는 BGM으로 페이드 전환합니다.
        /// </summary>
        public void FadeToBgm(string soundId, float duration)
        {
            SoundCueSO cue = ResolveCue(soundId);

            if (cue == null || !CanPlay(cue))
            {
                return;
            }

            FadeToBgmCue(cue, duration);
        }

        /// <summary>
        /// BGM을 즉시 재생합니다.
        /// </summary>
        public void PlayBgm(AudioClip clip, bool loop = true)
        {
            if (clip == null)
            {
                return;
            }

            if (bgmFadeCoroutine != null)
            {
                StopCoroutine(bgmFadeCoroutine);
                bgmFadeCoroutine = null;
            }

            bgmSource.clip = clip;
            bgmSource.loop = loop;
            bgmSource.volume = 1f;
            bgmSource.Play();
        }

        /// <summary>
        /// BGM을 정지합니다.
        /// </summary>
        public void StopBgm()
        {
            if (bgmFadeCoroutine != null)
            {
                StopCoroutine(bgmFadeCoroutine);
                bgmFadeCoroutine = null;
            }

            bgmSource.Stop();
            bgmSource.clip = null;
            bgmSource.volume = 1f;
        }

        /// <summary>
        /// 현재 BGM을 페이드 아웃한 뒤 새 BGM을 페이드 인합니다.
        /// </summary>
        public void FadeToBgm(AudioClip clip, float duration, bool loop = true)
        {
            if (bgmFadeCoroutine != null)
            {
                StopCoroutine(bgmFadeCoroutine);
            }

            bgmFadeCoroutine = StartCoroutine(FadeToBgmCoroutine(clip, Mathf.Max(0f, duration), loop));
        }

        /// <summary>
        /// soundId에 해당하는 SFX를 한 번 재생합니다.
        /// </summary>
        public void PlaySfx(string soundId)
        {
            SoundCueSO cue = ResolveCue(soundId);

            if (cue == null || !CanPlay(cue))
            {
                return;
            }

            PlaySfxCue(cue);
        }

        /// <summary>
        /// SFX를 한 번 재생합니다.
        /// </summary>
        public void PlaySfx(AudioClip clip, float volumeScale = 1f)
        {
            PlayOneShot(sfxSource, clip, volumeScale);
        }

        /// <summary>
        /// soundId에 해당하는 UI 사운드를 한 번 재생합니다.
        /// </summary>
        public void PlayUi(string soundId)
        {
            SoundCueSO cue = ResolveCue(soundId);

            if (cue == null || !CanPlay(cue))
            {
                return;
            }

            PlayUiCue(cue);
        }

        /// <summary>
        /// UI 사운드를 한 번 재생합니다.
        /// </summary>
        public void PlayUi(AudioClip clip, float volumeScale = 1f)
        {
            PlayOneShot(uiSource, clip, volumeScale);
        }

        /// <summary>
        /// SFX 후보 중 하나를 골라 한 번 재생합니다.
        /// </summary>
        public void PlayRandomSfx(AudioClip[] clips, float volumeScale = 1f)
        {
            if (clips == null || clips.Length == 0)
            {
                return;
            }

            AudioClip clip = clips[Random.Range(0, clips.Length)];
            PlaySfx(clip, volumeScale);
        }

        /// <summary>
        /// Master 볼륨을 0~1 값으로 설정하고 저장합니다.
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            masterVolume = SetVolume(masterVolumeParameter, MasterVolumePrefsKey, volume);
        }

        /// <summary>
        /// BGM 볼륨을 0~1 값으로 설정하고 저장합니다.
        /// </summary>
        public void SetBgmVolume(float volume)
        {
            bgmVolume = SetVolume(bgmVolumeParameter, BgmVolumePrefsKey, volume);
        }

        /// <summary>
        /// SFX 볼륨을 0~1 값으로 설정하고 저장합니다.
        /// </summary>
        public void SetSfxVolume(float volume)
        {
            sfxVolume = SetVolume(sfxVolumeParameter, SfxVolumePrefsKey, volume);
        }

        /// <summary>
        /// UI 볼륨을 0~1 값으로 설정하고 저장합니다.
        /// </summary>
        public void SetUiVolume(float volume)
        {
            uiVolume = SetVolume(uiVolumeParameter, UiVolumePrefsKey, volume);
        }

        /// <summary>
        /// 현재 Master 볼륨 값입니다.
        /// </summary>
        public float MasterVolume => masterVolume;

        /// <summary>
        /// 현재 BGM 볼륨 값입니다.
        /// </summary>
        public float BgmVolume => bgmVolume;

        /// <summary>
        /// 현재 SFX 볼륨 값입니다.
        /// </summary>
        public float SfxVolume => sfxVolume;

        /// <summary>
        /// 현재 UI 볼륨 값입니다.
        /// </summary>
        public float UiVolume => uiVolume;

        /// <summary>
        /// 볼륨 값을 0~1 범위로 제한합니다.
        /// </summary>
        public static float ClampVolume(float volume)
        {
            return Mathf.Clamp01(volume);
        }

        /// <summary>
        /// 0~1 선형 볼륨 값을 Audio Mixer dB 값으로 변환합니다.
        /// </summary>
        public static float LinearVolumeToDecibel(float volume)
        {
            float clampedVolume = ClampVolume(volume);

            if (clampedVolume <= 0f)
            {
                return MutedDecibel;
            }

            return Mathf.Log10(clampedVolume) * 20f;
        }

        /// <summary>
        /// PlayerPrefs에 저장된 볼륨 값을 읽고 0~1 범위로 제한합니다.
        /// </summary>
        public static float LoadSavedVolume(string prefsKey, float defaultVolume)
        {
            return ClampVolume(PlayerPrefs.GetFloat(prefsKey, defaultVolume));
        }

        private void EnsureAudioSources()
        {
            bgmSource = EnsureAudioSource(bgmSource, "BGM Source", bgmMixerGroup);
            bgmSource.loop = true;

            sfxSource = EnsureAudioSource(sfxSource, "SFX Source", sfxMixerGroup);
            uiSource = EnsureAudioSource(uiSource, "UI Source", uiMixerGroup);
        }

        private AudioSource EnsureAudioSource(AudioSource source, string sourceName, AudioMixerGroup mixerGroup)
        {
            if (source == null)
            {
                GameObject sourceObject = new GameObject(sourceName);
                sourceObject.transform.SetParent(transform);
                source = sourceObject.AddComponent<AudioSource>();
            }

            source.playOnAwake = false;
            source.outputAudioMixerGroup = mixerGroup;
            return source;
        }

        private SoundCueSO ResolveCue(string soundId)
        {
            if (soundDatabase == null)
            {
                Debug.LogWarning($"{nameof(SoundDatabase)}가 연결되지 않았습니다. id={soundId}", this);
                return null;
            }

            return soundDatabase.Get(soundId);
        }

        private bool CanPlay(SoundCueSO cue)
        {
            if (cue.Clip == null)
            {
                Debug.LogWarning($"SoundCueSO에 AudioClip이 없습니다. id={cue.Id}", cue);
                return false;
            }

            if (cue.Cooldown <= 0f)
            {
                MarkPlayed(cue);
                return true;
            }

            if (lastPlayedTimes.TryGetValue(cue.Id, out float lastPlayedTime)
                && Time.unscaledTime - lastPlayedTime < cue.Cooldown)
            {
                return false;
            }

            MarkPlayed(cue);
            return true;
        }

        private void MarkPlayed(SoundCueSO cue)
        {
            lastPlayedTimes[cue.Id] = Time.unscaledTime;
        }

        private void PlayBgmCue(SoundCueSO cue)
        {
            PlayBgm(cue.Clip, cue.Loop);
            bgmSource.volume = ClampVolume(cue.VolumeScale);
        }

        private void FadeToBgmCue(SoundCueSO cue, float duration)
        {
            if (bgmFadeCoroutine != null)
            {
                StopCoroutine(bgmFadeCoroutine);
            }

            bgmFadeCoroutine = StartCoroutine(FadeToBgmCoroutine(cue.Clip, Mathf.Max(0f, duration), cue.Loop, cue.VolumeScale));
        }

        private void PlaySfxCue(SoundCueSO cue)
        {
            PlaySfx(cue.Clip, cue.VolumeScale);
        }

        private void PlayUiCue(SoundCueSO cue)
        {
            PlayUi(cue.Clip, cue.VolumeScale);
        }

        private void LoadVolumes()
        {
            masterVolume = LoadSavedVolume(MasterVolumePrefsKey, DefaultVolume);
            bgmVolume = LoadSavedVolume(BgmVolumePrefsKey, DefaultVolume);
            sfxVolume = LoadSavedVolume(SfxVolumePrefsKey, DefaultVolume);
            uiVolume = LoadSavedVolume(UiVolumePrefsKey, DefaultVolume);
        }

        private void ApplyAllVolumes()
        {
            ApplyMixerVolume(masterVolumeParameter, masterVolume);
            ApplyMixerVolume(bgmVolumeParameter, bgmVolume);
            ApplyMixerVolume(sfxVolumeParameter, sfxVolume);
            ApplyMixerVolume(uiVolumeParameter, uiVolume);
        }

        private float SetVolume(string parameterName, string prefsKey, float volume)
        {
            float clampedVolume = ClampVolume(volume);

            ApplyMixerVolume(parameterName, clampedVolume);
            PlayerPrefs.SetFloat(prefsKey, clampedVolume);
            PlayerPrefs.Save();

            return clampedVolume;
        }

        private void ApplyMixerVolume(string parameterName, float volume)
        {
            if (audioMixer == null || string.IsNullOrWhiteSpace(parameterName))
            {
                return;
            }

            audioMixer.SetFloat(parameterName, LinearVolumeToDecibel(volume));
        }

        private static void PlayOneShot(AudioSource source, AudioClip clip, float volumeScale)
        {
            if (source == null || clip == null)
            {
                return;
            }

            source.PlayOneShot(clip, ClampVolume(volumeScale));
        }

        private IEnumerator FadeToBgmCoroutine(AudioClip clip, float duration, bool loop)
        {
            return FadeToBgmCoroutine(clip, duration, loop, 1f);
        }

        private IEnumerator FadeToBgmCoroutine(AudioClip clip, float duration, bool loop, float targetVolume)
        {
            if (duration <= 0f)
            {
                PlayBgm(clip, loop);
                bgmSource.volume = ClampVolume(targetVolume);
                bgmFadeCoroutine = null;
                yield break;
            }

            float startVolume = bgmSource.volume;
            float clampedTargetVolume = ClampVolume(targetVolume);
            float halfDuration = duration * 0.5f;

            for (float time = 0f; time < halfDuration; time += Time.unscaledDeltaTime)
            {
                bgmSource.volume = Mathf.Lerp(startVolume, 0f, time / halfDuration);
                yield return null;
            }

            bgmSource.Stop();
            bgmSource.clip = clip;
            bgmSource.loop = loop;

            if (clip != null)
            {
                bgmSource.Play();
            }

            for (float time = 0f; time < halfDuration; time += Time.unscaledDeltaTime)
            {
                bgmSource.volume = Mathf.Lerp(0f, clampedTargetVolume, time / halfDuration);
                yield return null;
            }

            bgmSource.volume = clampedTargetVolume;
            bgmFadeCoroutine = null;
        }
    }
}
