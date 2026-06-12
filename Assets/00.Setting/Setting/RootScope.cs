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
            builder.Register<ObjectPoolManager>(Lifetime.Singleton);
            builder.Register<AudioManager>(Lifetime.Singleton);
            builder.Register<SettingsManager>(Lifetime.Singleton)
                .AsSelf()
                .As<ISettingsManager>();
            builder.Register<IEventBus, EventBus>(Lifetime.Singleton);
            builder.Register<SaveManager<SaveData>>(Lifetime.Singleton);
            builder.Register<SaveDataInitializer>(Lifetime.Singleton).As<IInitializable>();

            var inputActions = RootCanvasSetting.Instance?.InputActions;
            builder.Register<InputManager>(c =>
                    new InputManager(inputActions, c.Resolve<IEventBus>()),
                    Lifetime.Singleton)
                .AsSelf().As<IInputManager>().As<IInitializable>();

            builder.Register<PauseService>(Lifetime.Singleton)
                .AsSelf().As<IPauseService>();

            builder.Register<Test>(Lifetime.Singleton);
            builder.Register<Test2>(Lifetime.Singleton);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            builder.Register<DebugLogCapture>(Lifetime.Singleton);
            builder.Register<DebugManager>(Lifetime.Singleton)
                .AsSelf().As<IInitializable>();
#endif
        }

        private void RegisterUI(IContainerBuilder builder)
        {
            var rootCanvasPrefab = RootCanvasSetting.Instance.RootCanvas;
            if (rootCanvasPrefab == null)
                return;

            RootCanvas rootCanvas = Instantiate(rootCanvasPrefab);
            rootCanvas.name = "[RootCanvas]";

            builder.RegisterComponent(rootCanvas).As<RootCanvas>();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var hudGo = new GameObject("[DebugHud]");
            DontDestroyOnLoad(hudGo);
            var hud = hudGo.AddComponent<DebugHud>();
            builder.RegisterComponent(hud).As<DebugHud>();

            var logPanelGo = new GameObject("[DebugLogPanel]");
            DontDestroyOnLoad(logPanelGo);
            var logPanel = logPanelGo.AddComponent<DebugLogPanel>();
            builder.RegisterComponent(logPanel).As<DebugLogPanel>();
#endif
        }
    }
}
