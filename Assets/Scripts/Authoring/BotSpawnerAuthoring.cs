using Components;
using Unity.Entities;
using UnityEngine;

namespace Authoring
{
    public class BotSpawnerAuthoring : MonoBehaviour
    {
        public GameObject botPrefab;
        public int maxBotCount = 3;
        public float spawnRadius = 20f;

        private class Baker : Baker<BotSpawnerAuthoring>
        {
            public override void Bake(BotSpawnerAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.None);

                AddComponent(entity, new BotSpawnerComponent
                {
                    BotPrefab = GetEntity(authoring.botPrefab, TransformUsageFlags.Dynamic),
                    MaxBotCount = authoring.maxBotCount,
                    SpawnRadius = authoring.spawnRadius
                });
            }
        }
    }
}