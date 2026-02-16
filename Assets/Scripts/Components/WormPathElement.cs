using Unity.Entities;
using Unity.Mathematics;

namespace Components
{
    [InternalBufferCapacity(100)]
    public struct WormPathElement : IBufferElementData
    {
        public float3 Position;
    }
}