using Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(WormMovementSystem))]
    public partial struct WormSegmentFollowPathSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var wormLookup = SystemAPI.GetComponentLookup<WormComponent>(true);
            var pathBufferLookup = SystemAPI.GetBufferLookup<WormPathElement>(true);

            foreach (var (segment, transform) in SystemAPI
                         .Query<RefRO<WormSegmentComponent>, RefRW<LocalTransform>>())
            {
                var wormOwner = segment.ValueRO.WormOwner;

                if (wormOwner == Entity.Null || !wormLookup.HasComponent(wormOwner))
                    continue;

                var wormHead = wormLookup[wormOwner];
                var pathBuffer = pathBufferLookup[wormOwner];

                if (pathBuffer.Length < 2)
                    continue;

                float targetDistance = (segment.ValueRO.SegmentIndex + 1) * wormHead.SegmentSpacing;

                float accumulatedDistance = 0f;
                float3 targetPosition = transform.ValueRO.Position;
                int pathLength = pathBuffer.Length;

                for (int i = 0; i < pathLength - 1; i++)
                {
                    float3 from = pathBuffer[i].Position;
                    float3 to = pathBuffer[i + 1].Position;
                    float segmentLength = math.distance(from, to);

                    if (accumulatedDistance + segmentLength >= targetDistance)
                    {
                        float remaining = targetDistance - accumulatedDistance;
                        float t = remaining / segmentLength;
                        targetPosition = math.lerp(from, to, t);
                        break;
                    }

                    accumulatedDistance += segmentLength;
                }

                transform.ValueRW.Position = targetPosition;
            }
        }
    }
}