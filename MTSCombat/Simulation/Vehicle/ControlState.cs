using System;
using System.Collections.Generic;

namespace MTSCombat.Simulation
{
    public abstract class ControlState
    {
        public abstract Tuple<Transform2, DynamicState> ProcessState(Tuple<Transform2, DynamicState> state);
        public abstract ControlState GetNextStateFromInput(StandardPlayerInput playerInput);
        public abstract void GetPossibleActions(List<ControlState> resultingControls);
    }
}
