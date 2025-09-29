using UnityEngine;

namespace Framework
{
    [RequireComponent(typeof(Canvas))]
    public class RootCanvas : MonoBehaviour
    {
        public Transform root;
        
        private void Awake()
        {
            UISystem.Initialize(root);
            
            DontDestroyOnLoad(this);
        }
    }
}