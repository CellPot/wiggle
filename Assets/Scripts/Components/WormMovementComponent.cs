using Unity.Entities;
using Unity.Mathematics;

namespace Components
{
    public struct WormMovementComponent : IComponentData
    {
        public float Speed;
        public float TurnSpeed;
        public float2 Direction;
        public float2 TargetDirection;
    }
}