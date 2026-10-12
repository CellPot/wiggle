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

        public int Score;
        public int ScorePerSegment;
        public float ScalePerScore;
        public int LastSegmentScore;

        public int InitialSegmentCount;
        public int SegmentCount;
    }
}