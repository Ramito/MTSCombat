using Microsoft.Xna.Framework;

namespace MTSCombat.Simulation
{
    public struct DynamicTransform2
    {
        public Vector2 Position { get { return DynamicPosition.Position; } }
        public Vector2 Velocity { get { return DynamicPosition.Velocity; } }
        public readonly DynamicPosition2 DynamicPosition;

        public Orientation2 Orientation { get { return DynamicOrientation.Orientation; } }
        public float AngularVelocity { get { return DynamicOrientation.AngularVelocity; } }
        public readonly DynamicOrientation2 DynamicOrientation;

        public DynamicTransform2(Vector2 position, Vector2 velocity, Orientation2 orientation, float angularVelocity)
            : this(new DynamicPosition2(position, velocity), new DynamicOrientation2(orientation, angularVelocity)) { }

        public DynamicTransform2(Vector2 position, Orientation2 orientation)
            : this(new DynamicPosition2(position), new DynamicOrientation2(orientation)) { }

        public DynamicTransform2(DynamicPosition2 dynamicPosition, DynamicOrientation2 dynamicOrientation)
        {
            DynamicPosition = dynamicPosition;
            DynamicOrientation = dynamicOrientation;
        }
    }
}
