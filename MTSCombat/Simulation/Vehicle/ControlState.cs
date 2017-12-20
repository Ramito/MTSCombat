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
                float resultingAxis1Value;
                if (!ValidPermute(axis1Delta, currentState.Axis1, Axis1RateOfChange, deltaTime, out resultingAxis1Value))
                {
                    continue;
                }
                for (int axis2Delta = -1; axis2Delta < 2; ++axis2Delta)
                {
                    float resultingAxis2Value;
                    if (!ValidPermute(axis2Delta, currentState.Axis2, Axis2RateOfChange, deltaTime, out resultingAxis2Value))
                    {
                        continue;
                    }
                    for (int axis3Delta = -1; axis3Delta < 2; ++axis3Delta)
                    {
                        float resultingAxis3Value;
                        if (!ValidPermute(axis3Delta, currentState.Axis3, Axis3RateOfChange, deltaTime, out resultingAxis3Value))
                        {
                            continue;
                        }
                        VehicleDriveControls resulting = new VehicleDriveControls(resultingAxis1Value, resultingAxis2Value, resultingAxis3Value);
                        possibleControls.Add(resulting);
                    }
                }
            }
        }

        public VehicleDriveControls GetNextFromPlayerInput(VehicleDriveControls current, StandardPlayerInput playerInput, float deltaTime)
        {
            float axis1 = ClampAxis(current.Axis1 + deltaTime * Axis1RateOfChange * playerInput.HorizontalInput);
            float axis2 = ClampAxis(current.Axis2 + deltaTime * Axis2RateOfChange * playerInput.VerticalInput);
            float axis3 = ClampAxis(current.Axis3 + deltaTime * Axis3RateOfChange * playerInput.RotationInput);
            return new VehicleDriveControls(axis1, axis2, axis3);
        }

        private bool ValidPermute(int axisDelta, float currentAxisValue, float axisRoC, float deltaTime, out float resultingValue)
        {
            if (axisRoC == 0f)
            {
                resultingValue = currentAxisValue;
                return axisDelta == 0;
            }
            Debug.Assert(Math.Abs(axisDelta) <= 1);
            resultingValue = currentAxisValue + axisDelta * deltaTime * axisRoC;
            resultingValue = ClampAxis(resultingValue);
            return resultingValue != currentAxisValue;
        }

        private float ClampAxis(float axisValue)
        {
            return MathHelper.Clamp(axisValue, -1f, 1f);
        }
    }
}
