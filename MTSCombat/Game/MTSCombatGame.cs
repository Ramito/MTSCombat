using Microsoft.Xna.Framework.Input;
using MTSCombat.Simulation;
using System.Collections.Generic;

namespace MTSCombat
{
    public sealed class MTSCombatGame
    {
        public SimulationState ActiveState { get; private set; }

        private Dictionary<uint, ControlState> mActiveInput;
        private SimulationProcessor mSimProcessor = new SimulationProcessor();

        public const uint kDefaultPlayerID = 0;

        public MTSCombatGame(int expectedVehicles)
        {
            ActiveState = new SimulationState(expectedVehicles, expectedVehicles);
            mActiveInput = new Dictionary<uint, ControlState>(expectedVehicles);
        }

        public void AddVehicle(VehicleState vehicle)
        {
            ActiveState.Vehicles.Add(vehicle);
        }

        public void Tick(float deltaTime)
        {
            StandardPlayerInput playerInput = StandardPlayerInput.ProcessKeyboard(Keyboard.GetState());
            var playerControl = ActiveState.GetCurrentControlStateForController(kDefaultPlayerID);
            mActiveInput[kDefaultPlayerID] = playerControl.GetNextStateFromInput(playerInput);
            ActiveState = mSimProcessor.ProcessState(ActiveState, mActiveInput, deltaTime);
        }
    }
}
