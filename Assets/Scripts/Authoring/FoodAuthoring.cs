using Components;
using Unity.Entities;
using UnityEngine;

namespace Authoring
{
    public class FoodAuthoring : MonoBehaviour
    {
        public int value = 1;
        public float collisionRadius = 0.5f;

        private class Baker : Baker<FoodAuthoring>
        {
            public override void Bake(FoodAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.Renderable);

                AddComponent(entity, new FoodComponent()
                {
                    Value = authoring.value,
                    CollisionRadius = authoring.collisionRadius
                });
            }
        }
    }
}