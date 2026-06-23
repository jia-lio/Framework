using UnityEngine;
using VContainer.Unity;

namespace Framework
{
    public class Test : IStartable
    {
        private int numIndex = 0;
        
        public void Start()
        {
            
        }

        public void Temp()
        {
            numIndex++;
            
            Debug.Log($"numIndex: {numIndex}");
        }
    }
}