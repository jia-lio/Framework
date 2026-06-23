using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace Framework
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class PopupView : MonoBehaviour, IPopup
    {
        [Inject] private UIManager _uiManager;

        private CanvasGroup _canvasGroup;
        internal CanvasGroup CanvasGroup => _canvasGroup ??= GetComponent<CanvasGroup>();

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            OnAwake();
        }

        /// <summary>서브클래스 초기화 훅. [Inject] 필드는 아직 null이므로 사용 금지.</summary>
        protected virtual void OnAwake() { }

        public virtual async UniTask OnShow()
            => await UIAnimation.PopIn(CanvasGroup, ct: destroyCancellationToken);

        public virtual async UniTask OnHide()
            => await UIAnimation.PopOut(CanvasGroup, ct: destroyCancellationToken);

        public virtual void OnEnter() { }
        public virtual void OnExit() { }

        public void Back() => _uiManager.Back().Forget();
    }
}
