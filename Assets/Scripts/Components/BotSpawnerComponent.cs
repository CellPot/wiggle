using Unity.Entities;

namespace Components
{
    public struct BotSpawnerComponent : IComponentData
    {
        public Entity BotPrefab;
        public int MaxBotCount;
        public float SpawnRadius;
    }
}