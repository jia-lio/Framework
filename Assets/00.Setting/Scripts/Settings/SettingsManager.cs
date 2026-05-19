using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Framework
{
    public sealed class SettingsManager : ISettingsManager
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
            var data = _saveManager.Data;
            if (data == null)
            {
                Debug.LogWarning("[SettingsManager] Save rejected: SaveData null.");
                return false;
            }
            data.MasterVolume = _master;
            data.BgmVolume = _bgm;
            data.SfxVolume = _sfx;
            data.HasAudioSettings = true;
            return await _saveManager.Save();
        }

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
