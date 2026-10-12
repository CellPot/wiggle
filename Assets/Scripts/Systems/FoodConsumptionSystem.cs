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
    [UpdateAfter(typeof(FoodGridSystem))]
    public partial struct FoodConsumptionSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<FoodGridComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            var grid = SystemAPI.GetSingleton<FoodGridComponent>();

            // Food already eaten this frame; destruction is deferred, so track it here
            var eaten = new NativeHashSet<Entity>(16, Allocator.Temp);

            foreach (var (wormTransform, wormCollision, worm, wormEntity) in SystemAPI
                         .Query<RefRO<LocalTransform>, RefRO<WormSegmentCollisionComponent>,
                             RefRW<WormComponent>>()
                         .WithEntityAccess())
            {
                float2 wormPos = wormTransform.ValueRO.Position.xy;
                float wormRadius = wormCollision.ValueRO.Radius;

                FoodGridComponent.CellRange(wormPos, wormRadius + grid.MaxFoodRadius,
                    out var minCell, out var maxCell);

                for (int cy = minCell.y; cy <= maxCell.y; cy++)
                for (int cx = minCell.x; cx <= maxCell.x; cx++)
                {
                    int key = FoodGridComponent.Key(new int2(cx, cy));
                    if (!grid.Cells.TryGetFirstValue(key, out var food, out var it))
                        continue;

                    do
                    {
                        if (eaten.Contains(food.Entity))
                            continue;

                        if (math.distance(wormPos, food.Position) > wormRadius + food.Radius)
                            continue;

                        eaten.Add(food.Entity);
                        ecb.DestroyEntity(food.Entity);
                        Eat(ref worm.ValueRW, wormEntity, wormTransform.ValueRO.Position, food.Value, ecb);
                    } while (grid.Cells.TryGetNextValue(out food, ref it));
                }
            }

            eaten.Dispose();
        }

        private static void Eat(ref WormComponent worm, Entity wormEntity, float3 wormPosition, int foodValue,
            EntityCommandBuffer ecb)
        {
            worm.Score += foodValue;
            worm.TargetScale = math.min(worm.TargetScale + foodValue * worm.ScalePerScore, worm.MaxScale);

            int scoreGained = worm.Score - worm.LastSegmentScore;
            if (scoreGained < worm.ScorePerSegment)
                return;

            int segmentsToAdd = scoreGained / worm.ScorePerSegment;
            worm.LastSegmentScore += segmentsToAdd * worm.ScorePerSegment;

            for (int i = 0; i < segmentsToAdd; i++)
            {
                var newSegment = ecb.Instantiate(worm.SegmentPrefab);
                int newSegmentIndex = worm.SegmentCount + i;

                ecb.SetComponent(newSegment, new WormSegmentComponent
                {
                    WormOwner = wormEntity,
                    SegmentIndex = newSegmentIndex
                });

                wormPosition.z = (newSegmentIndex + 1) * 0.01f;
                ecb.SetComponent(newSegment, LocalTransform.FromPositionRotationScale(
                    wormPosition, quaternion.identity, worm.CurrentScale));
            }

            worm.SegmentCount += segmentsToAdd;
        }
    }
}
