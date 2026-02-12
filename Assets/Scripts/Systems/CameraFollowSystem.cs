using Components;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Systems
{
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial struct CameraFollowSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var camera = Camera.main;
            if (!camera)
                return;

            foreach (var playerTransform in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<PlayerTagComponent>())
            {
                var playerPos = playerTransform.ValueRO.Position;
                camera.transform.position = new Vector3(playerPos.x, playerPos.y, camera.transform.position.z);
                break;
            }
        }
    }
}