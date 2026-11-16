using UnityEngine;

namespace Acrofobia.Environment
{
    /// <summary>Dust and debris carried by the wind; the emission rate follows the wind stimulus.</summary>
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class WindParticles : MonoBehaviour
    {
        private ParticleSystem particles;

        public void SetRate(float particlesPerSecond)
        {
            // Resolved lazily so the method also works in edit mode, where Awake does not run.
            if (particles == null) particles = GetComponent<ParticleSystem>();
            var emission = particles.emission;
            emission.rateOverTime = particlesPerSecond;
        }
    }
}
