using Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Systems
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct WormInitializationSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (transform, worm, wormEntity) in SystemAPI
                         .Query<RefRO<LocalTransform>, RefRW<WormComponent>>()
                         .WithAll<WormNeedsInitializationTag>()
                         .WithEntityAccess())
            {
                // Spawn initial segments
                for (int i = 0; i < worm.ValueRO.InitialSegmentCount; i++)
                {
                    var newSegment = ecb.Instantiate(worm.ValueRO.SegmentPrefab);

                    ecb.SetComponent(newSegment, new WormSegmentComponent
                    {
                        WormOwner = wormEntity,
                        SegmentIndex = i
                    });

                    // Spawn behind the head in a line
                    float3 spawnPos = transform.ValueRO.Position;
                    spawnPos.x -= worm.ValueRO.BaseSegmentSpacing * (i + 1);
                    spawnPos.z = (i + 1) * 0.01f;

                    ecb.SetComponent(newSegment, LocalTransform.FromPositionRotationScale(
                        spawnPos,
                        quaternion.identity,
                        worm.ValueRO.CurrentScale
                    ));
                }

                worm.ValueRW.SegmentCount = worm.ValueRO.InitialSegmentCount;

                // Remove the initialization tag so this only runs once
                ecb.RemoveComponent<WormNeedsInitializationTag>(wormEntity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}