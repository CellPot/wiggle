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
            var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);

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

                // Use CurrentScale for smooth transitions
                float actualSpacing = worm.BaseSegmentSpacing * worm.CurrentScale;
                float targetDistance = (segment.ValueRO.SegmentIndex + 1) * actualSpacing;

                // Using cumulative distance: newest sample is at the end
                var last = pathBuffer[pathBuffer.Length - 1];
                var first = pathBuffer[0];
                float availablePath = last.CumulativeDistance - first.CumulativeDistance;

                // If we don't have enough path, follow the segment in front or the head
                if (availablePath < targetDistance)
                {
                    float3 targetPosition = transform.ValueRO.Position;

                    if (segment.ValueRO.SegmentIndex == 0)
                    {
                        // First segment - follow the head at closest available distance
                        if (transformLookup.HasComponent(wormOwner))
                        {
                            var headPos = transformLookup[wormOwner].Position;
                            float3 dirToHead = headPos - transform.ValueRO.Position;
                            float distToHead = math.length(dirToHead);

                            if (distToHead > actualSpacing)
                            {
                                targetPosition = headPos - math.normalize(dirToHead) * actualSpacing;
                            }
                        }
                    }
                    else
                    {
                        // Follow the segment in front at current spacing
                        foreach (var (otherSegment, otherTransform) in SystemAPI
                                     .Query<RefRO<WormSegmentComponent>, RefRO<LocalTransform>>())
                        {
                            if (otherSegment.ValueRO.WormOwner == wormOwner &&
                                otherSegment.ValueRO.SegmentIndex == segment.ValueRO.SegmentIndex - 1)
                            {
                                float3 dirToTarget = otherTransform.ValueRO.Position - transform.ValueRO.Position;
                                float distToTarget = math.length(dirToTarget);

                                if (distToTarget > actualSpacing)
                                {
                                    targetPosition = otherTransform.ValueRO.Position -
                                                     math.normalize(dirToTarget) * actualSpacing;
                                }

                                break;
                            }
                        }
                    }

                    transform.ValueRW.Position = targetPosition;
                    continue;
                }

                // Normal path following using cumulative distances with binary search
                float desiredCumulative = last.CumulativeDistance - targetDistance;

                // Clamp to available range for extra safety
                if (desiredCumulative <= first.CumulativeDistance)
                {
                    transform.ValueRW.Position = first.Position;
                    continue;
                }

                // Binary search for the segment [lo, hi] such that
                // buffer[lo].cum <= desired < buffer[hi].cum
                int lo = 0;
                int hi = pathBuffer.Length - 1;
                while (lo + 1 < hi)
                {
                    int mid = (lo + hi) >> 1;
                    float midCum = pathBuffer[mid].CumulativeDistance;
                    if (midCum <= desiredCumulative)
                        lo = mid;
                    else
                        hi = mid;
                }

                var a = pathBuffer[lo];
                var b = pathBuffer[hi];
                float segLen = math.max(1e-5f, b.CumulativeDistance - a.CumulativeDistance);
                float tLerp = math.saturate((desiredCumulative - a.CumulativeDistance) / segLen);
                float3 interpPos = math.lerp(a.Position, b.Position, tLerp);
                transform.ValueRW.Position = interpPos;
            }
        }
    }
}