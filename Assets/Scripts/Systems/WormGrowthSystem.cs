using Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(WormMovementSystem))]
    public partial struct WormGrowthSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var wormLookup = SystemAPI.GetComponentLookup<WormComponent>(false);

            // Worm (head) 
            foreach (var (transform, worm) in SystemAPI
                         .Query<RefRW<LocalTransform>, RefRW<WormComponent>>())
            {
                if (math.abs(worm.ValueRO.CurrentScale - worm.ValueRO.TargetScale) > 0.001f)
                {
                    worm.ValueRW.CurrentScale = math.lerp(
                        worm.ValueRO.CurrentScale,
                        worm.ValueRO.TargetScale,
                        deltaTime * worm.ValueRO.GrowthSpeed
                    );

                    transform.ValueRW.Scale = worm.ValueRO.CurrentScale;
                }
            }

            // Segments
            foreach (var (segment, transform) in SystemAPI
                         .Query<RefRO<WormSegmentComponent>, RefRW<LocalTransform>>())
            {
                if (segment.ValueRO.WormOwner == Entity.Null)
                    continue;

                if (!wormLookup.HasComponent(segment.ValueRO.WormOwner))
                    continue;

                var worm = wormLookup[segment.ValueRO.WormOwner];

                if (math.abs(transform.ValueRO.Scale - worm.CurrentScale) > 0.001f)
                {
                    float newScale = math.lerp(
                        transform.ValueRO.Scale,
                        worm.CurrentScale,
                        deltaTime * worm.GrowthSpeed
                    );

                    transform.ValueRW.Scale = newScale;
                }
            }
        }
    }
}