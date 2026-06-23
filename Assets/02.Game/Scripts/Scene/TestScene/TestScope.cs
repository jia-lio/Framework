using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Framework
{
    public class TestScope : LifetimeScope
    {
        [SerializeField] private TestView view;
        
        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);

            builder.RegisterComponent(view);
        }
    }
}