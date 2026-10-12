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

            foreach (var (headTransform, headCollision, headEntity) in SystemAPI
                         .Query<RefRO<LocalTransform>, RefRO<WormSegmentCollisionComponent>>()
                         .WithAll<WormComponent>()
                         .WithEntityAccess())
            {
                float2 headPos = headTransform.ValueRO.Position.xy;
                float headRadius = headCollision.ValueRO.Radius;

                foreach (var (segTransform, segCollision, segment) in SystemAPI
                             .Query<RefRO<LocalTransform>, RefRO<WormSegmentCollisionComponent>,
                                 RefRO<WormSegmentComponent>>())
                {
                    var owner = segment.ValueRO.WormOwner;
                    if (owner == Entity.Null || owner == headEntity || !wormLookup.HasComponent(owner))
                        continue;

                    if (math.distance(headPos, segTransform.ValueRO.Position.xy) <=
                        headRadius + segCollision.ValueRO.Radius)
                    {
                        dead.Add(headEntity);
                        break;
                    }
                }

                if (dead.Contains(headEntity))
                    continue;

                foreach (var (otherTransform, otherCollision, otherEntity) in SystemAPI
                             .Query<RefRO<LocalTransform>, RefRO<WormSegmentCollisionComponent>>()
                             .WithAll<WormComponent>()
                             .WithEntityAccess())
                {
                    if (otherEntity == headEntity)
                        continue;

                    if (math.distance(headPos, otherTransform.ValueRO.Position.xy) <=
                        headRadius + otherCollision.ValueRO.Radius)
                    {
                        dead.Add(headEntity);
                        break;
                    }
                }
            }

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

            foreach (var deadWorm in dead)
                ecb.DestroyEntity(deadWorm);
        }
    }
}
