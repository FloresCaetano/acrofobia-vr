using UnityEngine;

namespace Acrofobia.Environment.Canyon
{
    /// <summary>
    /// River bed of the canyon as a separate object. Moving it vertically changes the perceived
    /// height under the bridge without deforming the terrain at runtime.
    /// </summary>
    public sealed class CanyonFloor : MonoBehaviour
    {
        [Tooltip("World height the depth is measured from (the bridge deck at mid-span).")]
        [SerializeField] private float referenceHeight;

        public float DepthBelowReference => referenceHeight - transform.position.y;

        public void SetReferenceHeight(float height) => referenceHeight = height;

        public void SetDepth(float metersBelowReference)
        {
            Vector3 position = transform.position;
            position.y = referenceHeight - Mathf.Max(0f, metersBelowReference);
            transform.position = position;
        }
    }
}
