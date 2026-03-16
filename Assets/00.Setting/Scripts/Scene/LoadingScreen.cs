using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Framework
{
    public class LoadingScreen : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;

        public async UniTask Show()
        {
            canvasGroup.alpha = 0f;
            await canvasGroup.DOFade(1f, 0.4f).SetEase(Ease.InQuad)
                .WithCancellation(destroyCancellationToken);
            await UniTask.WaitForSeconds(0.3f, cancellationToken: destroyCancellationToken);
        }

        public async UniTask Hide()
        {
            await UniTask.WaitForSeconds(0.2f, cancellationToken: destroyCancellationToken);
            await canvasGroup.DOFade(0f, 0.3f).SetEase(Ease.OutQuad)
                .WithCancellation(destroyCancellationToken);
        }
    }
}
