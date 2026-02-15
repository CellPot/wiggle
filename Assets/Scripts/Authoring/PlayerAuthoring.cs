using Components;
using Unity.Entities;
using UnityEngine;

namespace Authoring
{
    public class PlayerAuthoring : MonoBehaviour
    {
        private class Baker : Baker<PlayerAuthoring>
        {
            public override void Bake(PlayerAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);

                AddComponent(entity, new PlayerTagComponent());
            }
        }
    }
}