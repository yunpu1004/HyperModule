using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HyperModule
{
    public class AudioManager : MonoBehaviour
    {
        public SerializableDictionary_StringAudioClip audioClips;

        private static float _soundVolume = 1f;
        /// <summary>버튼 클릭, 효과음 등 단발성 소리 볼륨</summary>
        public static float soundVolume
        {
            get => _soundVolume;
            set
            {
                float clamped = Mathf.Clamp01(value);
                if (Mathf.Approximately(_soundVolume, clamped)) return;

                float factor = (_soundVolume <= 0f) ? clamped : clamped / _soundVolume;
                _soundVolume = clamped;

                if (_instance != null)
                    _instance.ApplySoundVolumeToActives(factor);

            }
        }

        private static float _musicVolume = 1f;
        /// <summary>배경음악 등 길게 재생되는 소리 볼륨(변경 시 즉시 반영)</summary>
        public static float musicVolume
        {
            get => _musicVolume;
            set
            {
                _musicVolume = Mathf.Clamp01(value);
                if (_instance != null && _instance._musicSource != null)
                    _instance._musicSource.volume = _musicVolume * _instance._musicScale; // ★ 변경
            }
        }

        private static AudioManager _instance;
        private static AudioManager Instance
        {
            get
            {
                if (_instance == null) Create();
                return _instance;
            }
        }

        private AudioSource _musicSource;
        private readonly List<AudioSource> _activeSfxSources = new();
        private bool _wasPlayingBeforeChange;
        private float _bgmPosBeforeChange;

        // ★ 추가: 현재 재생 중인 BGM의 로컬 스케일(PlayMusic의 volume 파라미터)
        private float _musicScale = 1f;

        // ★ 추가: 각 음악의 마지막 재생 위치를 저장하는 Dictionary
        private readonly Dictionary<AudioClip, float> _musicPositions = new();
        private AudioClip _currentMusicClip;


        private static void Create()
        {
            if (_instance != null) return;
            var go = new GameObject("[AudioManager]");
            _instance = go.AddComponent<AudioManager>();
            _instance.Init();
        }

        private void Awake()
        {
            Init();
        }

        private void OnEnable()
        {
            AudioSettings.OnAudioConfigurationChanged += OnAudioConfigChanged;
        }

        private void OnDisable()
        {
            AudioSettings.OnAudioConfigurationChanged -= OnAudioConfigChanged;
        }

        private void Init()
        {
            if (_instance != null) return;
            _instance = this;
            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.loop = true;
            _musicSource.spatialBlend = 0f; // 2D
            _musicSource.volume = _musicVolume * _musicScale;
            DontDestroyOnLoad(gameObject);
        }

        // ★ 추가: 현재 재생 중인 음악의 위치를 저장
        private void SaveCurrentMusicPosition()
        {
            if (_currentMusicClip != null && _musicSource.isPlaying)
            {
                _musicPositions[_currentMusicClip] = _musicSource.time;
            }
        }

        // ★ 추가: 특정 음악의 저장된 위치를 가져옴
        private float GetSavedMusicPosition(AudioClip clip)
        {
            return _musicPositions.TryGetValue(clip, out float position) ? position : 0f;
        }

        // ★ 변경: factor를 받아 로컬 스케일 보존
        private void ApplySoundVolumeToActives(float factor)
        {
            for (int i = _activeSfxSources.Count - 1; i >= 0; i--)
            {
                var src = _activeSfxSources[i];
                if (src == null)
                {
                    _activeSfxSources.RemoveAt(i);
                    continue;
                }
                src.volume *= factor; // 기존 로컬 스케일 유지한 채 전역 볼륨만 반영
            }
        }

        // =========================
        //          MUSIC
        // =========================

        /// <summary>
        /// BGM 재생. musicVolume * volume 으로 최종 볼륨이 결정됩니다.
        /// resumeIfPossible이 true면 이전에 재생했던 위치부터 이어서 재생합니다.
        /// </summary>
        public static void PlayMusic(AudioClip clip, bool loop = true, float fadeDuration = 0f, float startTime = 0f, float volume = 1f, bool resumeIfPossible = false)
        {
            if (clip == null) return;

            var inst = Instance;
            
            // 같은 음악이 재생 중이고 resumeIfPossible이 true인 경우
            if (inst._currentMusicClip == clip && inst._musicSource.isPlaying && resumeIfPossible && startTime == 0f)
            {
                // 볼륨과 루프 설정만 업데이트하고 계속 재생
                inst._musicScale = Mathf.Clamp01(volume);
                inst._musicSource.loop = loop;
                inst._musicSource.volume = musicVolume * inst._musicScale;
                return;
            }
            
            inst.StopAllCoroutines();

            // 현재 재생 중인 음악의 위치를 저장
            inst.SaveCurrentMusicPosition();

            inst._musicScale = Mathf.Clamp01(volume); // ★ 로컬 스케일 저장

            inst._musicSource.clip = clip;
            inst._musicSource.loop = loop;
            
            // resumeIfPossible이 true이고 startTime이 0이면 저장된 위치에서 재생
            if (resumeIfPossible && startTime == 0f)
            {
                float savedPosition = inst.GetSavedMusicPosition(clip);
                inst._musicSource.time = Mathf.Clamp(savedPosition, 0f, clip.length);
            }
            else
            {
                inst._musicSource.time = Mathf.Clamp(startTime, 0f, clip.length);
            }

            // 현재 재생 중인 클립 업데이트
            inst._currentMusicClip = clip;

            if (fadeDuration > 0f)
            {
                inst.StartCoroutine(inst.Co_FadeInMusic(fadeDuration));
            }
            else
            {
                inst._musicSource.volume = musicVolume * inst._musicScale;
                inst._musicSource.Play();
            }
        }

        public static void PlayMusic(string clip, bool loop = true, float fadeDuration = 0f, float startTime = 0f, float volume = 1f, bool resumeIfPossible = false)
        {
            if (Instance.audioClips == null || !Instance.audioClips.TryGetValue(clip, out var audioClip))
            {
                QAUtil.LogWarning($"AudioManager: Clip '{clip}' not found.");
                return;
            }

            PlayMusic(audioClip, loop, fadeDuration, startTime, volume, resumeIfPossible);
        }

        public static void StopMusic(float fadeOutDuration = 0f)
        {
            var inst = Instance;
            inst.StopAllCoroutines();

            // 음악을 멈출 때 현재 위치 저장
            inst.SaveCurrentMusicPosition();

            if (fadeOutDuration <= 0f)
            {
                inst._musicSource.Stop();
                inst._currentMusicClip = null;
                return;
            }

            inst.StartCoroutine(inst.Co_FadeOutMusic(fadeOutDuration));
        }

        /// <summary>
        /// 특정 음악의 저장된 재생 위치를 초기화합니다.
        /// </summary>
        public static void ResetMusicPosition(AudioClip clip)
        {
            if (clip != null && Instance._musicPositions.ContainsKey(clip))
            {
                Instance._musicPositions.Remove(clip);
            }
        }

        /// <summary>
        /// 특정 음악의 저장된 재생 위치를 초기화합니다.
        /// </summary>
        public static void ResetMusicPosition(string clipName)
        {
            if (Instance.audioClips == null || !Instance.audioClips.TryGetValue(clipName, out var audioClip))
            {
                QAUtil.LogWarning($"AudioManager: Clip '{clipName}' not found.");
                return;
            }

            ResetMusicPosition(audioClip);
        }

        /// <summary>
        /// 모든 음악의 저장된 재생 위치를 초기화합니다.
        /// </summary>
        public static void ResetAllMusicPositions()
        {
            Instance._musicPositions.Clear();
        }

        private IEnumerator Co_FadeInMusic(float duration)
        {
            _musicSource.volume = 0f;
            _musicSource.Play();

            float target = musicVolume * _musicScale; // ★ 변경
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                _musicSource.volume = Mathf.Lerp(0f, target, t / duration);
                yield return null;
            }
            _musicSource.volume = target;
        }

        private IEnumerator Co_FadeOutMusic(float duration)
        {
            float start = _musicSource.volume;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                _musicSource.volume = Mathf.Lerp(start, 0f, t / duration);
                yield return null;
            }
            _musicSource.Stop();
            _musicSource.volume = musicVolume * _musicScale; // ★ 변경
            _currentMusicClip = null;
        }

        // =========================
        //          SFX
        // =========================

        /// <summary>
        /// 2D(화면 전체) 효과음. 호출 측에 AudioSource 필요 없음.
        /// 최종볼륨 = soundVolume * volume
        /// </summary>
        public static void PlaySound(AudioClip clip, float pitch = 1f, float volume = 1f)
        {
            if (clip == null) return;

            float finalVol = Mathf.Clamp01(soundVolume * Mathf.Clamp01(volume));

            var src = CreateTempSource("SFX_2D", Instance.transform, Vector3.zero);
            src.spatialBlend = 0f;
            src.pitch = pitch;
            src.volume = finalVol; // src.volume 자체에 최종 볼륨 기록(전역 볼륨 변경 시 factor로 조정됨)
            src.PlayOneShot(clip, finalVol);

            Instance.RegisterAndAutoDestroy(src, clip.length / Mathf.Max(0.0001f, Mathf.Abs(pitch)));
        }

        public static void PlaySound(string clip, float pitch = 1f, float volume = 1f)
        {
            if (Instance.audioClips == null || !Instance.audioClips.TryGetValue(clip, out var audioClip))
            {
                QAUtil.LogWarning($"AudioManager: Clip '{clip}' not found.");
                return;
            }

            PlaySound(audioClip, pitch, volume);
        }

        /// <summary>
        /// 3D(월드 위치) 효과음. AudioSource.PlayClipAtPoint 대체용.
        /// 최종볼륨 = soundVolume * volume
        /// </summary>
        public static void PlaySoundAt(AudioClip clip, Vector3 worldPos, float pitch = 1f, float spatialBlend = 1f,
                                       float minDistance = 1f, float maxDistance = 100f, float volume = 1f)
        {
            if (clip == null) return;

            float finalVol = Mathf.Clamp01(soundVolume * Mathf.Clamp01(volume));

            var src = CreateTempSource("SFX_3D", parent: null, worldPos);
            src.spatialBlend = Mathf.Clamp01(spatialBlend);
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = minDistance;
            src.maxDistance = maxDistance;
            src.pitch = pitch;
            src.volume = finalVol;
            src.PlayOneShot(clip, finalVol);

            Instance.RegisterAndAutoDestroy(src, clip.length / Mathf.Max(0.0001f, Mathf.Abs(pitch)));
        }

        public static void PlaySoundAt(string clip, Vector3 worldPos, float pitch = 1f, float spatialBlend = 1f,
                                       float minDistance = 1f, float maxDistance = 100f, float volume = 1f)
        {
            if (Instance.audioClips == null || !Instance.audioClips.TryGetValue(clip, out var audioClip))
            {
                QAUtil.LogWarning($"AudioManager: Clip '{clip}' not found.");
                return;
            }

            PlaySoundAt(audioClip, worldPos, pitch, spatialBlend, minDistance, maxDistance, volume);
        }

        private static AudioSource CreateTempSource(string name, Transform parent, Vector3 position)
        {
            var go = new GameObject(name);
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
                go.transform.localPosition = Vector3.zero;
            }
            else
            {
                go.transform.position = position;
            }

            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            return src;
        }

        private void RegisterAndAutoDestroy(AudioSource src, float life)
        {
            _activeSfxSources.Add(src);
            StartCoroutine(Co_DestroyAfter(src.gameObject, life + 0.1f));
        }

        private IEnumerator Co_DestroyAfter(GameObject go, float t)
        {
            yield return new WaitForSecondsRealtime(t);
            if (go != null) Destroy(go);
        }
        
        private void OnAudioConfigChanged(bool deviceWasChanged)
        {
            if (_musicSource != null)
            {
                _wasPlayingBeforeChange = _musicSource.isPlaying;
                _bgmPosBeforeChange     = Mathf.Clamp(_musicSource.time, 0f,
                                            _musicSource.clip ? _musicSource.clip.length - 0.05f : 0f);
            }
            StartCoroutine(RestoreAfterRouteChange());
        }

        private IEnumerator RestoreAfterRouteChange()
        {
            // 오디오 엔진 재초기화가 끝나도록 한두 프레임 대기
            yield return null;
            yield return new WaitForSecondsRealtime(0.5f);

            // BGM 복구
            if (_musicSource != null && _musicSource.clip != null)
            {
                _musicSource.time = _bgmPosBeforeChange;
                if (_wasPlayingBeforeChange) _musicSource.Play();
            }
        }
    }
}