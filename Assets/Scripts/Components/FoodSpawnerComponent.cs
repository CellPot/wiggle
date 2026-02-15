using Unity.Entities;

namespace Components
{
    public struct FoodSpawnerComponent : IComponentData
    {
        public Entity FoodPrefab;
        public int MaxFoodCount;

        public float SpawnRadius;

        public float NextSpawnTime;
        public float SpawnInterval;
        public float FoodScale;
    }
}