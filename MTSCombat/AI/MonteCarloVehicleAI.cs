using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MTSCombat.Simulation
{
    public sealed class MonteCarloVehicleAI
    {
        public readonly Random mRandom = new Random();
        private readonly List<VehicleDriveControls> mControlCache = new List<VehicleDriveControls>(3 * 3 * 3);

        public VehicleControls ComputeControl(uint controlledVehicleID, SimulationState currentSimState, SimulationData simData, float deltaTime)
        {
            uint targetID = FindTargetID(controlledVehicleID, currentSimState);
            return GetAIInput(controlledVehicleID, currentSimState, simData, targetID, deltaTime);
        }

        private uint FindTargetID(uint controlledID, SimulationState currentSimState)
        {
            foreach (var kvp in currentSimState.Vehicles)
            {
                if (kvp.Key != controlledID)
                {
                    return kvp.Key;
                }
            }
            return uint.MaxValue;
        }

        private VehicleControls GetAIInput(uint playerID, SimulationState simulationState, SimulationData simulationData, uint targetID, float deltaTime)
        {
            MonteCarloTreeEvaluator treeEvaluator = new MonteCarloTreeEvaluator(playerID, targetID, simulationState, simulationData, deltaTime);
            treeEvaluator.Expand(20);
            VehicleDriveControls chosenControl = treeEvaluator.GetBestControl();

            //VehiclePrototype prototype = simulationData.GetVehiclePrototype(playerID);
            //VehicleState currentVehicleState = simulationState.Vehicles[playerID];
            //GunData gunData = prototype.Guns.MountedGun;
            //prototype.ControlConfig.GetPossibleControlChanges(currentVehicleState.ControlState, deltaTime, mControlCache);
            //VehicleState target = simulationState.Vehicles[targetID];
            //GunData targetGunData = simulationData.GetVehiclePrototype(targetID).Guns.MountedGun;
            //float bestHeuristic = float.MaxValue;
            //VehicleDriveControls chosenControl = new VehicleDriveControls();
            //DynamicTransform2 chosenDynamicState = new DynamicTransform2();
            //foreach (VehicleDriveControls control in mControlCache)
            //{
            //    DynamicTransform2 possibleState = prototype.VehicleDrive(currentVehicleState.DynamicTransform, control, deltaTime);
            //    float possibleShotDistance = ShotDistance(possibleState, gunData, target.DynamicTransform.DynamicPosition);
            //    float conversePossibleShotDistance = ShotDistance(target.DynamicTransform, targetGunData, possibleState.DynamicPosition);
            //    float heuristic = possibleShotDistance - 2f * conversePossibleShotDistance;
            //    if (heuristic < bestHeuristic)
            //    {
            //        bestHeuristic = heuristic;
            //        chosenControl = control;
            //        chosenDynamicState = possibleState;
            //    }
            //}
            //mControlCache.Clear();
            //TODO: Parallelize?
            //VehiclePrototype targetPrototype = simulationData.GetVehiclePrototype(targetID);
            //bool shouldShoot = ShouldShoot(simulationData, chosenDynamicState, prototype.Guns, target, targetPrototype, deltaTime);
            return new VehicleControls(chosenControl, false);
        }

        public static float ShotDistance(DynamicTransform2 shooter, GunData gun, DynamicPosition2 target)
        {
            Vector2 shotVelocity = gun.ShotSpeed * shooter.Orientation.Facing + shooter.Velocity;

            Vector2 shooterToTarget = target.Position - shooter.Position;
            float currentDistanceSq = shooterToTarget.LengthSquared();
            Vector2 relativeVelocities = target.Velocity - shotVelocity;
            float dot = Vector2.Dot(shooterToTarget, relativeVelocities);
            if (dot >= 0f)
            {
                return currentDistanceSq;
            }
            float relativeVelocityModule = relativeVelocities.LengthSquared();
            if (relativeVelocityModule < MTSCombatGame.kWorkingPrecision)
            {
                return currentDistanceSq;
            }
            float timeToImpact = -dot / relativeVelocityModule;
            float shotDistance = currentDistanceSq + timeToImpact * ((2f * dot) + (timeToImpact * currentDistanceSq));
            Debug.Assert(!float.IsInfinity(shotDistance));
            return shotDistance;
        }

        private bool ShouldShoot(SimulationData simulationData, DynamicTransform2 shooter, GunMount gun, VehicleState targetVehicle, VehiclePrototype targetPrototype, float deltaTime)
        {
            const uint kShooterID = 0;
            const uint kTargetID = 1;
            SimulationState initialTestState = new SimulationState(1);
            Dictionary<uint, VehicleControls> mockControls = new Dictionary<uint, VehicleControls>(1);
            initialTestState.Vehicles[kTargetID] = targetVehicle;
            DynamicPosition2 projectileState = SimulationProcessor.CreateProjectileState(shooter, gun, 0); //Default barrel
            initialTestState.SetProjectileCount(kShooterID, 1);
            SimulationProcessor.SpawnProjectile(kShooterID, initialTestState, projectileState);
            int trials = 20;
            while (--trials >= 0)
            {
                SimulationState iterationState = initialTestState;
                const int kMaxIterations = 10 * 30;  //Ten seconds at 30 fps
                for (int i = 0; i < kMaxIterations; ++i)
                {
                    targetPrototype.ControlConfig.GetPossibleControlChanges(targetVehicle.ControlState, deltaTime, mControlCache);
                    int random = mRandom.Next(0, mControlCache.Count);
                    mockControls[kTargetID] = new VehicleControls(mControlCache[random]);
                    mControlCache.Clear();
                    iterationState = SimulationProcessor.ProcessState(iterationState, simulationData, mockControls, deltaTime);
                    if (iterationState.RegisteredHits.ContainsKey(kShooterID))
                    {
                        return true;
                    }
                    else
                    {
                        if (iterationState.Projectiles[kShooterID].Count == 0)
                        {
                            //Projectile flew off or otherwise expired
                            break;
                        }
                    }
                }
            }
            return false;
        }
    }
}
