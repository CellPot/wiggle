using Components;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Authoring
{
    public class PlayerAuthoring : MonoBehaviour
    {
        public string playerName = "Player";

        [Header("Boost")] public float boostSpeedMultiplier = 1.8f;
        public float maxStamina = 100f;
        public float staminaDrainPerSecond = 35f;
        public float staminaRegenPerSecond = 20f;
        [Range(0f, 1f)] public float resumeFraction = 0.25f;

        private class Baker : Baker<PlayerAuthoring>
        {
            public override void Bake(PlayerAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);

                AddComponent(entity, new PlayerTagComponent());

                AddComponent(entity, new WormBoostComponent
                {
                    SpeedMultiplier = authoring.boostSpeedMultiplier,
                    MaxStamina = authoring.maxStamina,
                    DrainPerSecond = authoring.staminaDrainPerSecond,
                    RegenPerSecond = authoring.staminaRegenPerSecond,
                    ResumeFraction = authoring.resumeFraction,
                    Stamina = authoring.maxStamina
                });

                AddComponent(entity, new WormInfoComponent
                {
                    CurrentScore = 0,
                    Name = (FixedString64Bytes)authoring.playerName
                });
            }
        }
    }
}