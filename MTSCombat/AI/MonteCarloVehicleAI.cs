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
            mTreeEvaluator.Expand(600);
            VehicleDriveControls chosenControl = mTreeEvaluator.GetBestControl();
            return new VehicleControls(chosenControl, false);
        }

        public static float ShotDistance(DynamicTransform2 shooter, GunData gun, DynamicPosition2 target)
        {
            Vector2 shotVelocity = gun.ShotSpeed * shooter.Orientation.Facing + shooter.Velocity;

            Vector2 shooterToTarget = target.Position - shooter.Position;
            float currentDistanceSq = shooterToTarget.LengthSquared();
            Vector2 relativeVelocities = target.Velocity - shotVelocity;
            float dot = Vector2.Dot(shooterToTarget, relativeVelocities);
            float relativeVelocityModuleSq = relativeVelocities.LengthSquared();
            float timeToClosest = -dot / relativeVelocityModuleSq;
            if (timeToClosest < 0f)
            {
            }
            if (relativeVelocityModule < MTSCombatGame.kWorkingPrecision)
            {
                return currentDistanceSq;
            }
        }

        private bool ShouldShoot(SimulationData simulationData, DynamicTransform2 shooter, GunMount gun, VehicleState targetVehicle, VehiclePrototype targetPrototype, float deltaTime)
        {
        //    const uint kShooterID = 0;
        //    const uint kTargetID = 1;
        //    SimulationState initialTestState = new SimulationState(1);
        //    Dictionary<uint, VehicleControls> mockControls = new Dictionary<uint, VehicleControls>(1);
        //    initialTestState.Vehicles[kTargetID] = targetVehicle;
        //    DynamicPosition2 projectileState = SimulationProcessor.CreateProjectileState(shooter, gun, 0); //Default barrel
        //    initialTestState.SetProjectileCount(kShooterID, 1);
        //    SimulationProcessor.SpawnProjectile(kShooterID, initialTestState, projectileState);
        //    int trials = 20;
        //    while (--trials >= 0)
        //    {
        //        SimulationState iterationState = initialTestState;
        //        const int kMaxIterations = 10 * 30;  //Ten seconds at 30 fps
        //        for (int i = 0; i < kMaxIterations; ++i)
        //        {
        //            targetPrototype.ControlConfig.GetPossibleControlChanges(targetVehicle.ControlState, deltaTime, mControlCache);
        //            int random = mRandom.Next(0, mControlCache.Count);
        //            mockControls[kTargetID] = new VehicleControls(mControlCache[random]);
        //            mControlCache.Clear();
        //            iterationState = SimulationProcessor.ProcessState(iterationState, simulationData, mockControls, deltaTime);
        //            if (iterationState.RegisteredHits.ContainsKey(kShooterID))
        //            {
        //                return true;
        //            }
        //            else
        //            {
        //                if (iterationState.Projectiles[kShooterID].Count == 0)
        //                {
        //                    //Projectile flew off or otherwise expired
        //                    break;
        //                }
        //            }
        //        }
        //    }
            return false;
        }
    }
}
