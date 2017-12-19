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
        private Dictionary<uint, ControlState> mActiveInput;
        private SimulationProcessor mSimProcessor;

        public const uint kDefaultPlayerID = 0;
        public const uint kDefaultAIID = 1;

        public readonly Random mRandom = new Random();

        public MTSCombatGame(int expectedVehicles, int arenaWidth, int arenaHeight)
        {
            ActiveState = new SimulationState(expectedVehicles, expectedVehicles);
            mActiveInput = new Dictionary<uint, ControlState>(expectedVehicles);
            mSimProcessor = new SimulationProcessor(expectedVehicles, arenaWidth, arenaHeight);
        }

        public void AddVehicle(VehicleState vehicle, GunMount gunMount)
        {
            PlayerData playerData = new PlayerData(gunMount);
            mSimProcessor.RegisterVehicle(RegisteredPlayers, playerData, vehicle);
            ++RegisteredPlayers;
        }

        public void Tick(float deltaTime)
        {
            var playerControl = ActiveState.GetCurrentControlStateForController(kDefaultPlayerID);
            if (playerControl != null)
            {
                StandardPlayerInput playerInput = StandardPlayerInput.ProcessKeyboard(Keyboard.GetState());
                mActiveInput[kDefaultPlayerID] = playerControl.GetNextStateFromInput(playerInput);
            }
            ControlState currentAIControl = ActiveState.GetCurrentControlStateForController(kDefaultAIID);
            if (currentAIControl != null)
            {
                ControlState aiControlInput = GetAIInput(kDefaultAIID, ActiveState, deltaTime);
                mActiveInput[kDefaultAIID] = aiControlInput;
            }
            ActiveState = mSimProcessor.ProcessState(ActiveState, mActiveInput, deltaTime);
        }

        private ControlState GetAIInput(uint playerID, SimulationState simulationState, float deltaTime)
        {
            //TODO: Shooting controls need to be separated!
            VehicleState currentVehicleState = simulationState.GetVehicleFor(playerID);
            GunData gunData = mSimProcessor.GetGunDataFor(playerID);
            List<ControlState> allControls = currentVehicleState.ControlState.GetPossibleActions();
            VehicleState target = simulationState.GetTargetVehicleFor(playerID);
            float bestShotDistance = float.MaxValue;
            ControlState chosenControl = null;
            foreach (ControlState control in allControls)
            {
                DynamicTransform2 possibleState = control.ProcessState(currentVehicleState.DynamicTransform, deltaTime);
                float possibleShotDistance = ShotDistance(possibleState, gunData, target.DynamicTransform.DynamicPosition);
                if (possibleShotDistance < bestShotDistance)
                {
                    bestShotDistance = possibleShotDistance;
                    chosenControl = control;
                }
            }
            return chosenControl;
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
