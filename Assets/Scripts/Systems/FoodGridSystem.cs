using Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(BotAISystem))]
    public partial struct FoodGridSystem : ISystem
    {
        private Entity _gridEntity;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _gridEntity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(_gridEntity, new FoodGridComponent
            {
                Cells = new NativeParallelMultiHashMap<int, FoodGridEntry>(1024, Allocator.Persistent)
            });
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (state.EntityManager.Exists(_gridEntity))
            {
                state.EntityManager.GetComponentData<FoodGridComponent>(_gridEntity).Cells.Dispose();
                state.EntityManager.DestroyEntity(_gridEntity);
            }
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var grid = SystemAPI.GetComponentRW<FoodGridComponent>(_gridEntity);
            var cells = grid.ValueRO.Cells;
            cells.Clear();

            float maxRadius = 0f;

            foreach (var (transform, food, entity) in SystemAPI
                         .Query<RefRO<LocalTransform>, RefRO<FoodComponent>>()
                         .WithEntityAccess())
            {
                float2 pos = transform.ValueRO.Position.xy;
                float radius = food.ValueRO.CollisionRadius;
                maxRadius = math.max(maxRadius, radius);

                cells.Add(FoodGridComponent.Key(FoodGridComponent.CellOf(pos)), new FoodGridEntry
                {
                    Entity = entity,
                    Position = pos,
                    Radius = radius,
                    Value = food.ValueRO.Value
                });
            }

            grid.ValueRW.MaxFoodRadius = maxRadius;
        }
    }
}
