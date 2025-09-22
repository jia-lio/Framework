using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;

namespace Framework
{
    public class TestView : MonoBehaviour
    {
        [Inject] private Test2 test2;

        private async void Start()
        {
            test2.AAA();

            var currentScene = SceneManager.GetActiveScene();
            var nextScene = SceneManager.LoadSceneAsync("Test2Scene", LoadSceneMode.Additive);
            await nextScene.ToUniTask();
            await SceneManager.UnloadSceneAsync(currentScene);
        }
    }
}