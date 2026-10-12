using Unity.Entities;

namespace Components
{
    // Present on the (disabled) player worm while the game-over screen is up.
    public struct PlayerDeadComponent : IComponentData
    {
        public float TimeSinceDeath;
    }
}
