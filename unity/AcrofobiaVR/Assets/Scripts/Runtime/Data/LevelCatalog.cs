using System.Collections.Generic;
using UnityEngine;

namespace Acrofobia.Data
{
    /// <summary>Ordered list of exposure levels. Adding a level is a matter of adding an asset here.</summary>
    [CreateAssetMenu(menuName = "Acrofobia/Level Catalog", fileName = "LevelCatalog")]
    public sealed class LevelCatalog : ScriptableObject
    {
        [SerializeField] private List<LevelDefinition> levels = new();

        public IReadOnlyList<LevelDefinition> Levels => levels;
        public int Count => levels.Count;

        public LevelDefinition Get(int number)
        {
            foreach (var level in levels)
            {
                if (level != null && level.number == number) return level;
            }
            return null;
        }
    }
}
