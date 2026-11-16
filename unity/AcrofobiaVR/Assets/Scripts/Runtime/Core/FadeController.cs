using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Acrofobia.Core
{
    /// <summary>Full-screen fade to and from black. Starts opaque so the first frame never shows an empty world.</summary>
    public sealed class FadeController : MonoBehaviour
    {
        [SerializeField] private Color color = new(0.02f, 0.03f, 0.05f, 1f);

        private CanvasGroup group;

        public bool IsOpaque => group.alpha >= 0.999f;

        private void Awake()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = 1f;
            group.blocksRaycasts = true;

            var image = new GameObject("Black", typeof(RectTransform)).AddComponent<Image>();
            image.transform.SetParent(transform, false);
            image.color = color;
            var rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public IEnumerator FadeOut(float seconds) => FadeTo(1f, seconds);

        public IEnumerator FadeIn(float seconds) => FadeTo(0f, seconds);

        private IEnumerator FadeTo(float alpha, float seconds)
        {
            float start = group.alpha;
            group.blocksRaycasts = true;
            for (float t = 0f; t < seconds && !Mathf.Approximately(group.alpha, alpha); t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Lerp(start, alpha, Mathf.SmoothStep(0f, 1f, t / seconds));
                yield return null;
            }
            group.alpha = alpha;
            group.blocksRaycasts = alpha > 0.5f;
        }
    }
}
