using Components;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Mono.Camera
{
    public class CameraFollowPlayer : MonoBehaviour
    {
        private EntityManager _entityManager;
        private EntityQuery _playerQuery;

        private void Start()
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _playerQuery = _entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.ReadOnly<PlayerTagComponent>()
            );
        }

        private void Update()
        {
            if (_playerQuery.IsEmpty) return;

            if (_playerQuery.TryGetSingleton<LocalTransform>(out var playerTransform))
            {
                var playerPos = playerTransform.Position;
                transform.position = new Vector3(playerPos.x, playerPos.y, transform.position.z);
            }
        }
    }
}