using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Framework
{
    public class RootScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);

            RegisterModel(builder);
            
            Debug.Log("[RootScope] Configure");
        }

        private void RegisterModel(IContainerBuilder builder)
        {
            builder.Register<Test>(Lifetime.Singleton);
            builder.Register<Test2>(Lifetime.Singleton);
        }
    }
}