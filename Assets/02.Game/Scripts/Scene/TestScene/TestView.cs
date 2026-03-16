using UnityEngine;
using VContainer;

namespace Framework
{
    public class TestView : MonoBehaviour
    {
        [Inject] private Test2 test2;
        [Inject] private SceneLoader _sceneLoader;

        private async void Start()
        {
            test2.AAA();
            await _sceneLoader.LoadScene("Test2Scene");
        }
    }
}
