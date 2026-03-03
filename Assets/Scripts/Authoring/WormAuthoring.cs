using Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Authoring
{
    public class WormAuthoring : MonoBehaviour
    {
        public float speed = 15f;
        public float turnSpeed = 10f;
        public float collisionRadius = 0.5f;
        public GameObject segmentPrefab;
        public float baseSegmentSpacing = 1f;

        [Header("Growth Settings")] public float baseScale = 1f;
        public float maxScale = 3f;
        public float scalePerScore = 0.01f;
        public float growthSpeed = 2f;

        [Header("Segment Settings")] public int scorePerSegment = 5;
        public int initialSegmentCount = 3;

        private class Baker : Baker<WormAuthoring>
        {
            public override void Bake(WormAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);

                AddComponent(entity, new WormMovementComponent
                {
                    Speed = authoring.speed,
                    TargetDirection = new float2(1f, 0f),
                    Direction = new float2(1f, 0f),
                    TurnSpeed = authoring.turnSpeed
                });

                AddComponent(entity, new WormSegmentCollisionComponent()
                {
                    Radius = authoring.collisionRadius
                });

                AddComponent(entity, new WormComponent
                {
                    SegmentPrefab = GetEntity(authoring.segmentPrefab, TransformUsageFlags.Dynamic),
                    BaseSegmentSpacing = authoring.baseSegmentSpacing,
                    CurrentScale = authoring.baseScale,
                    TargetScale = authoring.baseScale,
                    BaseScale = authoring.baseScale,
                    BaseCollisionRadius = authoring.collisionRadius,
                    MaxScale = authoring.maxScale,
                    GrowthSpeed = authoring.growthSpeed,
                    Score = 0,
                    ScorePerSegment = authoring.scorePerSegment,
                    ScalePerScore = authoring.scalePerScore,
                    LastSegmentScore = 0,
                    InitialSegmentCount = authoring.initialSegmentCount
                });

                AddBuffer<WormPathElement>(entity);
                AddComponent(entity, new WormNeedsInitializationTag());
            }
        }
    }
}