using Microsoft.Xna.Framework.Input;
using MTSCombat.Simulation;
using System;
using System.Collections.Generic;

namespace MTSCombat
{
    public sealed class MTSCombatGame
    {
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
                ControlState aiControlInput = GetAIInput(currentAIControl);
                mActiveInput[kDefaultAIID] = aiControlInput;
            }
            ActiveState = mSimProcessor.ProcessState(ActiveState, mActiveInput, deltaTime);
        }

        private ControlState GetAIInput(ControlState currentControl)
        {
            //TODO: Shooting controls need to be separated!
            List<ControlState> allControls = currentControl.GetPossibleActions();
            int choice = mRandom.Next(0, allControls.Count);
            return allControls[choice];
        }
    }
}
