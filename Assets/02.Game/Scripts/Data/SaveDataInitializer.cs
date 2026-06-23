using UnityEngine;

namespace Framework
{
    public class SaveDataInitializer : IInitializable, IAppLifecycle
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

        // 백그라운드/종료 시 동기 flush (미로드 상태면 SaveManager가 자체 스킵)
        public void OnSuspend() => _saveManager.FlushSync();

        public void OnResume() { }   // 현재 미사용 (향후 무결성 재검증/리로드 훅)
    }
}
