using Microsoft.Xna.Framework;
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

        public void RenderSimState(SimulationData simulationData, SimulationState stateToRender)
        {
            RenderVehicles(simulationData, stateToRender.Vehicles);
            foreach (var kvp in stateToRender.Projectiles)
            {
                RenderProjectiles(kvp.Value);
            }
            mPrimitiveRenderer.Render();
        }

        private void RenderVehicles(SimulationData simData, Dictionary<uint, VehicleState> vehicles)
        {
            int colorIndex = 1;
            foreach (var vehicleKVP in vehicles)
            {
                VehiclePrototype prototype = simData.GetPlayerData(vehicleKVP.Key).Prototype;
                SetVehicleVerticesOnHash(prototype, vehicleKVP.Value);
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
        private void SetVehicleVerticesOnHash(VehiclePrototype prototype, VehicleState vehicleState)
        {
            Debug.Assert(mVertexHash.Count == 0);
            Vector2 position = vehicleState.DynamicTransform.Position;
            Vector2 sizedFacing = prototype.VehicleSize * vehicleState.DynamicTransform.Orientation.Facing;
            mVertexHash.Add(position + sizedFacing);
            mVertexHash.Add(position + PositiveRotateHelper(sizedFacing));
            mVertexHash.Add(position + NegativeRotateHelper(sizedFacing));
        }

        public static Vector2 GetRelativeGunMountLocation(int index)
        {
            index = index % 3;
            Vector2 reference = (new Orientation2()).Facing;
            switch (index)
            {
                case 0:
                    return reference;
                case 1:
                    return PositiveRotateHelper(reference);
                case 2:
                default:
                    return NegativeRotateHelper(reference);
            }
        }

        private void SetProjectileVerticesOnHash(DynamicPosition2 projectileState)
        {
            Debug.Assert(mVertexHash.Count == 0);
            const float projectileScale = 1f / 30f;
            const float forwardProportion = 0.1f;
            const float lateralScale = 0.15f;
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
