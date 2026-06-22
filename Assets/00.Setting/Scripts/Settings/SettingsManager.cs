using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Framework
{
    public sealed class SettingsManager : ISettingsManager, IAppLifecycle
    {
        private readonly SaveManager<SaveData> _saveManager;
        private readonly IEventBus _eventBus;

        private float _master = 1f;
        private float _bgm = 1f;
        private float _sfx = 1f;

        public SettingsManager(SaveManager<SaveData> saveManager, IEventBus eventBus)
        {
            _saveManager = saveManager;
            _eventBus = eventBus;
        }

        public float MasterVolume
        {
            get => _master;
            set { _master = Mathf.Clamp01(value); PublishAudio(); }
        }

        public float BgmVolume
        {
            get => _bgm;
            set { _bgm = Mathf.Clamp01(value); PublishAudio(); }
        }

        public float SfxVolume
        {
            get => _sfx;
            set { _sfx = Mathf.Clamp01(value); PublishAudio(); }
        }

        public void ApplyFromSaveData()
        {
            var data = _saveManager.Data;
            if (data != null && data.HasAudioSettings)
            {
                _master = Mathf.Clamp01(data.MasterVolume);
                _bgm = Mathf.Clamp01(data.BgmVolume);
                _sfx = Mathf.Clamp01(data.SfxVolume);
            }
            else
            {
                _master = 1f;
                _bgm = 1f;
                _sfx = 1f;
            }
            PublishAudio();
        }

        public async UniTask<bool> Save()
        {
            if (_saveManager.Data == null)
            {
                Debug.LogWarning("[SettingsManager] Save rejected: SaveData null.");
                return false;
            }
            SyncToCache();
            return await _saveManager.Save();
        }

        // 라이브 볼륨 필드를 _cachedData에 반영. Save()와 라이프사이클 OnSuspend가 공유.
        private void SyncToCache()
        {
            var data = _saveManager.Data;
            if (data == null) return;
            data.MasterVolume = _master;
            data.BgmVolume = _bgm;
            data.SfxVolume = _sfx;
            data.HasAudioSettings = true;
        }

        // 백그라운드/종료 시 라이브 상태를 _cachedData로 수확 → SaveDataInitializer.OnSuspend의 FlushSync가 디스크에 씀.
        // (RootScope 등록 순서상 SettingsManager가 SaveDataInitializer보다 먼저 → 수확이 flush보다 선행)
        public void OnSuspend() => SyncToCache();

        public void OnResume() { }

        public void ResetToDefaults()
        {
            _master = 1f;
            _bgm = 1f;
            _sfx = 1f;
            PublishAudio();
        }

        private void PublishAudio()
            => _eventBus.Publish(new AudioVolumeChangedEvent(_master, _bgm, _sfx));
    }
}
