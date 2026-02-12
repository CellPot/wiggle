using Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct WormMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (localTransform, wormMovement) in
                     SystemAPI.Query<RefRW<LocalTransform>, RefRO<WormMovementComponent>>())
            {
                float2 moveVector = wormMovement.ValueRO.Direction * wormMovement.ValueRO.Speed * deltaTime;
                localTransform.ValueRW.Position += new float3(moveVector.x, moveVector.y, 0);
            }
        }
    }
}