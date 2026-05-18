using System;

namespace Framework
{
    [Serializable]
    public class SaveData
    {
        public bool HasAudioSettings;
        public float MasterVolume;
        public float BgmVolume;
        public float SfxVolume;
    }
}
