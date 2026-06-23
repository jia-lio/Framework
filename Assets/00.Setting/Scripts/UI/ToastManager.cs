using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Framework
{
    public class ToastManager
    {
        private Transform _toastRoot;
        private GameObject _template;
        private readonly Queue<(string message, float duration)> _queue = new();
        private bool _isShowing;

        public void Initialize(Transform toastRoot)
        {
            _toastRoot = toastRoot;
            CreateTemplate();
        }

        public void Show(string message, float duration = 2f)
        {
            _queue.Enqueue((message, duration));
            if (!_isShowing) ProcessQueue().Forget();
        }

        private async UniTask ProcessQueue()
        {
            _isShowing = true;
            try
            {
                while (_queue.Count > 0)
                {
                    var (message, duration) = _queue.Dequeue();
                    await ShowToast(message, duration);
                }
            }
            finally
            {
                _isShowing = false;
            }
        }

        private async UniTask ShowToast(string message, float duration)
        {
            var go = Object.Instantiate(_template, _toastRoot);
            go.SetActive(true);

            var text = go.GetComponentInChildren<Text>();
            if (text == null) { Object.Destroy(go); return; }
            text.text = message;

            var cg = go.GetComponent<CanvasGroup>();

            var ct = go.GetCancellationTokenOnDestroy();
            try
            {
                await UIAnimation.FadeIn(cg, ct: ct);
                await UniTask.Delay((int)(duration * 1000), cancellationToken: ct);
                await UIAnimation.FadeOut(cg, ct: ct);
            }
            catch (System.OperationCanceledException)
            {
                if (go != null) Object.Destroy(go);
                return;
            }

            Object.Destroy(go);
        }

        private void CreateTemplate()
        {
            _template = new GameObject("ToastTemplate");
            _template.SetActive(false);
            _template.transform.SetParent(_toastRoot, false);

            var rect = _template.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 100f);
            rect.sizeDelta = new Vector2(600f, 80f);

            _template.AddComponent<CanvasGroup>();

            var bg = _template.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(_template.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(20f, 10f);
            textRect.offsetMax = new Vector2(-20f, -10f);

            var textComp = textGo.AddComponent<Text>();
            textComp.alignment = TextAnchor.MiddleCenter;
            textComp.fontSize = 28;
            textComp.color = Color.white;
            textComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }
}
