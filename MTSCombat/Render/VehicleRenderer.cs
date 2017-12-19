﻿using Microsoft.Xna.Framework;
using MTSCombat.Render;
using MTSCombat.Simulation;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MTSCombat
{
    public class MTSCombatRenderer
    {
        private static readonly Color[] sColors =
            new Color[]
            {
                Color.Black,
                Color.White,
            };

        private PrimitiveRenderer mPrimitiveRenderer;
        private List<Vector2> mVertexHash;

        public MTSCombatRenderer(PrimitiveRenderer primitiveRenderer)
        {
            mPrimitiveRenderer = primitiveRenderer;
            mVertexHash = new List<Vector2>(12);
        }

        public void RenderSimState(SimulationState stateToRender)
        {
            RenderVehicles(stateToRender.Vehicles);
            RenderProjectiles(stateToRender.Projectiles);
            mPrimitiveRenderer.Render();
        }

        private void RenderVehicles(List<VehicleState> vehicles)
        {
            int colorIndex = 1;
            foreach (var vehicle in vehicles)
            {
                SetVehicleVerticesOnHash(vehicle);
                mPrimitiveRenderer.PushPolygon(mVertexHash, sColors[colorIndex]);
                colorIndex = (colorIndex + 1) % sColors.Length;
                mVertexHash.Clear();
            }
        }

        private void RenderProjectiles(List<DynamicPosition2> projectiles)
        {
            foreach (var projectile in projectiles)
            {
                SetProjectileVerticesOnHash(projectile);
                mPrimitiveRenderer.PushPolygon(mVertexHash, Color.Orange);
                mVertexHash.Clear();
            }
        }

        const double kDrawAngle = 2.25f;
        private readonly static double kCosDrawAngle = Math.Cos(kDrawAngle);
        private readonly static double kSinDrawAngle = Math.Sin(kDrawAngle);
        private void SetVehicleVerticesOnHash(VehicleState vehicle)
        {
            Debug.Assert(mVertexHash.Count == 0);
            Vector2 position = vehicle.DynamicTransform.Position;
            Vector2 sizedFacing = vehicle.Size * vehicle.DynamicTransform.Orientation.Facing;
            mVertexHash.Add(position + sizedFacing);
            mVertexHash.Add(position + PositiveRotateHelper(sizedFacing));
            mVertexHash.Add(position + NegativeRotateHelper(sizedFacing));
        }

        private void SetProjectileVerticesOnHash(DynamicPosition2 projectileState)
        {
            Debug.Assert(mVertexHash.Count == 0);
            const float projectileScale = 1f / 45f;
            const float forwardProportion = 0.1f;
            const float lateralScale = 0.1f;
            Vector2 position = projectileState.Position;
            Vector2 direction = projectileScale * projectileState.Velocity;
            Vector2 lateral = lateralScale * (new Vector2(-direction.Y, direction.X));
            mVertexHash.Add(position + forwardProportion * direction);
            mVertexHash.Add(position + lateral);
            mVertexHash.Add(position + (forwardProportion - 1f) * direction);
            mVertexHash.Add(position - lateral);
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
