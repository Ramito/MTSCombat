using Microsoft.Xna.Framework;
using MTSCombat.Render;
using MTSCombat.Simulation;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MTSCombat
{
    public class VehicleRenderer
    {
        private static readonly Color[] sColors =
            new Color[]
            {
                Color.Black,
                Color.White,
            };
        private List<Vector2> mVertexHash = new List<Vector2>();

        public void RenderVehicles(List<VehicleState> vehicles, PrimitiveRenderer renderer)
        {
            int colorIndex = 1;
            foreach (var vehicle in vehicles)
            {
                WriteVerticesOnHash(vehicle);
                renderer.PushPolygon(mVertexHash, sColors[colorIndex]);
                colorIndex = (colorIndex + 1) % sColors.Length;
                mVertexHash.Clear();
            }
        }

        public void RenderProjectiles(List<DynamicPosition2> projectiles, PrimitiveRenderer renderer)
        {
            foreach (var projectile in projectiles)
            {
                mVertexHash.Add(projectile.Position + Vector2.UnitX);
                mVertexHash.Add(projectile.Position + Vector2.UnitY);
                mVertexHash.Add(projectile.Position - Vector2.UnitX);
                mVertexHash.Add(projectile.Position - Vector2.UnitY);
                renderer.PushPolygon(mVertexHash, Color.Yellow);
                mVertexHash.Clear();
            }
        }

        const double kDrawAngle = 2.25f;
        private readonly static double kCosDrawAngle = Math.Cos(kDrawAngle);
        private readonly static double kSinDrawAngle = Math.Sin(kDrawAngle);
        private void WriteVerticesOnHash(VehicleState vehicle)
        {
            Debug.Assert(mVertexHash.Count == 0);
            Vector2 position = vehicle.DynamicTransform.Position;
            Vector2 sizedFacing = vehicle.Size * vehicle.DynamicTransform.Orientation.Facing;
            mVertexHash.Add(position + sizedFacing);
            mVertexHash.Add(position + PositiveRotateHelper(sizedFacing));
            mVertexHash.Add(position + NegativeRotateHelper(sizedFacing));
        }

        private static Vector2 PositiveRotateHelper(Vector2 facing)
        {
            double x = kCosDrawAngle * facing.X - kSinDrawAngle * facing.Y;
            double y = kSinDrawAngle * facing.X + kCosDrawAngle * facing.Y;
            return new Vector2((float)x, (float)y);
        }

        private static Vector2 NegativeRotateHelper(Vector2 facing)
        {
            double x = kCosDrawAngle * facing.X + kSinDrawAngle * facing.Y;
            double y = - kSinDrawAngle * facing.X + kCosDrawAngle * facing.Y;
            return new Vector2((float)x, (float)y);
        }
    }
}
