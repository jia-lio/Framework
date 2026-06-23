using UnityEngine;

namespace Framework
{
    public class TestPopup : PopupView
    {
        private int index = 0;

        public override void OnEnter()
        {
            index++;
            Debug.Log($"[TestPopup] Enter index: {index}");
        }

        public override void OnExit()
        {
            Debug.Log("[TestPopup] Exit");
        }
    }
}
