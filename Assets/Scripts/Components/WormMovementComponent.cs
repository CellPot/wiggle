using Unity.Entities;
using Unity.Mathematics;

namespace Components
{
    public struct WormMovementComponent : IComponentData
    {
        public float Speed;
        public float2 Direction;
    }
}