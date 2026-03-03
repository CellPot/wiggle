using Components;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Authoring
{
    public class PlayerAuthoring : MonoBehaviour
    {
        public string playerName = "Player";

        private class Baker : Baker<PlayerAuthoring>
        {
            public override void Bake(PlayerAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);

                AddComponent(entity, new PlayerTagComponent());

                AddComponent(entity, new WormInfoComponent
                {
                    CurrentScore = 0,
                    Name = (FixedString64Bytes)authoring.playerName
                });
            }
        }
    }
}