using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace MTSCombat.Simulation
{
    public class VehicleState
    {
        public uint ControllerID { get; private set; }
        //OPTION: We could blend this bundle with the control base class! (?)
        public DynamicTransform2 DynamicTransform { get; private set; }
        public ControlState ControlState { get; private set; }

        public void SetControllerID(uint controllerID)
        {
            ControllerID = controllerID;
        }

        public void SetState(DynamicTransform2 transform, ControlState controlState)
        {
            DynamicTransform = transform;
            ControlState = controlState;
        }
    }
}
