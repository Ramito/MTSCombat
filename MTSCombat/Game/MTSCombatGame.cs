using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MTSCombat.Simulation;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MTSCombat
{
    public sealed class MTSCombatGame
    {
        public const float kWorkingPrecision = 0.0001f;

        public SimulationState ActiveState { get; private set; }

        private ushort RegisteredPlayers = 0;
        private Dictionary<uint, VehicleControls> mActiveInput;
        public SimulationData SimulationData { get; private set; }
        private SimulationProcessor mSimProcessor;

        public const uint kDefaultPlayerID = 0;
        public const uint kDefaultAIID = 1;

        public readonly Random mRandom = new Random();

        public MTSCombatGame(int expectedVehicles, int arenaWidth, int arenaHeight)
        {
            ActiveState = new SimulationState(expectedVehicles, expectedVehicles);
            mActiveInput = new Dictionary<uint, VehicleControls>(expectedVehicles);
            SimulationData = new SimulationData(expectedVehicles, arenaWidth, arenaHeight);
            mSimProcessor = new SimulationProcessor(expectedVehicles);
        }

        public uint AddVehicle(VehiclePrototype prototype, VehicleState vehicle)
        {
            PlayerData playerData = new PlayerData(prototype);
            uint assignedID = RegisteredPlayers;
            ++RegisteredPlayers;
            SimulationData.RegisterPlayer(assignedID, playerData);
            mSimProcessor.RegisterVehicle(assignedID, vehicle);
            return assignedID;
        }

        public void Tick(float deltaTime)
        {
            VehicleControls playerControl;
            if (mActiveInput.TryGetValue(kDefaultPlayerID, out playerControl))
            {
                StandardPlayerInput playerInput = StandardPlayerInput.ProcessKeyboard(Keyboard.GetState());
                VehiclePrototype prototype = SimulationData.GetPlayerData(kDefaultPlayerID).Prototype;
                VehicleDriveControls newDriveControl = prototype.ControlConfig.GetNextFromPlayerInput(playerControl.DriveControls, playerInput, deltaTime);
                mActiveInput[kDefaultPlayerID] = new VehicleControls(newDriveControl, playerInput.TriggerInput);
            }
            VehicleControls currentAIControl;
            if (mActiveInput.TryGetValue(kDefaultAIID, out currentAIControl))
            {
                VehicleControls aiControlInput = GetAIInput(kDefaultAIID, ActiveState, kDefaultPlayerID, deltaTime);
                mActiveInput[kDefaultAIID] = aiControlInput;
            }
            ActiveState = mSimProcessor.ProcessState(ActiveState, SimulationData, mActiveInput, deltaTime);
        }

        private VehicleControls GetAIInput(uint playerID, SimulationState simulationState, uint targetID, float deltaTime)
        {
            VehiclePrototype prototype = SimulationData.GetPlayerData(playerID).Prototype;
            VehicleState currentVehicleState = simulationState.Vehicles[playerID];
            GunData gunData = prototype.Guns.MountedGun;
            List<VehicleDriveControls> allControls = new List<VehicleDriveControls>(25);    //TODO: CACHE!
            prototype.ControlConfig.GetPossibleControlChanges(currentVehicleState.ControlState, deltaTime, allControls);
            VehicleState target = simulationState.Vehicles[targetID];
            GunData targetGunData = SimulationData.GetPlayerData(targetID).Prototype.Guns.MountedGun;
            float bestHeuristic = float.MaxValue;
            VehicleDriveControls chosenControl = new VehicleDriveControls();
            foreach (VehicleDriveControls control in allControls)
            {
                DynamicTransform2 possibleState = prototype.VehicleDrive(currentVehicleState.DynamicTransform, control, deltaTime);
                float possibleShotDistance = ShotDistance(possibleState, gunData, target.DynamicTransform.DynamicPosition);
                float conversePossibleShotDistance = ShotDistance(target.DynamicTransform, targetGunData, possibleState.DynamicPosition);
                float heuristic = possibleShotDistance - conversePossibleShotDistance;
                if (heuristic < bestHeuristic)
                {
                    bestHeuristic = heuristic;
                    chosenControl = control;
                }
            }
            return new VehicleControls(chosenControl, true);
        }

        private float ShotDistance(DynamicTransform2 shooter, GunData gun, DynamicPosition2 target)
        {
            Vector2 shotVelocity = gun.ShotSpeed * shooter.Orientation.Facing;

            Vector2 shooterToTarget = target.Position - shooter.Position;
            float currentDistanceSq = shooterToTarget.LengthSquared();
            Vector2 relativeVelocities = target.Velocity - shotVelocity;
            float dot = Vector2.Dot(shooterToTarget, relativeVelocities);
            if (dot >= 0f)
            {
                return currentDistanceSq;
            }
            float relativeVelocityModule = relativeVelocities.LengthSquared();
            if (relativeVelocityModule < kWorkingPrecision)
            {
                return currentDistanceSq;
            }
            float timeToImpact = -dot / relativeVelocityModule;
            float shotDistance = currentDistanceSq + timeToImpact * ((2f * dot) + (timeToImpact * currentDistanceSq));
            Debug.Assert(!float.IsInfinity(shotDistance));
            return shotDistance;
        }
    }
}
