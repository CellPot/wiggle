using Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Authoring
{
    public class PlayerAuthoring : MonoBehaviour
    {
        public float speed = 5f;
        public float turnSpeed = 5f;

        private class Baker : Baker<PlayerAuthoring>
        {
            public override void Bake(PlayerAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);

                AddComponent(entity, new WormMovementComponent
                {
                    Speed = authoring.speed,
                    TargetDirection = new float2(1f, 0f),
                    Direction = new float2(1f, 0f),
                    TurnSpeed = authoring.turnSpeed
                });

                AddComponent(entity, new PlayerTagComponent());
            }
        }
    }
}