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

        public float NormSq()
        {
            return (Axis1 * Axis1) + (Axis2 * Axis2) + (Axis3 * Axis3);
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
        public readonly AxisDeltas[] PossibleDeltas;

        public VehicleControlsConfig(float axis1RateOfChange, float axis2RateOfChange, float axis3RateOfChange, VehicleDriveControls defaultControl)
        {
            Axis1RateOfChange = axis1RateOfChange;
            Axis2RateOfChange = axis2RateOfChange;
            Axis3RateOfChange = axis3RateOfChange;
            DefaultControl = defaultControl;
            PossibleDeltas = GenerateDeltas(axis1RateOfChange, axis2RateOfChange, axis3RateOfChange);
        }

        private static AxisDeltas[] GenerateDeltas(float axis1RateOfChange, float axis2RateOfChange, float axis3RateOfChange)
        {
            int deltaCount = 0;
            for (short axis1Delta = -1; axis1Delta < 2; ++axis1Delta)
            {
                if ((axis1Delta != 0) && (axis1RateOfChange == 0f))
                {
                    continue;
                }
                for (short axis2Delta = -1; axis2Delta < 2; ++axis2Delta)
                {
                    if ((axis2Delta != 0) && (axis2RateOfChange == 0f))
                    {
                        continue;
                    }
                    for (short axis3Delta = -1; axis3Delta < 2; ++axis3Delta)
                    {
                        if ((axis3Delta != 0) && (axis3RateOfChange == 0f))
                        {
                            continue;
                        }
                        ++deltaCount;
                    }
                }
            }
            AxisDeltas[] deltas = new AxisDeltas[deltaCount];
            deltaCount = 0;
            for (short axis1Delta = -1; axis1Delta < 2; ++axis1Delta)
            {
                if ((axis1Delta != 0) && (axis1RateOfChange == 0f))
                {
                    continue;
                }
                for (short axis2Delta = -1; axis2Delta < 2; ++axis2Delta)
                {
                    if ((axis2Delta != 0) && (axis2RateOfChange == 0f))
                    {
                        continue;
                    }
                    for (short axis3Delta = -1; axis3Delta < 2; ++axis3Delta)
                    {
                        if ((axis3Delta != 0) && (axis3RateOfChange == 0f))
                        {
                            continue;
                        }
                        deltas[deltaCount] = new AxisDeltas(axis1Delta, axis2Delta, axis3Delta);
                        ++deltaCount;
                    }
                }
            }
            return deltas;
        }

        public void GetPossibleControlChanges(VehicleDriveControls currentState, float deltaTime, List<VehicleDriveControls> possibleControls)
        {
            foreach (var axisDeltas in PossibleDeltas)
            {
                VehicleDriveControls resulting = GetControlFromDeltas(axisDeltas, currentState, deltaTime);
                possibleControls.Add(resulting);
            }
        }

        public VehicleDriveControls GetControlFromDeltas(AxisDeltas axisDeltas, VehicleDriveControls currentState, float deltaTime)
        {
            float resultingAxis1Value = AxisValue(currentState.Axis1, axisDeltas.Delta1, Axis1RateOfChange, deltaTime);
            float resultingAxis2Value = AxisValue(currentState.Axis2, axisDeltas.Delta2, Axis2RateOfChange, deltaTime);
            float resultingAxis3Value = AxisValue(currentState.Axis3, axisDeltas.Delta3, Axis3RateOfChange, deltaTime);
            return new VehicleDriveControls(resultingAxis1Value, resultingAxis2Value, resultingAxis3Value);
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

        public struct AxisDeltas
        {
            public readonly short Delta1;
            public readonly short Delta2;
            public readonly short Delta3;

            public AxisDeltas(short delta1, short delta2, short delta3)
            {
                Delta1 = delta1;
                Delta2 = delta2;
                Delta3 = delta3;
            }
        }
    }
}
