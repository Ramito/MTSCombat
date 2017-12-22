using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MTSCombat.Simulation
{
    public sealed class MonteCarloVehicleAI
    {
        //public readonly Random mRandom = new Random();
        //private readonly List<VehicleDriveControls> mControlCache = new List<VehicleDriveControls>(3 * 3 * 3);
        private readonly MonteCarloTreeEvaluator mTreeEvaluator;

        public MonteCarloVehicleAI(uint playerID, uint targetID, float deltaTime, SimulationData simulationData)
        {
            mTreeEvaluator = new MonteCarloTreeEvaluator(playerID, targetID, deltaTime, simulationData);
        }

        public VehicleControls ComputeControl(SimulationState currentSimState)
        {
            return GetAIInput(currentSimState);
        }

        private VehicleControls GetAIInput(SimulationState simulationState)
        {
            mTreeEvaluator.ResetAndSetup(simulationState);
            mTreeEvaluator.Expand(300);
            VehicleControls chosenControl = mTreeEvaluator.GetBestControl();
            return chosenControl;
        }

        public static float ShotDistance(DynamicTransform2 shooter, GunData gun, DynamicPosition2 target)
        {
            Vector2 shotPosition = shooter.Position;
            Vector2 shotVelocity = gun.ShotSpeed * shooter.Orientation.Facing + shooter.Velocity;
            DynamicPosition2 initialProjectileState = new DynamicPosition2(shotPosition, shotVelocity);
            return ShotDistance(initialProjectileState, target);
        }

        public static float ShotDistance(DynamicPosition2 projectile, DynamicPosition2 target)
        {
            Vector2 shooterToTarget = target.Position - projectile.Position;
            float currentDistanceSq = shooterToTarget.LengthSquared();
            Vector2 relativeVelocities = target.Velocity - projectile.Velocity;
            float dot = Vector2.Dot(shooterToTarget, relativeVelocities);
            float relativeVelocityModuleSq = relativeVelocities.LengthSquared();
            float timeToClosest = -dot / relativeVelocityModuleSq;
            if (timeToClosest < 0f)
            {
                //TODO: Makes sense, but I wonder how smooth the resulting function is. I'd like to analyze this
                //Penalize by how far in the past the projectile would need to back track to hit
                return currentDistanceSq + (timeToClosest * timeToClosest) * relativeVelocityModuleSq;
            }
            //Squared distance to shot!
            float shotDistanceSq = currentDistanceSq + timeToClosest * ((2f * dot) + (timeToClosest * relativeVelocityModuleSq));
            Debug.Assert(!float.IsInfinity(shotDistanceSq));
            return shotDistanceSq;
        }
    }
}
