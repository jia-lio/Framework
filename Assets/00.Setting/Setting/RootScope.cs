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
            RegisterUI(builder);
            
            Debug.Log("[RootScope] Configure");
        }

        private void RegisterModel(IContainerBuilder builder)
        {
            //builder.RegisterInstance();
            builder.Register<RootManager>(Lifetime.Singleton).AsSelf();
            
            builder.Register<Test>(Lifetime.Singleton);
            builder.Register<Test2>(Lifetime.Singleton);
        }

        private void RegisterUI(IContainerBuilder builder)
        {
            var rootCanvasPrefab = RootCanvasSetting.Instance.RootCanvas;
            if (rootCanvasPrefab == null)
                return;

            RootCanvas rootCanvas = Instantiate(rootCanvasPrefab);
            rootCanvas.name = "[RootCanvas]";

            builder.RegisterComponent(rootCanvas).As<RootCanvas>();
        }
    }
}