using UnityEngine;
using VContainer;

namespace Framework
{
    public class Test2View : MonoBehaviour
    {
        [Inject] private Test2 test2;
        [Inject] private UIManager _uiManager;

        private async void Start()
        {
            test2.AAA();
            await _uiManager.Open<TestPopup>(Address.TESTPOPUP_PREFAB);
        }
    }
}
