using Components;
using Unity.Entities;
using UnityEngine;

namespace Authoring
{
    public class WormSegmentAuthoring : MonoBehaviour
    {
        private class Baker : Baker<WormSegmentAuthoring>
        {
            public override void Bake(WormSegmentAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);

                AddComponent(entity, new WormSegmentComponent()
                {
                    WormOwner = Entity.Null,
                    SegmentIndex = 0
                });

                AddComponent(entity, new WormSegmentCollisionComponent()
                {
                    Radius = 0.5f
                });
            }
        }
    }
}