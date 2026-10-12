using Unity.Entities;
using Unity.Mathematics;

namespace Components
{
    public struct PlayerSpawnerComponent : IComponentData
    {
        public Entity PlayerPrefab;
        public float3 SpawnPosition;
        public float SpawnRadius;
        public float Clearance;
    }
}
