using Microsoft.Xna.Framework;

namespace MTSCombat.Simulation
{
    public struct DynamicPosition2
    {
        public readonly Vector2 Position;
        public readonly Vector2 Velocity;

        public DynamicPosition2(Vector2 position) : this(position, Vector2.Zero) { }

        public DynamicPosition2(Vector2 position, Vector2 velocity)
        {
            Position = position;
            Velocity = velocity;
        }
    }
}
