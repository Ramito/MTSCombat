using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace MTSCombat.Simulation
{
    public class VehicleState
    {
        public uint ControllerID { get; private set; }
        public float Size { get; private set; } //Represents a radius
        public DynamicTransform2 DynamicTransform { get; private set; }
        public ControlState ControlState { get; private set; }

        public void SetControllerID(uint controllerID)
        {
            ControllerID = controllerID;
        }

        public void SetState(float size, DynamicTransform2 transform, ControlState controlState)
        {
            Size = size;
            DynamicTransform = transform;
            ControlState = controlState;
        }
    }
}
