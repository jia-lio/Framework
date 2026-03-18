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
            builder.Register<RootManager>(Lifetime.Singleton).AsSelf().As<IStartable>();
            builder.Register<UIManager>(Lifetime.Singleton);
            builder.Register<ToastManager>(Lifetime.Singleton);
            builder.Register<SceneLoader>(Lifetime.Singleton);
            builder.Register<SaveManager<SaveData>>(Lifetime.Singleton);
            builder.Register<SaveDataInitializer>(Lifetime.Singleton).As<IInitializable>();

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
