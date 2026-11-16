namespace Acrofobia.Core
{
    /// <summary>Scene identifiers shared by the runtime flow and the editor builder.</summary>
    public static class SceneNames
    {
        public const string Boot = "Boot";
        public const string Neutral = "Neutral";
        public const string Acrophobia = "Acrophobia";

        public static readonly string[] All = { Boot, Neutral, Acrophobia };
    }
}
