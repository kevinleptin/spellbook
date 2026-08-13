using System.Collections.Generic;
using UnityEngine;

namespace Spellbook.FX
{
    /// <summary>
    /// 全局音频:界面音效池 + 环境音乐。剪辑按名从 Resources/Audio 加载并缓存。
    /// 静音状态存 PlayerPrefs("muted")。
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private const int SfxSources = 6;
        private static AudioManager _instance;

        private readonly Dictionary<string, AudioClip> _cache = new Dictionary<string, AudioClip>();
        private AudioSource[] _sfx;
        private int _next;
        private AudioSource _music;
        private bool _muted;

        public static AudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[Audio]");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<AudioManager>();
                    _instance.Init();
                }
                return _instance;
            }
        }

        public bool Muted
        {
            get => _muted;
            set
            {
                _muted = value;
                PlayerPrefs.SetInt("muted", value ? 1 : 0);
                _music.mute = value;
            }
        }

        private void Init()
        {
            _sfx = new AudioSource[SfxSources];
            for (var i = 0; i < SfxSources; i++)
            {
                _sfx[i] = gameObject.AddComponent<AudioSource>();
                _sfx[i].playOnAwake = false;
            }
            _music = gameObject.AddComponent<AudioSource>();
            _music.playOnAwake = false;
            _music.loop = true;
            _music.volume = 0.35f;
            _muted = PlayerPrefs.GetInt("muted", 0) == 1;
            _music.mute = _muted;
        }

        /// <summary>播放音效:轮询源池,轻微随机变调避免机械感。</summary>
        public void Play(string name, float volume = 1f, float pitchJitter = 0.06f)
        {
            if (_muted) return;
            var clip = Load(name);
            if (clip == null) return;

            var src = _sfx[_next];
            _next = (_next + 1) % SfxSources;
            src.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            src.PlayOneShot(clip, volume);
        }

        /// <summary>从一组变体中随机播一个(如 cast1..cast5、page1..pageN)。</summary>
        public void PlayVariant(string prefix, int count, float volume = 1f)
            => Play(prefix + Random.Range(1, count + 1), volume);

        public void PlayMusic(string name)
        {
            var clip = Load(name);
            if (clip == null) return;
            _music.clip = clip;
            _music.Play();
        }

        private AudioClip Load(string name)
        {
            if (_cache.TryGetValue(name, out var clip)) return clip;
            clip = Resources.Load<AudioClip>("Audio/" + name);
            if (clip == null) Debug.LogWarning($"缺少音频资源: Audio/{name}");
            _cache[name] = clip;
            return clip;
        }
    }
}
