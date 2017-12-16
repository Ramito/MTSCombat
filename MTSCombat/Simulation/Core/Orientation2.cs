using Microsoft.Xna.Framework;
using System;

namespace MTSCombat.Simulation
{
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
}
