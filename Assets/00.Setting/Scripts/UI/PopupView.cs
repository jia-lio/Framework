using UnityEngine;

namespace Framework
{
    public class PopupView : MonoBehaviour, IPopup
    {
        public virtual void Initialize() { }
        public virtual void Dispose() { }
    }
}