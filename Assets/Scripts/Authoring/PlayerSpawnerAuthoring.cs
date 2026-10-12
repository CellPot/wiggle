using Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Authoring
{
    // The player spawns at this object's position (offset randomly within spawnRadius, if set).
    public class PlayerSpawnerAuthoring : MonoBehaviour
    {
        public GameObject playerPrefab;
        public float spawnRadius = 0f;

        // Minimum gap to any worm (head or segment) at the spawn position; the spawn point is
        // moved outward until it is free.
        public float clearance = 3f;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, Mathf.Max(spawnRadius, 0.5f));
        }

        private class Baker : Baker<PlayerSpawnerAuthoring>
        {
            public override void Bake(PlayerSpawnerAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.None);

                var position = authoring.transform.position;

                AddComponent(entity, new PlayerSpawnerComponent
                {
                    PlayerPrefab = GetEntity(authoring.playerPrefab, TransformUsageFlags.Dynamic),
                    SpawnPosition = new float3(position.x, position.y, 0f),
                    SpawnRadius = authoring.spawnRadius,
                    Clearance = authoring.clearance
                });
            }
        }
    }
}
