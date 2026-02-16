using Unity.Entities;

namespace Components
{
    public struct WormSegmentComponent : IComponentData
    {
        public Entity WormOwner;
        public int SegmentIndex;
    }
}