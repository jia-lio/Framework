using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Framework
{
    public class Test2Scope : LifetimeScope
    {
        [SerializeField] private Test2View view;
        
        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);

            builder.RegisterComponent(view);
        }
    }
}