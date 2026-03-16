using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Framework
{
    public static class UIAnimation
    {
        public static async UniTask FadeIn(CanvasGroup canvasGroup, float duration = 0.3f,
            CancellationToken ct = default)
        {
            canvasGroup.alpha = 0f;
            await canvasGroup.DOFade(1f, duration).WithCancellation(ct);
        }

        public static async UniTask FadeOut(CanvasGroup canvasGroup, float duration = 0.3f,
            CancellationToken ct = default)
        {
            await canvasGroup.DOFade(0f, duration).WithCancellation(ct);
        }

        public static async UniTask ScaleIn(Transform transform, float duration = 0.3f,
            CancellationToken ct = default)
        {
            transform.localScale = Vector3.zero;
            await transform.DOScale(Vector3.one, duration).SetEase(Ease.OutBack)
                .WithCancellation(ct);
        }

        public static async UniTask ScaleOut(Transform transform, float duration = 0.2f,
            CancellationToken ct = default)
        {
            await transform.DOScale(Vector3.zero, duration).SetEase(Ease.InBack)
                .WithCancellation(ct);
        }

        public static async UniTask PopIn(CanvasGroup canvasGroup, float duration = 0.3f,
            CancellationToken ct = default)
        {
            await UniTask.WhenAll(
                FadeIn(canvasGroup, duration, ct),
                ScaleIn(canvasGroup.transform, duration, ct)
            );
        }

        public static async UniTask PopOut(CanvasGroup canvasGroup, float duration = 0.2f,
            CancellationToken ct = default)
        {
            await UniTask.WhenAll(
                FadeOut(canvasGroup, duration, ct),
                ScaleOut(canvasGroup.transform, duration, ct)
            );
        }
    }
}
