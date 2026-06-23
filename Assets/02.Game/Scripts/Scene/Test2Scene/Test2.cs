using VContainer;
using VContainer.Unity;

namespace Framework
{
    public class Test2 : IStartable
    {
        [Inject] private Test test;

        public void Start()
        {
            
        }

        public void AAA()
        {
            test.Temp();
        }
    }
}