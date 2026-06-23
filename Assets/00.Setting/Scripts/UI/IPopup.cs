using Cysharp.Threading.Tasks;

namespace Framework
{
    public interface IPopup
    {
        UniTask OnShow();
        UniTask OnHide();
        void OnEnter();
        void OnExit();
    }
}
