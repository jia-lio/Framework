using UnityEngine;

namespace Framework
{
    public class SaveDataInitializer : IInitializable
    {
        private readonly SaveManager<SaveData> _saveManager;

        public SaveDataInitializer(SaveManager<SaveData> saveManager)
        {
            _saveManager = saveManager;
        }

        public void Initialize()
        {
            SaveEncryption.SetKey("FrameworkPlaceholderKey_32Bytes!");
            _saveManager.Initialize("save.dat");
            Debug.Log("[SaveDataInitializer] Initialized");
        }
    }
}
