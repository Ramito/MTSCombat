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

        public void Tick()
        {
            StandardPlayerInput playerInput = StandardPlayerInput.ProcessKeyboard(Keyboard.GetState());
            var playerControl = ActiveState.GetCurrentControlStateForController(kDefaultPlayerID);
            mActiveInput[kDefaultPlayerID] = playerControl.GetNextStateFromInput(playerInput);
            ActiveState = mSimProcessor.ProcessState(ActiveState, mActiveInput);
        }
    }
}
