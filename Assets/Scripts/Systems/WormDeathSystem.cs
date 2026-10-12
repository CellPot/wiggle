using Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Systems
{
    // A head touching another worm's segment (or another head) kills its owner.
    // A dead worm is destroyed together with its segments, and every segment drops food
    // worth an equal share of the worm's score.
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(WormCollisionScalingSystem))]
    [UpdateAfter(typeof(FoodConsumptionSystem))]
    public partial struct WormDeathSystem : ISystem
    {
        private struct Collider
        {
            public Entity Owner;
            public float2 Position;
            public float Radius;
        }

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<FoodSpawnerComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            var wormLookup = SystemAPI.GetComponentLookup<WormComponent>(true);

            var dead = new NativeHashSet<Entity>(8, Allocator.Temp);

            // Spatial hash of everything a head can collide with: other worms' segments and heads.
            // Each entry carries its owning worm so a head can ignore its own body.
            // Cell size covers the largest possible touching distance, so 3x3 cells are enough.
            float maxRadius = 0f;
            foreach (var collision in SystemAPI.Query<RefRO<WormSegmentCollisionComponent>>())
                maxRadius = math.max(maxRadius, collision.ValueRO.Radius);

            float cellSize = math.max(2f * maxRadius, 0.5f);
            var cells = new NativeParallelMultiHashMap<int, Collider>(1024, Allocator.Temp);

            foreach (var (transform, collision, segment) in SystemAPI
                         .Query<RefRO<LocalTransform>, RefRO<WormSegmentCollisionComponent>,
                             RefRO<WormSegmentComponent>>())
            {
                var owner = segment.ValueRO.WormOwner;
                if (owner == Entity.Null || !wormLookup.HasComponent(owner))
                    continue;

                float2 pos = transform.ValueRO.Position.xy;
                cells.Add((int)math.hash((int2)math.floor(pos / cellSize)),
                    new Collider { Owner = owner, Position = pos, Radius = collision.ValueRO.Radius });
            }

            foreach (var (transform, collision, headEntity) in SystemAPI
                         .Query<RefRO<LocalTransform>, RefRO<WormSegmentCollisionComponent>>()
                         .WithAll<WormComponent>()
                         .WithEntityAccess())
            {
                float2 pos = transform.ValueRO.Position.xy;
                cells.Add((int)math.hash((int2)math.floor(pos / cellSize)),
                    new Collider { Owner = headEntity, Position = pos, Radius = collision.ValueRO.Radius });
            }

            foreach (var (headTransform, headCollision, headEntity) in SystemAPI
                         .Query<RefRO<LocalTransform>, RefRO<WormSegmentCollisionComponent>>()
                         .WithAll<WormComponent>()
                         .WithEntityAccess())
            {
                float2 headPos = headTransform.ValueRO.Position.xy;
                float headRadius = headCollision.ValueRO.Radius;
                int2 headCell = (int2)math.floor(headPos / cellSize);

                bool hit = false;
                for (int dx = -1; dx <= 1 && !hit; dx++)
                {
                    for (int dy = -1; dy <= 1 && !hit; dy++)
                    {
                        int key = (int)math.hash(headCell + new int2(dx, dy));
                        if (!cells.TryGetFirstValue(key, out var other, out var it))
                            continue;

                        do
                        {
                            if (other.Owner == headEntity)
                                continue;

                            if (math.distance(headPos, other.Position) <= headRadius + other.Radius)
                            {
                                hit = true;
                                break;
                            }
                        } while (cells.TryGetNextValue(out other, ref it));
                    }
                }

                if (hit)
                    dead.Add(headEntity);
            }

            cells.Dispose();

            // Segments of dead worms: count them, and clean up segments whose worm is already gone
            var segmentCounts = new NativeHashMap<Entity, int>(8, Allocator.Temp);

            foreach (var (segment, segmentEntity) in SystemAPI
                         .Query<RefRO<WormSegmentComponent>>()
                         .WithEntityAccess())
            {
                var owner = segment.ValueRO.WormOwner;
                if (owner == Entity.Null)
                    continue;

                if (!wormLookup.HasComponent(owner))
                {
                    ecb.DestroyEntity(segmentEntity);
                    continue;
                }

                if (dead.Contains(owner))
                {
                    segmentCounts.TryGetValue(owner, out int count);
                    segmentCounts[owner] = count + 1;
                }
            }

            if (dead.IsEmpty)
                return;

            var spawner = SystemAPI.GetSingleton<FoodSpawnerComponent>();
            var foodTemplate = SystemAPI.GetComponent<FoodComponent>(spawner.FoodPrefab);

            foreach (var (segment, segmentTransform, segmentEntity) in SystemAPI
                         .Query<RefRO<WormSegmentComponent>, RefRO<LocalTransform>>()
                         .WithEntityAccess())
            {
                var owner = segment.ValueRO.WormOwner;
                if (owner == Entity.Null || !dead.Contains(owner))
                    continue;

                // Spread the worm's score evenly over its segments, at least 1 per segment
                int value = math.max(1, wormLookup[owner].Score / segmentCounts[owner]);
                float sizeFactor = math.sqrt(value);

                var food = ecb.Instantiate(spawner.FoodPrefab);
                ecb.SetComponent(food, new FoodComponent
                {
                    Value = value,
                    CollisionRadius = foodTemplate.CollisionRadius * sizeFactor
                });
                ecb.SetComponent(food, LocalTransform.FromPositionRotationScale(
                    new float3(segmentTransform.ValueRO.Position.xy, 0f),
                    quaternion.identity,
                    spawner.FoodScale * sizeFactor));

                ecb.DestroyEntity(segmentEntity);
            }

            // Dead worms are destroyed. BotSpawnSystem refills bots; for the player, a
            // PlayerDeadComponent marker keeps the game-over state until PlayerSpawnSystem respawns it.
            foreach (var deadWorm in dead)
            {
                if (SystemAPI.HasComponent<PlayerTagComponent>(deadWorm))
                {
                    var marker = ecb.CreateEntity();
                    ecb.AddComponent(marker, new PlayerDeadComponent
                    {
                        FinalScore = SystemAPI.GetComponent<WormInfoComponent>(deadWorm).CurrentScore
                    });
                }

                ecb.DestroyEntity(deadWorm);
            }
        }
    }
}
