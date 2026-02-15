using Unity.Entities;

namespace Components
{
    public struct FoodComponent : IComponentData
    {
        public int Value;
        public float CollisionRadius;
    }
}