using Unity.Entities;

namespace Components
{
    public struct WormComponent : IComponentData
    {
        public Entity SegmentPrefab;
        public float SegmentSpacing;
    }
}