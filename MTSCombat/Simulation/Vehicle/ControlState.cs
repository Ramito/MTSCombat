using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MTSCombat.Simulation
{
    public struct VehicleDriveControls
    {
        public readonly float Axis1;
        public readonly float Axis2;
        public readonly float Axis3;

        public VehicleDriveControls(float axis1, float axis2, float axis3)
        {
            Axis1 = axis1;
            Axis2 = axis2;
            Axis3 = axis3;
        }

        public VehicleDriveControls(float axis1, float axis2) : this(axis1, axis2, 0f) { }

        public VehicleDriveControls(float axis1) : this(axis1, 0f, 0f) { }
    }

    public struct VehicleControls
    {
        public readonly VehicleDriveControls DriveControls;
        public readonly bool GunTriggerDown;

        public VehicleControls(VehicleDriveControls driveControls) : this(driveControls, false) { }

        public VehicleControls(VehicleDriveControls driveControls, bool gunTriggerDown)
        {
            DriveControls = driveControls;
            GunTriggerDown = gunTriggerDown;
        }
    }

    public struct VehicleControlsConfig
    {
        public readonly float Axis1RateOfChange;
        public readonly float Axis2RateOfChange;
        public readonly float Axis3RateOfChange;

        public readonly VehicleDriveControls DefaultControl;

        public VehicleControlsConfig(float axis1RateOfChange, float axis2RateOfChange, float axis3RateOfChange, VehicleDriveControls defaultControl)
        {
            Axis1RateOfChange = axis1RateOfChange;
            Axis2RateOfChange = axis2RateOfChange;
            Axis3RateOfChange = axis3RateOfChange;
            DefaultControl = defaultControl;
        }

        public void GetPossibleControlChanges(VehicleDriveControls currentState, float deltaTime, List<VehicleDriveControls> possibleControls)
        {
            for (int axis1Delta = -1; axis1Delta < 2; ++axis1Delta)
            {
                if ((axis1Delta != 0) && (Axis1RateOfChange == 0f))
                {
                    continue;
                }
                float resultingAxis1Value = AxisValue(currentState.Axis1, axis1Delta, Axis1RateOfChange, deltaTime);
                for (int axis2Delta = -1; axis2Delta < 2; ++axis2Delta)
                {

                    if ((axis2Delta != 0) && (Axis2RateOfChange == 0f))
                    {
                        continue;
                    }
                    float resultingAxis2Value = AxisValue(currentState.Axis2, axis2Delta, Axis2RateOfChange, deltaTime);
                    for (int axis3Delta = -1; axis3Delta < 2; ++axis3Delta)
                    {
                        if ((axis3Delta != 0) && (Axis3RateOfChange == 0f))
                        {
                            continue;
                        }
                        float resultingAxis3Value = AxisValue(currentState.Axis3, axis3Delta, Axis3RateOfChange, deltaTime);
                        VehicleDriveControls resulting = new VehicleDriveControls(resultingAxis1Value, resultingAxis2Value, resultingAxis3Value);
                        possibleControls.Add(resulting);
                    }
                }
            }
        }

        private float AxisValue(float currentValue, float targetValue, float rateOfChange, float deltaTime)
        {
            float targetChange = targetValue - currentValue;
            float maxChange = rateOfChange * deltaTime;
            Debug.Assert(maxChange >= 0f);
            if (Math.Abs(targetChange) <= maxChange)
            {
                return targetValue;
            }
            else
            {
                return currentValue + Math.Sign(targetChange) * maxChange;
            }
        }

        public VehicleDriveControls GetNextFromPlayerInput(VehicleDriveControls current, StandardPlayerInput playerInput, float deltaTime)
        {
            float axis1 = AxisValue(current.Axis1, playerInput.HorizontalInput, Axis1RateOfChange, deltaTime);
            float axis2 = AxisValue(current.Axis2, playerInput.VerticalInput, Axis2RateOfChange, deltaTime);
            float axis3 = AxisValue(current.Axis3, playerInput.RotationInput, Axis3RateOfChange, deltaTime);
            return new VehicleDriveControls(axis1, axis2, axis3);
        }
    }
}
