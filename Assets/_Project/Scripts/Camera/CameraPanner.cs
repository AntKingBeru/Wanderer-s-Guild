using System.Threading;
using DG.Tweening;
using UnityEngine;

namespace WanderersGuild
{
    public class CameraPanner : MonoBehaviour
    {
        private Tween _panTween;

        public void SnapTo(Transform anchor)
        {
            KillPan();
            transform.position = OnCameraPlane(anchor.position);
        }

        //Pans to the anchor using GameConfig.CameraPan timing. Completes on arrival or when interrupted.
        public async Awaitable PanToAsync(Transform anchor, CancellationToken token)
        {
            KillPan();
            var settings = GameConfig.Instance.CameraPan;

            _panTween = transform.DOMove(OnCameraPlane(anchor.position), settings.Duration)
                .SetEase(settings.Ease)
                .SetUpdate(true)
                .SetLink(gameObject);

            await _panTween.AwaitCompletionAsync(token);
        }

        private void KillPan()
        {
            if (_panTween.IsActive())
                _panTween.Kill();
        }

        // NOTE: Anchors only define X/Y. The camera keeps its own Z distance from the layers.
        private Vector3 OnCameraPlane(Vector3 point) => new(point.x, point.y, transform.position.z);
    }
}