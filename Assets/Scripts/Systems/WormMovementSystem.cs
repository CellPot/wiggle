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
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (localTransform, worm) in
                     SystemAPI.Query<RefRW<LocalTransform>, RefRW<WormMovementComponent>>())
            {
                var newAngle = GetNewAngle(worm, deltaTime);

                math.sincos(newAngle, out var sin, out var cos);
                var newDirection = new float2(cos, sin);

                worm.ValueRW.Direction = newDirection;

                localTransform.ValueRW.Position += new float3(newDirection, 0) * worm.ValueRO.Speed * deltaTime;
                localTransform.ValueRW.Rotation = quaternion.RotateZ(newAngle);
            }
        }

        private static float GetNewAngle(RefRW<WormMovementComponent> worm, float deltaTime)
        {
            var targetDir = worm.ValueRO.TargetDirection;
            var currentDir = worm.ValueRO.Direction;

            var targetAngle = math.atan2(targetDir.y, targetDir.x);
            var currentAngle = math.atan2(currentDir.y, currentDir.x);

            var angleDiff = math.atan2(math.sin(targetAngle - currentAngle), math.cos(targetAngle - currentAngle));

            var maxTurn = worm.ValueRO.TurnSpeed * deltaTime;
            var clampedRotation = math.clamp(angleDiff, -maxTurn, maxTurn);

            var newAngle = currentAngle + clampedRotation;
            return newAngle;
        }
    }
}