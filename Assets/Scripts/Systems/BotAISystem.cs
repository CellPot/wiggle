using Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(WormMovementSystem))]
    public partial struct BotAISystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<FoodGridComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var grid = SystemAPI.GetSingleton<FoodGridComponent>();
            var currentTime = (float)SystemAPI.Time.ElapsedTime;

            foreach (var (bot, movement, transform, entity) in SystemAPI
                         .Query<RefRW<BotComponent>, RefRW<WormMovementComponent>, RefRO<LocalTransform>>()
                         .WithEntityAccess())
            {
                float2 botPos = transform.ValueRO.Position.xy;
                float distanceFromCenter = math.length(botPos);

                // PRIORITY 1: If way too far from center, head straight back
                if (distanceFromCenter > bot.ValueRO.MaxDistanceFromCenter)
                {
                    float2 directionToCenter = math.normalize(-botPos);
                    movement.ValueRW.TargetDirection = directionToCenter;
                    continue;
                }

                // PRIORITY 2: Look for food (only nearby grid cells)
                bool foundFood = false;
                float2 foodPos = float2.zero;
                float closestDistance = float.MaxValue;
                float detectionRadius = bot.ValueRO.FoodDetectionRadius;

                FoodGridComponent.CellRange(botPos, detectionRadius, out var minCell, out var maxCell);
                for (int cy = minCell.y; cy <= maxCell.y; cy++)
                for (int cx = minCell.x; cx <= maxCell.x; cx++)
                {
                    if (!grid.Cells.TryGetFirstValue(FoodGridComponent.Key(new int2(cx, cy)),
                            out var food, out var it))
                        continue;

                    do
                    {
                        float distance = math.distance(botPos, food.Position);
                        if (distance < detectionRadius && distance < closestDistance)
                        {
                            closestDistance = distance;
                            foodPos = food.Position;
                            foundFood = true;
                        }
                    } while (grid.Cells.TryGetNextValue(out food, ref it));
                }

                if (foundFood)
                {
                    // Found food - but check if chasing it would take us too far
                    float foodDistanceFromCenter = math.length(foodPos);

                    // Only chase food if it's not taking us further from center when we're already far
                    if (distanceFromCenter > bot.ValueRO.MaxDistanceFromCenter * 0.6f &&
                        foodDistanceFromCenter > distanceFromCenter)
                    {
                        // Food is further out - ignore it and turn towards center instead
                        float2 directionToCenter = math.normalize(-botPos);
                        movement.ValueRW.TargetDirection = directionToCenter;
                    }
                    else
                    {
                        // Safe to chase this food
                        float2 directionToFood = math.normalize(foodPos - botPos);
                        movement.ValueRW.TargetDirection = directionToFood;
                    }
                }
                else if (currentTime >= bot.ValueRO.NextDirectionChangeTime)
                {
                    // No food - wander with strong center bias when far
                    var random = Unity.Mathematics.Random.CreateFromIndex((uint)(currentTime * 1000 + entity.Index));

                    // Calculate how far we are as a percentage (0 = center, 1 = at boundary)
                    float distanceRatio = distanceFromCenter / bot.ValueRO.MaxDistanceFromCenter;

                    if (distanceRatio > 0.5f)
                    {
                        // We're in outer 50% - strongly bias towards center
                        float2 toCenter = math.normalize(-botPos);
                        float centerBias = (distanceRatio - 0.5f) * 2f; // 0 to 1 as we go from 50% to 100%

                        // Random angle, but weighted towards center
                        float randomAngle = random.NextFloat(-math.PI * 0.25f, math.PI * 0.25f);
                        float centerAngle = math.atan2(toCenter.y, toCenter.x);
                        float targetAngle = math.lerp(centerAngle + randomAngle, centerAngle, centerBias);

                        float2 newDirection = new float2(math.cos(targetAngle), math.sin(targetAngle));
                        movement.ValueRW.TargetDirection = math.normalize(newDirection);
                    }
                    else
                    {
                        // We're in inner 50% - free to wander
                        float2 botDirection = movement.ValueRO.Direction;
                        float currentAngle = math.atan2(botDirection.y, botDirection.x);
                        float randomTurn = random.NextFloat(-math.PI * 0.3f, math.PI * 0.3f);
                        float newAngle = currentAngle + randomTurn;

                        float2 newDirection = new float2(math.cos(newAngle), math.sin(newAngle));
                        movement.ValueRW.TargetDirection = math.normalize(newDirection);
                    }

                    bot.ValueRW.NextDirectionChangeTime = currentTime + bot.ValueRO.DirectionChangeInterval;
                }
            }
        }
    }
}