using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Acrofobia.UI
{
    public enum ButtonStyle
    {
        Primary,
        Secondary,
        Calm,
    }

    /// <summary>
    /// Builds uGUI hierarchies from code with the project's theme, so every panel shares the same
    /// look and there are no hand-edited prefabs to keep in sync.
    /// </summary>
    public sealed class UiFactory
    {
        private static Sprite roundedSprite;
        private static Sprite circleSprite;

        public UiFactory(UiTheme theme) => Theme = theme;

        public UiTheme Theme { get; }

        public static Sprite Rounded => roundedSprite ??= CreateRoundedSprite(64, 18);
        public static Sprite CircleSprite => circleSprite ??= CreateRoundedSprite(128, 64);

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        public static void Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        public static void Anchor(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 offset)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = offset;
        }

        public Image Panel(string name, Transform parent, Color color, bool rounded = true)
        {
            var image = Rect(name, parent).gameObject.AddComponent<Image>();
            if (rounded)
            {
                image.sprite = Rounded;
                image.type = Image.Type.Sliced;
            }
            image.color = color;
            return image;
        }

        /// <summary>A true circle: the sprite is drawn unsliced so it never turns into a rounded square when scaled.</summary>
        public Image Circle(string name, Transform parent, Color color)
        {
            var image = Panel(name, parent, color);
            image.sprite = CircleSprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            return image;
        }

        public static VerticalLayoutGroup Vertical(GameObject target, int padding, float spacing, TextAnchor alignment = TextAnchor.UpperLeft)
        {
            var layout = target.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.spacing = spacing;
            layout.childAlignment = alignment;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return layout;
        }

        public static HorizontalLayoutGroup Horizontal(GameObject target, float spacing, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            var layout = target.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = alignment;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return layout;
        }

        public static void FitContent(GameObject target)
        {
            var fitter = target.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        public static LayoutElement Size(Component target, float preferredWidth = -1f, float preferredHeight = -1f, float flexibleWidth = -1f)
        {
            if (!target.TryGetComponent(out LayoutElement element)) element = target.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = preferredWidth;
            element.preferredHeight = preferredHeight;
            element.flexibleWidth = flexibleWidth;
            return element;
        }

        public TextMeshProUGUI Label(Transform parent, string text, float size, Color color, bool bold = false,
            TextAlignmentOptions alignment = TextAlignmentOptions.Left)
        {
            var label = Rect("Label", parent).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = bold ? Theme.boldFont : Theme.regularFont;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
            return label;
        }

        public Button Button(Transform parent, string text, Action onClick, ButtonStyle style = ButtonStyle.Secondary, float height = 64f)
        {
            var image = Panel($"Button {text}", parent, ButtonColor(style));
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.colors = new ColorBlock
            {
                normalColor = new Color(0.88f, 0.88f, 0.88f),
                highlightedColor = Color.white,
                selectedColor = Color.white,
                pressedColor = new Color(0.7f, 0.7f, 0.7f),
                disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f),
                colorMultiplier = 1f,
                fadeDuration = 0.08f,
            };
            if (onClick != null) button.onClick.AddListener(new UnityAction(onClick));

            var label = Label(image.transform, text, Theme.bodySize * 0.9f,
                style == ButtonStyle.Secondary ? Theme.textPrimary : Theme.textOnAccent, true, TextAlignmentOptions.Center);
            Stretch(label.rectTransform, 8f);
            Size(image, preferredHeight: height, flexibleWidth: 1f);
            return button;
        }

        public Color ButtonColor(ButtonStyle style) => style switch
        {
            ButtonStyle.Primary => Theme.accent,
            ButtonStyle.Calm => Theme.calm,
            _ => Theme.surfaceRaised,
        };

        public Slider Slider(Transform parent, float min, float max, Action<float> onChanged, Color fillColor, float height = 40f)
        {
            var root = Rect("Slider", parent);
            var slider = root.gameObject.AddComponent<Slider>();
            Size(root, preferredHeight: height, flexibleWidth: 1f);

            var track = Panel("Track", root, Theme.surfaceRaised);
            Stretch(track.rectTransform);
            track.rectTransform.offsetMin = new Vector2(0f, height * 0.3f);
            track.rectTransform.offsetMax = new Vector2(0f, -height * 0.3f);

            var fillArea = Rect("Fill Area", root);
            Stretch(fillArea);
            fillArea.offsetMin = new Vector2(0f, height * 0.3f);
            fillArea.offsetMax = new Vector2(0f, -height * 0.3f);
            var fill = Panel("Fill", fillArea, fillColor);
            fill.rectTransform.sizeDelta = Vector2.zero;

            var handleArea = Rect("Handle Area", root);
            Stretch(handleArea);
            handleArea.offsetMin = new Vector2(height * 0.5f, 0f);
            handleArea.offsetMax = new Vector2(-height * 0.5f, 0f);
            var handle = Circle("Handle", handleArea, Theme.textPrimary);
            handle.rectTransform.sizeDelta = new Vector2(height, 0f);

            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            if (onChanged != null) slider.onValueChanged.AddListener(new UnityAction<float>(onChanged));
            return slider;
        }

        private static Sprite CreateRoundedSprite(int size, int radius)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = $"Rounded{radius}",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                    float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                    float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                    float alpha = Mathf.Clamp01(radius - distance + 0.5f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            texture.Apply(false, true);

            float border = Mathf.Min(radius, size / 2f - 1f);
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }
    }
}
