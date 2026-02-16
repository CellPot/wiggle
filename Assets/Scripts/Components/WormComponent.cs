using Unity.Entities;

namespace Components
{
    public struct WormComponent : IComponentData
    {
        public Entity SegmentPrefab;
        public float SegmentSpacing;
        
        //Scale
        public float CurrentScale;
        public float TargetScale;
        public float BaseScale;
        public float MaxScale;
        public float ScalePerFood;
        public float GrowthSpeed;
    }
}