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
        public Transform2 Transform { get; private set; }
        public DynamicState DynamicState { get; private set; }
        public ControlState ControlState { get; private set; }

        public void SetControllerID(uint controllerID)
        {
            ControllerID = controllerID;
        }

        public void SetState(Transform2 transform, DynamicState dynamicState, ControlState controlState)
        {
            Transform = transform;
            DynamicState = dynamicState;
            ControlState = controlState;
        }
    }
}
