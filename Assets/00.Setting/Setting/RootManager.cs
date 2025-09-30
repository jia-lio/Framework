using System.Collections.Generic;
using VContainer.Unity;

namespace Framework
{
    public class RootManager : IStartable
    {
        private readonly IEnumerable<IInitializable> initializables;

        public RootManager(IEnumerable<IInitializable> initializables)
        {
            this.initializables = initializables;
        }
        
        public void Start()
        {
            foreach (var init in initializables)
            {
                init.Initialize();
            }
        }
    }
}