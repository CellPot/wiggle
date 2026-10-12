using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Components
{
    public struct FoodGridEntry
    {
        public Entity Entity;
        public float2 Position;
        public float Radius;
        public int Value;
    }

    // Singleton: spatial hash of all food, rebuilt every frame by FoodGridSystem.
    public struct FoodGridComponent : IComponentData
    {
        public const float CellSize = 4f;

        public NativeParallelMultiHashMap<int, FoodGridEntry> Cells;
        public float MaxFoodRadius;

        public static int2 CellOf(float2 position)
        {
            return (int2)math.floor(position / CellSize);
        }

        public static int Key(int2 cell)
        {
            return (int)math.hash(cell);
        }

        // Inclusive cell range covering a circle. Hash collisions are possible, so callers still check distance.
        public static void CellRange(float2 center, float radius, out int2 min, out int2 max)
        {
            min = CellOf(center - radius);
            max = CellOf(center + radius);
        }
    }
}
