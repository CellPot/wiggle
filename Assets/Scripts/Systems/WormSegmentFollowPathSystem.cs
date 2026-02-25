using Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(WormPathRecordingSystem))]
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

                var worm = wormLookup[wormOwner];
                var pathBuffer = pathBufferLookup[wormOwner];

                if (pathBuffer.Length < 2)
                    continue;

                float actualSpacing = worm.BaseSegmentSpacing * worm.CurrentScale;
                float targetDistance = (segment.ValueRO.SegmentIndex + 1) * actualSpacing;

                // Use cumulative distances (already calculated in path buffer)
                var last = pathBuffer[pathBuffer.Length - 1];
                var first = pathBuffer[0];
                float desiredCumulative = last.CumulativeDistance - targetDistance;

                // Clamp to valid range
                if (desiredCumulative <= first.CumulativeDistance)
                {
                    float3 pos = first.Position;
                    pos.z = (segment.ValueRO.SegmentIndex + 1) * 0.01f;
                    transform.ValueRW.Position = pos;
                    continue;
                }

                // Binary search - this is actually fine! O(log n) is very fast
                int lo = 0;
                int hi = pathBuffer.Length - 1;
                while (lo + 1 < hi)
                {
                    int mid = (lo + hi) >> 1;
                    if (pathBuffer[mid].CumulativeDistance <= desiredCumulative)
                        lo = mid;
                    else
                        hi = mid;
                }

                // Interpolate position
                var a = pathBuffer[lo];
                var b = pathBuffer[hi];
                float segLen = b.CumulativeDistance - a.CumulativeDistance;

                float3 targetPosition;
                if (segLen > 1e-5f)
                {
                    float t = (desiredCumulative - a.CumulativeDistance) / segLen;
                    targetPosition = math.lerp(a.Position, b.Position, t);
                }
                else
                {
                    targetPosition = a.Position;
                }

                // Set Z-layering
                targetPosition.z = (segment.ValueRO.SegmentIndex + 1) * 0.01f;

                transform.ValueRW.Position = targetPosition;
            }
        }
    }
}