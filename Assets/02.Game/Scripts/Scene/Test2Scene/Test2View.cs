using UnityEngine;
using VContainer;

namespace Framework
{ 
    public class Test2View : MonoBehaviour
    {
        [Inject] private Test2 test2;
        
        private void Start()
        {
            test2.AAA();
        }
    }
}