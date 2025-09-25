using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;

namespace Framework
{
    public static class UISystem
    {
        private static readonly Dictionary<string, GameObject> _prefabs = new();
        private static readonly Dictionary<string, GameObject> _loadedPrefabs = new();

        private static Transform _uiRoot;

        public static void Initialize(Transform root)
        {
            _uiRoot = root;
        }

        public static async UniTask<T> Open<T>(string key) where T : Component, IPopup
        {
            GameObject go;

            if (_prefabs.TryGetValue(key, out go) == false && _loadedPrefabs.TryGetValue(key, out go) == false)
            {
                var prefab = await Addressables.LoadAssetAsync<GameObject>(key);
                go = Object.Instantiate(prefab, _uiRoot);
                go.name = key;

                _loadedPrefabs[key] = go;
            }

            go.SetActive(true);

            var ui = go.GetComponent<T>();
            ui.Initialize();
            
            return ui;
        }
    }
}