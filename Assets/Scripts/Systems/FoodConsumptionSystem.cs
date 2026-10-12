using Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(WormPathRecordingSystem))]
    public partial struct FoodConsumptionSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            // Food already eaten this frame; destruction is deferred, so track it here
            var eaten = new NativeHashSet<Entity>(16, Allocator.Temp);

            foreach (var (wormTransform, wormCollision, worm, wormEntity) in SystemAPI
                         .Query<RefRO<LocalTransform>, RefRO<WormSegmentCollisionComponent>,
                             RefRW<WormComponent>>()
                         .WithEntityAccess())
            {
                float2 wormPos = wormTransform.ValueRO.Position.xy;

                foreach (var (foodTransform, foodComponent, foodEntity) in SystemAPI
                             .Query<RefRO<LocalTransform>, RefRO<FoodComponent>>().WithEntityAccess())
                {
                    if (eaten.Contains(foodEntity))
                        continue;

                    float2 foodPos = foodTransform.ValueRO.Position.xy;
                    float combinedRadius = wormCollision.ValueRO.Radius + foodComponent.ValueRO.CollisionRadius;

                    if (math.distance(wormPos, foodPos) > combinedRadius)
                        continue;

                    eaten.Add(foodEntity);
                    ecb.DestroyEntity(foodEntity);

                    int foodValue = foodComponent.ValueRO.Value;
                    worm.ValueRW.Score += foodValue;

                    float growthAmount = foodValue * worm.ValueRO.ScalePerScore;
                    worm.ValueRW.TargetScale = math.min(
                        worm.ValueRO.TargetScale + growthAmount,
                        worm.ValueRO.MaxScale
                    );

                    int scoreGained = worm.ValueRO.Score - worm.ValueRO.LastSegmentScore;
                    if (scoreGained < worm.ValueRO.ScorePerSegment)
                        continue;

                    int segmentsToAdd = scoreGained / worm.ValueRO.ScorePerSegment;
                    worm.ValueRW.LastSegmentScore += segmentsToAdd * worm.ValueRO.ScorePerSegment;

                    for (int i = 0; i < segmentsToAdd; i++)
                    {
                        var newSegment = ecb.Instantiate(worm.ValueRO.SegmentPrefab);
                        int newSegmentIndex = worm.ValueRO.SegmentCount + i;

                        ecb.SetComponent(newSegment, new WormSegmentComponent
                        {
                            WormOwner = wormEntity,
                            SegmentIndex = newSegmentIndex
                        });

                        float3 spawnPos = wormTransform.ValueRO.Position;
                        spawnPos.z = (newSegmentIndex + 1) * 0.01f;

                        ecb.SetComponent(newSegment, LocalTransform.FromPositionRotationScale(
                            spawnPos,
                            quaternion.identity,
                            worm.ValueRO.CurrentScale
                        ));
                    }

                    worm.ValueRW.SegmentCount += segmentsToAdd;
                }
            }

            eaten.Dispose();
        }
    }
}
