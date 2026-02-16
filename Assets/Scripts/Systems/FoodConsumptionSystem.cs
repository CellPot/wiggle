using Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(WormMovementSystem))]
    public partial struct FoodConsumptionSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            foreach (var (wormTransform, wormCollision, worm, wormEntity) in SystemAPI
                         .Query<RefRO<LocalTransform>, RefRO<WormSegmentCollisionComponent>, RefRW<WormComponent>>()
                         .WithEntityAccess())
            {
                float2 wormPos = wormTransform.ValueRO.Position.xy;

                foreach (var (foodTransform, foodComponent, foodEntity) in SystemAPI
                             .Query<RefRO<LocalTransform>, RefRO<FoodComponent>>().WithEntityAccess())
                {
                    float2 foodPos = foodTransform.ValueRO.Position.xy;
                    float distance = math.distance(wormPos, foodPos);
                    float combinedRadius = wormCollision.ValueRO.Radius + foodComponent.ValueRO.CollisionRadius;

                    if (distance <= combinedRadius)
                    {
                        ecb.DestroyEntity(foodEntity);

                        float newTargetScale = math.min(
                            worm.ValueRO.TargetScale + worm.ValueRO.ScalePerFood,
                            worm.ValueRO.MaxScale
                        );

                        worm.ValueRW.TargetScale = newTargetScale;

                        int segmentCount = 0;
                        foreach (var segmentComponent in SystemAPI.Query<RefRO<WormSegmentComponent>>())
                        {
                            if (segmentComponent.ValueRO.WormOwner == wormEntity)
                                segmentCount++;
                        }

                        var newSegment = ecb.Instantiate(worm.ValueRO.SegmentPrefab);
                        ecb.SetComponent(newSegment,
                            new WormSegmentComponent { WormOwner = wormEntity, SegmentIndex = segmentCount });
                        ecb.SetComponent(newSegment, LocalTransform.FromPositionRotationScale(
                            wormTransform.ValueRO.Position,
                            quaternion.identity,
                            worm.ValueRO.CurrentScale
                        ));
                    }
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}