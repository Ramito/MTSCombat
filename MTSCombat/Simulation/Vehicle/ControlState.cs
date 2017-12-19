using System;
using System.Collections.Generic;

namespace MTSCombat.Simulation
{
    public abstract class ControlState
    {
        public abstract DynamicTransform2 ProcessState(DynamicTransform2 state, float deltaTime);
        public abstract ControlState GetNextStateFromInput(StandardPlayerInput playerInput);
        public abstract bool GunTriggerDown();
        public abstract List<ControlState> GetPossibleActions();
    }
}
