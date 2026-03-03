using Components;
using Unity.Burst;
using Unity.Entities;

namespace Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(FoodConsumptionSystem))]
    public partial struct ScoreUpdateSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (worm, wormInfo) in SystemAPI
                         .Query<RefRO<WormComponent>, RefRW<WormInfoComponent>>())
            {
                wormInfo.ValueRW.CurrentScore = worm.ValueRO.Score;
            }
        }
    }
}