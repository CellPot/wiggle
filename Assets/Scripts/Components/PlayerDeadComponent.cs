using Unity.Entities;

namespace Components
{
    // Present on the (disabled) player worm while the game-over screen is up.
    public struct PlayerDeadComponent : IComponentData
    {
        public float TimeSinceDeath;
        // Set by the game-over UI; PlayerRespawnSystem resets the worm once it is allowed to.
        public bool RespawnRequested;
    }
}
