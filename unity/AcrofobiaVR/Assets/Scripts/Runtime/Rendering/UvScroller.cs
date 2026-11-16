using UnityEngine;

namespace Acrofobia.Rendering
{
    /// <summary>Scrolls a material's base map (and its normal map, which shares the tiling) to fake flowing water.</summary>
    [RequireComponent(typeof(Renderer))]
    public sealed class UvScroller : MonoBehaviour
    {
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

        [SerializeField] private Vector2 speed = new(0.02f, 0.05f);

        private Material material;
        private Vector2 offset;

        private void Awake()
        {
            // Instance the material so the shared asset is never modified at runtime.
            material = GetComponent<Renderer>().material;
        }

        private void Update()
        {
            offset = new Vector2((offset.x + speed.x * Time.deltaTime) % 1f, (offset.y + speed.y * Time.deltaTime) % 1f);
            material.SetTextureOffset(BaseMapId, offset);
        }

        private void OnDestroy()
        {
            if (material != null) Destroy(material);
        }
    }
}
