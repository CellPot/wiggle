using Unity.Entities;

namespace Components
{
    // Sprint ability: while requested and not exhausted, the worm moves SpeedMultiplier times
    // faster and drains stamina; stamina regenerates otherwise.
    public struct WormBoostComponent : IComponentData
    {
        public float SpeedMultiplier;
        public float MaxStamina;
        public float DrainPerSecond;
        public float RegenPerSecond;
        // After running dry, boost stays locked until stamina refills to this fraction of max
        public float ResumeFraction;

        public float Stamina;
        public bool Requested;
        public bool Active;
        public bool Exhausted;
    }
}
