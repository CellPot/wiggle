using Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Systems
{
    // Spawns the player prefab when there is no player and no game-over pending (game start), and
    // again once the game-over UI requests a respawn (after a short delay).
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct PlayerSpawnSystem : ISystem
    {
        public const float MinGameOverTime = 1f;
        private const int MaxSpawnAttempts = 10;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerSpawnerComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var spawner = SystemAPI.GetSingleton<PlayerSpawnerComponent>();

            if (!SystemAPI.QueryBuilder().WithAll<PlayerTagComponent>().Build().IsEmpty)
                return;

            bool dead = SystemAPI.TryGetSingletonEntity<PlayerDeadComponent>(out var deadEntity);

            if (dead)
            {
                var deadState = SystemAPI.GetComponentRW<PlayerDeadComponent>(deadEntity);
                deadState.ValueRW.TimeSinceDeath += SystemAPI.Time.DeltaTime;

                if (!deadState.ValueRO.RespawnRequested)
                    return;

                // Requests during the grace period are dropped
                if (deadState.ValueRO.TimeSinceDeath < MinGameOverTime)
                {
                    deadState.ValueRW.RespawnRequested = false;
                    return;
                }
            }

            var random = Random.CreateFromIndex((uint)(SystemAPI.Time.ElapsedTime * 1000));
            float3 position = spawner.SpawnPosition;

            for (int attempt = 0; attempt < MaxSpawnAttempts; attempt++)
            {
                // First try the configured area, then search progressively further out
                float radius = attempt == 0
                    ? spawner.SpawnRadius
                    : math.max(spawner.SpawnRadius, spawner.Clearance) * (1f + attempt * 0.5f);

                float angle = random.NextFloat(0f, math.PI * 2f);
                float distance = random.NextFloat(0f, radius);
                position = spawner.SpawnPosition + new float3(math.cos(angle), math.sin(angle), 0f) * distance;

                if (IsFree(ref state, position.xy, spawner.Clearance))
                    break;
            }

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            var player = ecb.Instantiate(spawner.PlayerPrefab);
            ecb.SetComponent(player, LocalTransform.FromPosition(position));

            if (dead)
                ecb.DestroyEntity(deadEntity);

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        // No worm head or segment within clearance of the point
        private bool IsFree(ref SystemState state, float2 point, float clearance)
        {
            foreach (var (transform, collision) in SystemAPI
                         .Query<RefRO<LocalTransform>, RefRO<WormSegmentCollisionComponent>>())
            {
                if (math.distance(point, transform.ValueRO.Position.xy) < clearance + collision.ValueRO.Radius)
                    return false;
            }

            return true;
        }
    }
}
