﻿using Microsoft.Xna.Framework;
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
            mFacingAngle = (angle % kTwoPi);
        }

        public Orientation2 RotatedBy(float angle)
        {
            return new Orientation2(mFacingAngle + angle);
        }

        public Vector2 LocalToGlobal(Vector2 local)
        {
            Vector2 facing = Facing;
            float x = facing.X * local.X - facing.Y * local.Y;
            float y = facing.Y * local.X + facing.X * local.Y;
            return new Vector2(x, y);
        }
    }
}
