using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Framework
{
    public class AudioManager
    {
        private readonly IEventBus _eventBus;
        private readonly ISettingsManager _settingsManager;

        private readonly Dictionary<string, AudioClip> _clips = new();
        private readonly Dictionary<string, AsyncOperationHandle<AudioClip>> _handles = new();
        private readonly HashSet<string> _loading = new();
        private readonly HashSet<string> _failed = new();

        private readonly Queue<AudioSource> _sfxPool = new();
        private readonly Dictionary<AudioSource, float> _activeSfx = new();
        private Transform _sfxRoot;

        private AudioSource _bgmA;
        private AudioSource _bgmB;
        private float _ratioA;
        private float _ratioB;
        private Tween _tweenA;
        private Tween _tweenB;
        private bool _activeIsA = true;
        private string _currentBgmKey;

        private float _masterVolume = 1f;
        private float _bgmVolume = 1f;
        private float _sfxVolume = 1f;
        private bool _isInitialized;

        private int _generation;
        private CancellationTokenSource _releaseCts = new();

        public float MasterVolume => _masterVolume;
        public float BgmVolume => _bgmVolume;
        public float SfxVolume => _sfxVolume;

        public AudioManager(IEventBus eventBus, ISettingsManager settingsManager)
        {
            _eventBus = eventBus;
            _settingsManager = settingsManager;
        }

        public void Initialize(Transform root)
        {
            if (_isInitialized)
            {
                Debug.LogWarning("[AudioManager] Already initialized.");
                return;
            }
            if (root == null)
            {
                Debug.LogError("[AudioManager] Initialize failed: root is null.");
                return;
            }

            _bgmA = CreateBgmSource(root, "BGM_A");
            _bgmB = CreateBgmSource(root, "BGM_B");

            var sfxGo = new GameObject("SFX");
            sfxGo.transform.SetParent(root, false);
            _sfxRoot = sfxGo.transform;

            _eventBus.Subscribe<AudioVolumeChangedEvent>(OnVolumeChanged);
            ApplyVolumes(_settingsManager.MasterVolume, _settingsManager.BgmVolume, _settingsManager.SfxVolume);
            _isInitialized = true;
        }

        private void OnVolumeChanged(AudioVolumeChangedEvent e)
            => ApplyVolumes(e.Master, e.Bgm, e.Sfx);

        private void ApplyVolumes(float master, float bgm, float sfx)
        {
            _masterVolume = Mathf.Clamp01(master);
            _bgmVolume = Mathf.Clamp01(bgm);
            _sfxVolume = Mathf.Clamp01(sfx);
            ApplyBgmVolumes();
            ApplySfxVolumes();
        }

        public async UniTask PlayBgm(string key, float fadeDuration = 1f, bool loop = true, CancellationToken ct = default)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning($"[AudioManager] PlayBgm('{key}') rejected: not initialized.");
                return;
            }
            if (string.IsNullOrEmpty(key)) return;
            if (_currentBgmKey == key && GetActiveBgm() != null && GetActiveBgm().isPlaying)
                return;

            var gen = _generation;
            var clip = await LoadClip(key);
            if (clip == null || ct.IsCancellationRequested) return;
            if (gen != _generation) return;

            var incoming = _activeIsA ? _bgmB : _bgmA;
            var outgoing = _activeIsA ? _bgmA : _bgmB;

            _tweenA?.Kill();
            _tweenB?.Kill();
            _tweenA = null;
            _tweenB = null;

            incoming.clip = clip;
            incoming.loop = loop;
            SetRatio(incoming, 0f);
            incoming.Play();

            fadeDuration = Mathf.Max(0f, fadeDuration);
            if (fadeDuration <= 0f)
            {
                SetRatio(incoming, 1f);
                SetRatio(outgoing, 0f);
                outgoing.Stop();
                outgoing.clip = null;
                _activeIsA = !_activeIsA;
                _currentBgmKey = key;
                return;
            }

            var incomingTween = CreateRatioTween(incoming, 1f, fadeDuration);
            var outgoingTween = CreateRatioTween(outgoing, 0f, fadeDuration);

            if (incoming == _bgmA) { _tweenA = incomingTween; _tweenB = outgoingTween; }
            else { _tweenB = incomingTween; _tweenA = outgoingTween; }

            try
            {
                await UniTask.WhenAll(
                    incomingTween.WithCancellation(ct),
                    outgoingTween.WithCancellation(ct));
            }
            catch (OperationCanceledException)
            {
                incomingTween.Kill();
                outgoingTween.Kill();
                return;
            }

            if (gen != _generation) return;

            outgoing.Stop();
            outgoing.clip = null;
            _activeIsA = !_activeIsA;
            _currentBgmKey = key;
        }

        public async UniTask StopBgm(float fadeDuration = 1f, CancellationToken ct = default)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("[AudioManager] StopBgm rejected: not initialized.");
                return;
            }

            var active = GetActiveBgm();
            if (active == null || !active.isPlaying) return;

            var gen = _generation;

            _tweenA?.Kill();
            _tweenB?.Kill();
            _tweenA = null;
            _tweenB = null;

            fadeDuration = Mathf.Max(0f, fadeDuration);
            if (fadeDuration <= 0f)
            {
                SetRatio(active, 0f);
                active.Stop();
                active.clip = null;
                _currentBgmKey = null;
                return;
            }

            var tween = CreateRatioTween(active, 0f, fadeDuration);
            if (active == _bgmA) _tweenA = tween; else _tweenB = tween;

            try
            {
                await tween.WithCancellation(ct);
            }
            catch (OperationCanceledException)
            {
                tween.Kill();
                return;
            }

            if (gen != _generation) return;

            active.Stop();
            active.clip = null;
            _currentBgmKey = null;
        }

        public async UniTask PlaySfx(string key, float volumeScale = 1f, float pitch = 1f)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning($"[AudioManager] PlaySfx('{key}') rejected: not initialized.");
                return;
            }
            if (string.IsNullOrEmpty(key)) return;

            var gen = _generation;
            var releaseToken = _releaseCts.Token;

            var clip = await LoadClip(key);
            if (clip == null) return;
            if (gen != _generation) return;

            if (float.IsNaN(volumeScale) || float.IsInfinity(volumeScale)) volumeScale = 1f;
            if (float.IsNaN(pitch) || float.IsInfinity(pitch)) pitch = 1f;
            volumeScale = Mathf.Max(0f, volumeScale);
            pitch = Mathf.Clamp(pitch, -3f, 3f);

            var src = RentSfx();
            if (src == null) return;
            if (gen != _generation)
            {
                if (src != null) UnityEngine.Object.Destroy(src.gameObject);
                return;
            }

            src.clip = clip;
            src.pitch = pitch;
            src.volume = volumeScale * _masterVolume * _sfxVolume;
            _activeSfx[src] = volumeScale;
            src.Play();

            try
            {
                await UniTask.WaitWhile(() => src != null && src.isPlaying, cancellationToken: releaseToken);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                if (gen != _generation)
                {
                    if (src != null) UnityEngine.Object.Destroy(src.gameObject);
                }
                else
                {
                    _activeSfx.Remove(src);
                    if (src != null) ReturnSfx(src);
                }
            }
        }

        public void ReleaseAll()
        {
            _generation++;
            try { _releaseCts.Cancel(); } catch { }
            _releaseCts.Dispose();
            _releaseCts = new CancellationTokenSource();

            _tweenA?.Kill();
            _tweenB?.Kill();
            _tweenA = null;
            _tweenB = null;

            if (_bgmA != null) { _bgmA.Stop(); _bgmA.clip = null; }
            if (_bgmB != null) { _bgmB.Stop(); _bgmB.clip = null; }
            _ratioA = 0f;
            _ratioB = 0f;
            _currentBgmKey = null;

            foreach (var kvp in _activeSfx)
            {
                var s = kvp.Key;
                if (s != null) UnityEngine.Object.Destroy(s.gameObject);
            }
            _activeSfx.Clear();

            while (_sfxPool.Count > 0)
            {
                var src = _sfxPool.Dequeue();
                if (src != null) UnityEngine.Object.Destroy(src.gameObject);
            }

            _clips.Clear();
            foreach (var kvp in _handles)
                Addressables.Release(kvp.Value);
            _handles.Clear();
            _loading.Clear();
            _failed.Clear();
        }

        private AudioSource CreateBgmSource(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = true;
            src.volume = 0f;
            return src;
        }

        private AudioSource GetActiveBgm() => _activeIsA ? _bgmA : _bgmB;

        private void SetRatio(AudioSource src, float ratio)
        {
            if (src == _bgmA) _ratioA = ratio;
            else if (src == _bgmB) _ratioB = ratio;
            src.volume = ratio * _masterVolume * _bgmVolume;
        }

        private Tween CreateRatioTween(AudioSource src, float target, float duration)
        {
            if (src == _bgmA)
                return DOTween.To(() => _ratioA, v => SetRatio(_bgmA, v), target, duration).SetEase(Ease.Linear);
            return DOTween.To(() => _ratioB, v => SetRatio(_bgmB, v), target, duration).SetEase(Ease.Linear);
        }

        private void ApplyBgmVolumes()
        {
            if (_bgmA != null) _bgmA.volume = _ratioA * _masterVolume * _bgmVolume;
            if (_bgmB != null) _bgmB.volume = _ratioB * _masterVolume * _bgmVolume;
        }

        private void ApplySfxVolumes()
        {
            foreach (var kvp in _activeSfx)
            {
                var src = kvp.Key;
                if (src != null) src.volume = kvp.Value * _masterVolume * _sfxVolume;
            }
        }

        private AudioSource RentSfx()
        {
            while (_sfxPool.Count > 0)
            {
                var s = _sfxPool.Dequeue();
                if (s != null) return s;
            }
            if (_sfxRoot == null)
            {
                Debug.LogError("[AudioManager] RentSfx failed: sfxRoot null.");
                return null;
            }
            var go = new GameObject("SFX_Source");
            go.transform.SetParent(_sfxRoot, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            return src;
        }

        private void ReturnSfx(AudioSource src)
        {
            if (src == null) return;
            src.Stop();
            src.clip = null;
            src.pitch = 1f;
            src.volume = 1f;
            _sfxPool.Enqueue(src);
        }

        private async UniTask<AudioClip> LoadClip(string key)
        {
            if (_clips.TryGetValue(key, out var cached))
                return cached;
            if (_failed.Contains(key))
                return null;

            var gen = _generation;
            var releaseToken = _releaseCts.Token;

            if (_loading.Contains(key))
            {
                try
                {
                    await UniTask.WaitUntil(
                        () => _clips.ContainsKey(key) || _failed.Contains(key) || !_loading.Contains(key),
                        cancellationToken: releaseToken);
                }
                catch (OperationCanceledException)
                {
                    return null;
                }
                if (gen != _generation) return null;
                return _clips.TryGetValue(key, out var loaded) ? loaded : null;
            }

            _loading.Add(key);
            AsyncOperationHandle<AudioClip> handle = default;
            try
            {
                handle = Addressables.LoadAssetAsync<AudioClip>(key);
                var clip = await handle;

                if (gen != _generation)
                {
                    Addressables.Release(handle);
                    return null;
                }

                if (clip == null)
                {
                    _failed.Add(key);
                    Addressables.Release(handle);
                    Debug.LogError($"[AudioManager] LoadClip('{key}') returned null.");
                    return null;
                }
                _clips[key] = clip;
                _handles[key] = handle;
                return clip;
            }
            catch (Exception e)
            {
                if (gen == _generation) _failed.Add(key);
                if (handle.IsValid()) Addressables.Release(handle);
                Debug.LogError($"[AudioManager] LoadClip('{key}') failed: {e.Message}");
                return null;
            }
            finally
            {
                if (gen == _generation) _loading.Remove(key);
            }
        }
    }
}
