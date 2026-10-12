using Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Systems
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct BotSpawnSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var spawner in SystemAPI.Query<RefRW<BotSpawnerComponent>>())
            {
                int botCount = 0;
                foreach (var bot in SystemAPI.Query<RefRO<BotTagComponent>>())
                {
                    botCount++;
                }

                int botsToSpawn = spawner.ValueRO.MaxBotCount - botCount;

                if (botsToSpawn > 0)
                {
                    var random = Unity.Mathematics.Random.CreateFromIndex((uint)(SystemAPI.Time.ElapsedTime * 1000));

                    for (int i = 0; i < botsToSpawn; i++)
                    {
                        var bot = ecb.Instantiate(spawner.ValueRO.BotPrefab);

                        float angle = random.NextFloat(0f, math.PI * 2f);
                        float distance = random.NextFloat(5f, spawner.ValueRO.SpawnRadius);
                        float3 spawnPos = new float3(
                            math.cos(angle) * distance,
                            math.sin(angle) * distance,
                            0f
                        );

                        ecb.SetComponent(bot, LocalTransform.FromPosition(spawnPos));

                        // Build bot name manually for Burst compatibility
                        int botNumber = ++spawner.ValueRW.NextBotNumber;
                        FixedString64Bytes botName = new FixedString64Bytes();
                        botName.Append((FixedString32Bytes)"Bot ");
                        botName.Append(botNumber);

                        ecb.SetComponent(bot, new WormInfoComponent
                        {
                            CurrentScore = 0,
                            Name = botName
                        });
                    }
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}