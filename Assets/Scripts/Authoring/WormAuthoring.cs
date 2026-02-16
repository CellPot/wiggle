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
        public float segmentSpacing = 1f;

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

                AddComponent(entity, new WormComponent()
                {
                    SegmentPrefab = GetEntity(authoring.segmentPrefab, TransformUsageFlags.Dynamic),
                    SegmentSpacing = authoring.segmentSpacing
                });

                AddBuffer<WormPathElement>(entity);
            }
        }
    }
}