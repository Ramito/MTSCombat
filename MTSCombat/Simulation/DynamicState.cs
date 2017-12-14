using Microsoft.Xna.Framework;

namespace MTSCombat.Simulation
{
    public struct DynamicState
    {
        public readonly Vector2 Velocity;
        public readonly float AngularVelocity;

        public DynamicState(Vector2 velocity, float angularVelocity)
        {
            Velocity = velocity;
            AngularVelocity = angularVelocity;
        }
    }
}
