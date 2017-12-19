using Microsoft.Xna.Framework.Input;
using MTSCombat.Simulation;
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
            StandardPlayerInput playerInput = StandardPlayerInput.ProcessKeyboard(Keyboard.GetState());
            var playerControl = ActiveState.GetCurrentControlStateForController(kDefaultPlayerID);
            if (playerControl != null)
            {
                mActiveInput[kDefaultPlayerID] = playerControl.GetNextStateFromInput(playerInput);
            }
            ActiveState = mSimProcessor.ProcessState(ActiveState, mActiveInput, deltaTime);
        }
    }
}
