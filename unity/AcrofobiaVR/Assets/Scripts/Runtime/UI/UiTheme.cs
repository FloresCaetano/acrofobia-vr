using TMPro;
using UnityEngine;

namespace Acrofobia.UI
{
    /// <summary>
    /// Fonts and palette shared by every panel. Night blue and teal, with amber reserved for the calm button.
    /// Text colors keep at least a 4.5:1 contrast ratio over their backgrounds.
    /// </summary>
    [CreateAssetMenu(menuName = "Acrofobia/UI Theme", fileName = "UiTheme")]
    public sealed class UiTheme : ScriptableObject
    {
        public TMP_FontAsset regularFont;
        public TMP_FontAsset boldFont;

        [Header("Surfaces")]
        public Color background = new(0.059f, 0.106f, 0.176f, 0.94f);
        public Color surface = new(0.086f, 0.149f, 0.239f, 1f);
        public Color surfaceRaised = new(0.137f, 0.212f, 0.318f, 1f);

        [Header("Text")]
        public Color textPrimary = new(0.957f, 0.969f, 0.980f, 1f);
        public Color textSecondary = new(0.722f, 0.769f, 0.827f, 1f);
        public Color textOnAccent = new(0.043f, 0.090f, 0.149f, 1f);

        [Header("Accents")]
        public Color accent = new(0.169f, 0.702f, 0.659f, 1f);
        public Color calm = new(0.949f, 0.663f, 0.231f, 1f);
        public Color danger = new(0.910f, 0.416f, 0.353f, 1f);

        [Header("Sizes (reference resolution 1920×1080)")]
        public float titleSize = 44f;
        public float bodySize = 30f;
        public float smallSize = 24f;
    }
}
