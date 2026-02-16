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
        private const float DistanceBufferMultiplier = 1.5f;
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

                if (pathBuffer.Length == 0 || math.distancesq(currentPos, pathBuffer[0].Position) > 0.01f)
                {
                    pathBuffer.Insert(0, new WormPathElement { Position = currentPos });
                }

                int segmentCount = 0;
                if (_wormSegmentCounts.ContainsKey(wormEntity))
                    segmentCount = _wormSegmentCounts[wormEntity];

                float totalDistanceNeeded = segmentCount * worm.ValueRO.SegmentSpacing;

                float accumulatedDistance = 0f;
                int keepUntilIndex = pathBuffer.Length;

                for (int i = 0; i < pathBuffer.Length - 1; i++)
                {
                    float segmentDistance = math.distance(pathBuffer[i].Position, pathBuffer[i + 1].Position);
                    accumulatedDistance += segmentDistance;

                    if (accumulatedDistance > totalDistanceNeeded * DistanceBufferMultiplier)
                    {
                        keepUntilIndex = i + 1;
                        break;
                    }
                }

                while (pathBuffer.Length > keepUntilIndex && pathBuffer.Length > 10)
                {
                    pathBuffer.RemoveAt(pathBuffer.Length - 1);
                }
            }
        }
    }
}