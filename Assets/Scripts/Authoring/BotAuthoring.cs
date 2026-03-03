using Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Authoring
{
    public class BotAuthoring : MonoBehaviour
    {
        public float directionChangeInterval = 2f;
        public float foodDetectionRadius = 15f;
        public float maxDistanceFromCenter = 25f; // Stay within this radius

        private class Baker : Baker<BotAuthoring>
        {
            public override void Bake(BotAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);

                AddComponent(entity, new BotTagComponent());

                AddComponent(entity, new BotComponent
                {
                    NextDirectionChangeTime = 0f,
                    DirectionChangeInterval = authoring.directionChangeInterval,
                    FoodDetectionRadius = authoring.foodDetectionRadius,
                    MaxDistanceFromCenter = authoring.maxDistanceFromCenter
                });
            }
        }
    }
}