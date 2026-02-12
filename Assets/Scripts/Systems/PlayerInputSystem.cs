using Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Systems
{
    public partial struct PlayerInputSystem : ISystem
    {
        private float2 _lastMouseWorldPos;
        private bool _isInitialized;

        public void OnUpdate(ref SystemState state)
        {
            //TODO: authored
            var camera = Camera.main;
            if (!camera)
                return;

            var mouseScreenPos = Input.mousePosition;
            var mouseWorldPos =
                (float2)(Vector2)camera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y,
                    camera.nearClipPlane));

            if (!_isInitialized)
            {
                _lastMouseWorldPos = mouseWorldPos;
                _isInitialized = true;
                return;
            }

            float2 mouseDelta = mouseWorldPos - _lastMouseWorldPos;
            float mouseMoveDistance = math.length(mouseDelta);

            if (mouseMoveDistance > 0.1f)
            {
                foreach (var playerMovement in SystemAPI.Query<RefRW<WormMovementComponent>>()
                             .WithAll<PlayerTagComponent>())
                {
                    playerMovement.ValueRW.Direction = math.normalize(mouseDelta);
                }

                _lastMouseWorldPos = mouseWorldPos;
            }
        }
    }
}