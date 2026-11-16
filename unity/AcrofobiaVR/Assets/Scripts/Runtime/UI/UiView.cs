using UnityEngine;

namespace Acrofobia.UI
{
    /// <summary>Base for panels that build their uGUI hierarchy from code on Awake.</summary>
    public abstract class UiView : MonoBehaviour
    {
        [SerializeField] private UiTheme theme;

        protected UiFactory Factory { get; private set; }
        protected UiTheme Theme => theme;
        protected RectTransform Root { get; private set; }

        protected virtual void Awake()
        {
            Factory = new UiFactory(theme);
            Root = UiFactory.Rect(GetType().Name, transform);
            UiFactory.Stretch(Root);
            Build(Root);
        }

        protected abstract void Build(RectTransform root);

        protected void SetVisible(bool visible) => Root.gameObject.SetActive(visible);
    }
}
