using UnityEngine;

namespace Framework
{
    public class TestPopup : PopupView
    {
        private int index = 0;
        
        public override void Initialize()
        {
            index++;
            Debug.Log($"index : {index}");
        }

        public override void Dispose()
        {
            
        }
    }
}