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
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            foreach (var (wormTransform, wormCollision, worm, wormEntity) in SystemAPI
                         .Query<RefRO<LocalTransform>, RefRO<WormSegmentCollisionComponent>,
                             RefRW<WormComponent>>()
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

                        int foodValue = foodComponent.ValueRO.Value;
                        worm.ValueRW.Score += foodValue;

                        float growthAmount = foodValue * worm.ValueRO.ScalePerScore;
                        float newTargetScale = math.min(
                            worm.ValueRO.TargetScale + growthAmount,
                            worm.ValueRO.MaxScale
                        );
                        worm.ValueRW.TargetScale = newTargetScale;

                        int scoreGained = worm.ValueRO.Score - worm.ValueRO.LastSegmentScore;
                        if (scoreGained >= worm.ValueRO.ScorePerSegment)
                        {
                            int segmentsToAdd = scoreGained / worm.ValueRO.ScorePerSegment;
                            worm.ValueRW.LastSegmentScore += segmentsToAdd * worm.ValueRO.ScorePerSegment;

                            int segmentCount = 0;
                            foreach (var segment in SystemAPI.Query<RefRO<WormSegmentComponent>>())
                            {
                                if (segment.ValueRO.WormOwner == wormEntity)
                                    segmentCount++;
                            }

                            for (int i = 0; i < segmentsToAdd; i++)
                            {
                                var newSegment = ecb.Instantiate(worm.ValueRO.SegmentPrefab);
                                ecb.SetComponent(newSegment, new WormSegmentComponent
                                {
                                    WormOwner = wormEntity,
                                    SegmentIndex = segmentCount + i
                                });

                                int newSegmentIndex = segmentCount + i;
                                float3 spawnPos = wormTransform.ValueRO.Position;
                                spawnPos.z = (newSegmentIndex + 1) * 0.01f;

                                ecb.SetComponent(newSegment, LocalTransform.FromPositionRotationScale(
                                    spawnPos,
                                    quaternion.identity,
                                    worm.ValueRO.CurrentScale
                                ));
                            }
                        }
                    }
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}