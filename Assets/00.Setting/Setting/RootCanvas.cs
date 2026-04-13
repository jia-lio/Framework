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

        [Inject] private UIManager _uiManager;
        [Inject] private ToastManager _toastManager;
        [Inject] private ObjectPoolManager _poolManager;

        private void Start()
        {
            _uiManager.Initialize(root);
            _toastManager.Initialize(toastRoot);
            _poolManager.Initialize(poolRoot);
            DontDestroyOnLoad(this);
        }
    }
}
