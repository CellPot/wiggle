using Unity.Entities;

namespace Components
{
    public struct WormComponent : IComponentData
    {
        public Entity SegmentPrefab;
        public float BaseSegmentSpacing;
        public float CurrentScale;
        public float TargetScale;
        public float BaseScale;
        public float MaxScale;
        public float GrowthSpeed;
        public float BaseCollisionRadius;

        // Score-based system
        public int Score; // Total score accumulated
        public int ScorePerSegment; // Score needed to add a segment
        public float ScalePerScore; // How much to grow per score point
        public int LastSegmentScore; // Score when we last added a segment
    }
}