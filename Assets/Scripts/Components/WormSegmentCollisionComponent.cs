using Unity.Entities;

namespace Components
{
    public struct WormSegmentCollisionComponent : IComponentData
    {
        // Current radius, rewritten every frame by WormCollisionScalingSystem as
        // WormComponent.BaseCollisionRadius * CurrentScale.
        public float Radius;
    }
}