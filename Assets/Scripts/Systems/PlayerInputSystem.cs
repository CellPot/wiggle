using Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Systems
{
    [UpdateBefore(typeof(WormMovementSystem))]
    public partial struct PlayerInputSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var camera = Camera.main;
            if (!camera) return;

            var mouseWorldPos = (float2)(Vector2)camera.ScreenToWorldPoint(Input.mousePosition);

            foreach (var (worm, transform) in SystemAPI.Query<RefRW<WormMovementComponent>, RefRO<LocalTransform>>()
                         .WithAll<PlayerTagComponent>())
            {
                var wormPos = new float2(transform.ValueRO.Position.x, transform.ValueRO.Position.y);
                var vectorToMouse = mouseWorldPos - wormPos;

                if (math.lengthsq(vectorToMouse) > 0.1f)
                {
                    worm.ValueRW.TargetDirection = math.normalize(vectorToMouse);
                }

                break;
            }
        }
    }
}