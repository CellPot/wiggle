using Unity.Entities;
using Unity.Mathematics;

namespace Components
{
    [InternalBufferCapacity(256)]
    public struct WormPathElement : IBufferElementData
    {
        // World position of the worm head at a sampled time
        public float3 Position;
        // Cumulative distance along the path from the oldest recorded point to this point
        public float CumulativeDistance;
    }
}