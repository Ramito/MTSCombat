using Microsoft.Xna.Framework;
using System;

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

    public struct Orientation2
    {
        private const float kTwoPi = (float)(2.0 * Math.PI);

        private readonly float mFacingAngle;
        public Vector2 Facing { get { return new Vector2((float)Math.Cos(mFacingAngle), (float)Math.Sin(mFacingAngle)); } }

        public Orientation2(float angle)
        {
            mFacingAngle = ((angle % kTwoPi) + kTwoPi) % kTwoPi;
        }

        public Orientation2 RotatedBy(float angle)
        {
            return new Orientation2(mFacingAngle + angle);
        }
    }

    public struct DynamicOrientation2
    {
        public readonly Orientation2 Orientation;
        public readonly float AngularVelocity;

        public DynamicOrientation2(Orientation2 orientation) : this(orientation, 0f) { }

        public DynamicOrientation2(Orientation2 orientation, float angularVelocity)
        {
            Orientation = orientation;
            AngularVelocity = angularVelocity;
        }
    }
}
