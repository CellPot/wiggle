using Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Systems
{
    // While the player is dead (disabled, see WormDeathSystem), waits for the game-over UI to
    // request a respawn (after a short delay) and resets the same entity as a fresh worm.
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(WormDeathSystem))]
    public partial struct PlayerRespawnSystem : ISystem
    {
        public const float MinGameOverTime = 1f;
        private const float DefaultSpawnRadius = 20f;

        public void OnUpdate(ref SystemState state)
        {
            float spawnRadius = SystemAPI.TryGetSingleton<BotSpawnerComponent>(out var botSpawner)
                ? botSpawner.SpawnRadius
                : DefaultSpawnRadius;

            float deltaTime = SystemAPI.Time.DeltaTime;

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (dead, worm, movement, transform, collision, info, path, entity) in SystemAPI
                         .Query<RefRW<PlayerDeadComponent>, RefRW<WormComponent>, RefRW<WormMovementComponent>,
                             RefRW<LocalTransform>, RefRW<WormSegmentCollisionComponent>,
                             RefRW<WormInfoComponent>, DynamicBuffer<WormPathElement>>()
                         .WithAll<PlayerTagComponent, Disabled>()
                         .WithEntityAccess())
            {
                dead.ValueRW.TimeSinceDeath += deltaTime;

                if (!dead.ValueRO.RespawnRequested)
                    continue;

                // Requests during the grace period are dropped
                if (dead.ValueRO.TimeSinceDeath < MinGameOverTime)
                {
                    dead.ValueRW.RespawnRequested = false;
                    continue;
                }

                var random = Unity.Mathematics.Random.CreateFromIndex((uint)(SystemAPI.Time.ElapsedTime * 1000));
                float angle = random.NextFloat(0f, math.PI * 2f);
                float distance = random.NextFloat(0f, spawnRadius);

                ref var w = ref worm.ValueRW;
                w.Score = 0;
                w.LastSegmentScore = 0;
                w.SegmentCount = 0;
                w.CurrentScale = w.BaseScale;
                w.TargetScale = w.BaseScale;

                movement.ValueRW.Direction = new float2(1f, 0f);
                movement.ValueRW.TargetDirection = new float2(1f, 0f);

                collision.ValueRW.Radius = w.BaseCollisionRadius * w.BaseScale;
                info.ValueRW.CurrentScore = 0;
                path.Clear();

                transform.ValueRW = LocalTransform.FromPositionRotationScale(
                    new float3(math.cos(angle) * distance, math.sin(angle) * distance, 0f),
                    quaternion.identity,
                    w.BaseScale);

                ecb.RemoveComponent<Disabled>(entity);
                ecb.RemoveComponent<PlayerDeadComponent>(entity);
                ecb.AddComponent<WormNeedsInitializationTag>(entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
