using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Framework
{
    public class RootCanvasSetting : ScriptableObject
    {
        public static RootCanvasSetting Instance { get; private set; }

        public RootCanvas RootCanvas;
        public InputActionAsset InputActions;
        
#if UNITY_EDITOR
        [UnityEditor.MenuItem("Assets/Create/Setting/Root Canvas Setting")]
        public static void CreateAsset()
        {
            var path = UnityEditor.EditorUtility.SaveFilePanelInProject(
                "Save RootCanvasSettings",
                "RootCanvasSettings",
                "asset",
                string.Empty);
            
            if(string.IsNullOrEmpty(path))
                return;

            var newSettings = CreateInstance<RootCanvasSetting>();
            UnityEditor.AssetDatabase.CreateAsset(newSettings, path);

            var preloadedAssets = UnityEditor.PlayerSettings.GetPreloadedAssets().ToList();
            preloadedAssets.RemoveAll(x => x is RootCanvasSetting);
            preloadedAssets.Add(newSettings);

            UnityEditor.PlayerSettings.SetPreloadedAssets(preloadedAssets.ToArray());
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void RuntimeInitialize()
        {
            LoadInstanceFromPreloadAssets();
        }

        private static void LoadInstanceFromPreloadAssets()
        {
            var preloadedAssets = UnityEditor.PlayerSettings.GetPreloadedAssets().FirstOrDefault(x => x is RootCanvasSetting);
            if (preloadedAssets is RootCanvasSetting instance)
            {
                instance.OnEnable();
            }
        }
#endif
        void OnEnable()
        {
            Instance = this;
        }
    }
}