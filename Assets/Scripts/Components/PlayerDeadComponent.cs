using Unity.Entities;

namespace Components
{
    // Singleton that exists while the game-over screen is up (the player entity itself is destroyed).
    public struct PlayerDeadComponent : IComponentData
    {
        public int FinalScore;
        public float TimeSinceDeath;
        // Set by the game-over UI; PlayerSpawnSystem spawns a new player once it is allowed to.
        public bool RespawnRequested;
    }
}
