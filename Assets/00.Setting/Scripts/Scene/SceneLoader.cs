using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Framework
{
    public class SceneLoader
    {
        private readonly UIManager _uiManager;
        private readonly ObjectPoolManager _poolManager;

        public bool IsTransitioning { get; private set; }

        public SceneLoader(UIManager uiManager, ObjectPoolManager poolManager)
        {
            _uiManager = uiManager;
            _poolManager = poolManager;
        }

        public async UniTask LoadScene(string sceneName, Scene outgoingScene = default)
        {
            if (IsTransitioning) return;
            IsTransitioning = true;

            try
            {
                var currentScene = await PrepareTransition(outgoingScene);

                var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                await operation.ToUniTask();

                SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));

                await SceneManager.UnloadSceneAsync(currentScene);
            }
            finally
            {
                IsTransitioning = false;
            }
        }

        public async UniTask LoadScene(string sceneName, string loadingSceneName,
            Scene outgoingScene = default)
        {
            if (IsTransitioning) return;
            IsTransitioning = true;

            LoadingScreen loadingScreen = null;
            try
            {
                var currentScene = await PrepareTransition(outgoingScene);

                var loadingOperation = SceneManager.LoadSceneAsync(loadingSceneName, LoadSceneMode.Additive);
                await loadingOperation.ToUniTask();

                loadingScreen = FindInScene<LoadingScreen>(loadingSceneName);
                if (loadingScreen != null)
                    await loadingScreen.Show();

                await SceneManager.UnloadSceneAsync(currentScene);

                var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                await operation.ToUniTask();

                SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
            }
            finally
            {
                IsTransitioning = false;
            }

            CleanupLoadingScene(loadingScreen, loadingSceneName).Forget();
        }

        private async UniTask<Scene> PrepareTransition(Scene outgoingScene)
        {
            await _uiManager.CloseAll();
            _poolManager.ReleaseAll();
            return outgoingScene.IsValid() ? outgoingScene : SceneManager.GetActiveScene();
        }

        private async UniTask CleanupLoadingScene(LoadingScreen loadingScreen, string loadingSceneName)
        {
            try
            {
                if (loadingScreen != null)
                    await loadingScreen.Hide();
                await SceneManager.UnloadSceneAsync(loadingSceneName);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SceneLoader] Loading scene cleanup failed: {e}");
            }
        }

        private static T FindInScene<T>(string sceneName) where T : Component
        {
            var scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid()) return null;

            foreach (var root in scene.GetRootGameObjects())
            {
                var component = root.GetComponentInChildren<T>();
                if (component != null) return component;
            }

            Debug.LogWarning($"[SceneLoader] '{typeof(T).Name}' not found in scene '{sceneName}'");
            return null;
        }
    }
}
