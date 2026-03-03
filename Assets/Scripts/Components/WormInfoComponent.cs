using Unity.Collections;
using Unity.Entities;

namespace Components
{
    public struct WormInfoComponent : IComponentData
    {
        public int CurrentScore;
        public FixedString64Bytes Name;
    }
}