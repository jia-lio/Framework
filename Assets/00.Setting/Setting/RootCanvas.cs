using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace Framework
{
    [RequireComponent(typeof(Canvas))]
    public class RootCanvas : MonoBehaviour
    {
        public Transform root;
        public Transform toastRoot;
        public Transform poolRoot;
        public Transform audioRoot;

        [Inject] private UIManager _uiManager;
        [Inject] private ToastManager _toastManager;
        [Inject] private ObjectPoolManager _poolManager;
        [Inject] private AudioManager _audioManager;
        [Inject] private SaveManager<SaveData> _saveManager;

        private void Start()
        {
            _uiManager.Initialize(root);
            _toastManager.Initialize(toastRoot);
            _poolManager.Initialize(poolRoot);

            if (audioRoot == null)
            {
                var go = new GameObject("AudioRoot");
                go.transform.SetParent(transform, false);
                audioRoot = go.transform;
            }

            DontDestroyOnLoad(this);

            InitializeAudioAsync().Forget();
        }

        private async UniTaskVoid InitializeAudioAsync()
        {
            await _saveManager.Load();
            _audioManager.Initialize(audioRoot, _saveManager.Data);
        }
    }
}
