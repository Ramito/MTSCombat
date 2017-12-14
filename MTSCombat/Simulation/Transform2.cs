using Microsoft.Xna.Framework;
using System;

namespace MTSCombat.Simulation
{
    public struct Transform2
    {
        public readonly static Vector2 Forward = Vector2.UnitX;
        private const float kTwoPi = (float) (2.0 * Math.PI);

        public readonly Vector2 Position;
        public Vector2 Facing { get { return new Vector2((float)Math.Cos(mFacingAngle), (float)Math.Sin(mFacingAngle)); } }

        private readonly float mFacingAngle;

        public Transform2(Vector2 position, float angle)
        {
            Position = position;
            mFacingAngle = ((angle % kTwoPi) + kTwoPi) % kTwoPi;
        }
    }
}
