using Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(WormMovementSystem))]
    public partial struct WormPathRecordingSystem : ISystem
    {
        private NativeHashMap<Entity, int> _wormSegmentCounts;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _wormSegmentCounts = new NativeHashMap<Entity, int>(16, Allocator.Persistent);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (_wormSegmentCounts.IsCreated)
                _wormSegmentCounts.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _wormSegmentCounts.Clear();

            // Count segments per worm once per frame
            foreach (var segment in SystemAPI.Query<RefRO<WormSegmentComponent>>())
            {
                if (segment.ValueRO.WormOwner == Entity.Null) continue;

                if (_wormSegmentCounts.ContainsKey(segment.ValueRO.WormOwner))
                    _wormSegmentCounts[segment.ValueRO.WormOwner]++;
                else
                    _wormSegmentCounts.Add(segment.ValueRO.WormOwner, 1);
            }

            foreach (var (transform, worm, pathBuffer, wormEntity) in SystemAPI
                         .Query<RefRO<LocalTransform>, RefRO<WormComponent>, DynamicBuffer<WormPathElement>>()
                         .WithEntityAccess())
            {
                var currentPos = transform.ValueRO.Position;

                // If buffer empty, seed it with current position at distance 0
                if (pathBuffer.Length == 0)
                {
                    pathBuffer.Add(new WormPathElement { Position = currentPos, CumulativeDistance = 0f });
                    continue;
                }

                // Compute distance from last sampled position and append only if moved enough
                var last = pathBuffer[pathBuffer.Length - 1];

                // Use CurrentScale for smooth transitions
                float actualSpacing = worm.ValueRO.BaseSegmentSpacing * worm.ValueRO.CurrentScale;
                float minSampleDistance = math.max(0.05f, actualSpacing * 0.01f); // sample ~4x per spacing

                float moved = math.distance(last.Position, currentPos);
                if (moved >= minSampleDistance)
                {
                    pathBuffer.Add(new WormPathElement
                    {
                        Position = currentPos,
                        CumulativeDistance = last.CumulativeDistance + moved
                    });
                }

                // Prune old samples to keep only the distance we need
                int segmentCount = 0;
                if (_wormSegmentCounts.ContainsKey(wormEntity))
                    segmentCount = _wormSegmentCounts[wormEntity];

                // Keep 3x the needed following distance to handle sharp turns
                float totalDistanceNeeded = (segmentCount + 1) * actualSpacing * 3f;
                float minKeepPoints = 32; // avoid over-pruning for small worms

                // Ensure we have latest last after potential add
                last = pathBuffer[pathBuffer.Length - 1];

                // Remove from the beginning while we exceed needed history
                while (pathBuffer.Length > minKeepPoints)
                {
                    var first = pathBuffer[0];
                    if (last.CumulativeDistance - first.CumulativeDistance > totalDistanceNeeded)
                    {
                        pathBuffer.RemoveAt(0);
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
    }
}