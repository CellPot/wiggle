using Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PlayerInputSystem))]
    [UpdateBefore(typeof(WormMovementSystem))]
    public partial struct WormBoostSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var boost in SystemAPI.Query<RefRW<WormBoostComponent>>())
            {
                ref var b = ref boost.ValueRW;

                if (b.Exhausted && b.Stamina >= b.MaxStamina * b.ResumeFraction)
                    b.Exhausted = false;

                b.Active = b.Requested && !b.Exhausted && b.Stamina > 0f;

                if (b.Active)
                {
                    b.Stamina = math.max(0f, b.Stamina - b.DrainPerSecond * deltaTime);
                    if (b.Stamina <= 0f)
                        b.Exhausted = true;
                }
                else
                {
                    b.Stamina = math.min(b.MaxStamina, b.Stamina + b.RegenPerSecond * deltaTime);
                }
            }
        }
    }
}
