using Components;
using Unity.Entities;
using UnityEngine;

namespace Authoring
{
    public class FoodSpawnerAuthoring : MonoBehaviour
    {
        public GameObject foodPrefab;
        public int maxFoodCount = 200;
        public float spawnRadius = 500f;
        public float spawnInterval = 2f;

        class Baker : Baker<FoodSpawnerAuthoring>
        {
            public override void Bake(FoodSpawnerAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);

                AddComponent(entity, new FoodSpawnerComponent
                {
                    FoodPrefab = GetEntity(authoring.foodPrefab, TransformUsageFlags.Dynamic),
                    MaxFoodCount = authoring.maxFoodCount,
                    SpawnRadius = authoring.spawnRadius,
                    SpawnInterval = authoring.spawnInterval
                });
            }
        }
    }
}