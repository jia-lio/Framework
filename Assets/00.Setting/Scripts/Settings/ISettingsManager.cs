using Cysharp.Threading.Tasks;

namespace Framework
{
    public interface ISettingsManager
    {
        float MasterVolume { get; set; }
        float BgmVolume { get; set; }
        float SfxVolume { get; set; }

        void ApplyFromSaveData();
        UniTask<bool> Save();
        void ResetToDefaults();
    }
}
