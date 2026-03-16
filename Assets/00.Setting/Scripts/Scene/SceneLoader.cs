using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Framework
{
    public class SceneLoader
    {
        private readonly UIManager _uiManager;
        private bool _isTransitioning;

        public bool IsTransitioning => _isTransitioning;

        public SceneLoader(UIManager uiManager)
        {
            _uiManager = uiManager;
        }

        public async UniTask LoadScene(string sceneName, Scene outgoingScene = default)
        {
            if (_isTransitioning) return;
            _isTransitioning = true;

            try
            {
                await _uiManager.CloseAll();

                var currentScene = outgoingScene.IsValid() ? outgoingScene : SceneManager.GetActiveScene();

                var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                await operation.ToUniTask();

                SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));

                await SceneManager.UnloadSceneAsync(currentScene);
            }
            finally
            {
                _isTransitioning = false;
            }
        }

        public async UniTask LoadScene(string sceneName, string loadingSceneName,
            Scene outgoingScene = default)
        {
            if (_isTransitioning) return;
            _isTransitioning = true;

            LoadingScreen loadingScreen = null;
            try
            {
                await _uiManager.CloseAll();

                var currentScene = outgoingScene.IsValid() ? outgoingScene : SceneManager.GetActiveScene();

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
                _isTransitioning = false;
            }

            CleanupLoadingScene(loadingScreen, loadingSceneName).Forget();
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
