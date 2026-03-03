using Unity.Entities;

namespace Components
{
    public struct BotComponent : IComponentData
    {
        public float NextDirectionChangeTime;
        public float DirectionChangeInterval;
        public float FoodDetectionRadius;
        public float MaxDistanceFromCenter; // How far from (0,0) they can go
    }
}