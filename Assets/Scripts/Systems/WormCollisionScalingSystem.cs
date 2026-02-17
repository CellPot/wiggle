using Components;
using Unity.Burst;
using Unity.Entities;

namespace Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(WormGrowthSystem))]
    public partial struct WormCollisionScalingSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var wormLookup = SystemAPI.GetComponentLookup<WormComponent>(true);

            // Head
            foreach (var (worm, collision) in SystemAPI
                         .Query<RefRO<WormComponent>, RefRW<WormSegmentCollisionComponent>>())
            {
                collision.ValueRW.Radius = worm.ValueRO.BaseCollisionRadius * worm.ValueRO.CurrentScale;
            }

            // Segment
            foreach (var (segment, collision) in SystemAPI
                         .Query<RefRO<WormSegmentComponent>, RefRW<WormSegmentCollisionComponent>>())
            {
                if (segment.ValueRO.WormOwner == Entity.Null)
                    continue;

                if (!wormLookup.HasComponent(segment.ValueRO.WormOwner))
                    continue;

                var worm = wormLookup[segment.ValueRO.WormOwner];
                collision.ValueRW.Radius = worm.BaseCollisionRadius * worm.CurrentScale;
            }
        }
    }
}