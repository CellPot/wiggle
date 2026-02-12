using Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Authoring
{
    public class PlayerAuthoring : MonoBehaviour
    {
        public float speed = 5f;

        private class Baker : Baker<PlayerAuthoring>
        {
            public override void Bake(PlayerAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);

                AddComponent(entity, new WormMovementComponent
                {
                    Speed = authoring.speed,
                    Direction = float2.zero
                });

                AddComponent(entity, new PlayerTagComponent());
            }
        }
    }
}