using Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(WormMovementSystem))]
    public partial struct FoodSpawnSystem : ISystem
    {
        private uint _randomSeed;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _randomSeed = 1;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float currentTime = (float)SystemAPI.Time.ElapsedTime;

            foreach (var spawnerComponent in SystemAPI.Query<RefRW<FoodSpawnerComponent>>())
            {
                int foodCount = SystemAPI.QueryBuilder().WithAll<FoodComponent>().Build().CalculateEntityCount();
                
                int toSpawn = math.min(spawnerComponent.ValueRO.MaxFoodCount - foodCount, 5);

                if (toSpawn > 0 && currentTime >= spawnerComponent.ValueRO.NextSpawnTime)
                {
                    var random = Random.CreateFromIndex(_randomSeed++);
                    for (int i = 0; i < toSpawn; i++)
                    {
                        float angle = random.NextFloat(0, math.PI * 2);
                        float distance = random.NextFloat(0, spawnerComponent.ValueRO.SpawnRadius);
                    
                        float3 spawnPos = new float3(
                            math.cos(angle) * distance,
                            math.sin(angle) * distance,
                            0
                        );

                        var foodEntity = state.EntityManager.Instantiate(spawnerComponent.ValueRO.FoodPrefab);
                        state.EntityManager.SetComponentData(foodEntity, LocalTransform.FromPosition(spawnPos));
                    }

                    spawnerComponent.ValueRW.NextSpawnTime = currentTime + spawnerComponent.ValueRO.SpawnInterval;
                }
            }
        }
    }
}