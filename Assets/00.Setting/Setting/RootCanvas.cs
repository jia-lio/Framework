using UnityEngine;
using VContainer;

namespace Framework
{
    [RequireComponent(typeof(Canvas))]
    public class RootCanvas : MonoBehaviour
    {
        public Transform root;
        public Transform toastRoot;

        [Inject] private UIManager _uiManager;
        [Inject] private ToastManager _toastManager;

        private void Start()
        {
            _uiManager.Initialize(root);
            _toastManager.Initialize(toastRoot);
            DontDestroyOnLoad(this);
        }
    }
}
