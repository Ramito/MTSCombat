using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MTSCombat.Simulation
{
    public static class AsteroidsControlsFactory
    {
        public static readonly VehicleDriveControls DefaultDriveControls = new VehicleDriveControls();

        public static readonly VehicleControlsConfig StandardConfig = new VehicleControlsConfig(float.MaxValue, float.MaxValue, 0f, DefaultDriveControls);

        public static VehicleDrive MakeDrive(AsteroidsControlData data)
        {
            return (s, c, dt) => ProcessState(data, s, c, dt);
        }

        private static DynamicTransform2 ProcessState(AsteroidsControlData data, DynamicTransform2 state, VehicleDriveControls controls, float deltaTime)
        {
            float appliedRotationalThrust;
            if (controls.Axis1 != 0f)
            {
                appliedRotationalThrust = -data.RotationAcceleration * controls.Axis1;
            }
            else
            {
                appliedRotationalThrust = -state.AngularVelocity / deltaTime;
            }
            float originalRotationalVelocity = state.AngularVelocity;
            float newRotationalVelocity = originalRotationalVelocity + appliedRotationalThrust * deltaTime;
            if (Math.Abs(newRotationalVelocity) > data.MaxRotationSpeed)
            {
                newRotationalVelocity = Math.Sign(newRotationalVelocity) * data.MaxRotationSpeed;
                appliedRotationalThrust = (newRotationalVelocity - originalRotationalVelocity) / deltaTime;
            }

            float rotatedAmount = deltaTime * originalRotationalVelocity + 0.5f * (deltaTime * deltaTime) * appliedRotationalThrust;
            Orientation2 currentOrientation = state.Orientation;
            Orientation2 resultingOrientation = currentOrientation.RotatedBy(rotatedAmount);
            DynamicOrientation2 resultingDynamicOrientation = new DynamicOrientation2(resultingOrientation, newRotationalVelocity);

            //Use new orientation so that rotating and accelerating rsults in different outputs than just accelerating!
            Vector2 thrustDirection = resultingDynamicOrientation.Orientation.Facing;
            Vector2 appliedThrust = data.Acceleration * controls.Axis2 * thrustDirection;
            Vector2 originalVelocity = state.Velocity;
            Vector2 newVelocity = originalVelocity + deltaTime * appliedThrust;

            float intendedSpeedSq = newVelocity.LengthSquared();
            if (intendedSpeedSq > (data.MaxSpeed * data.MaxSpeed))
            {
                //NOTE: This is not the most accurate approach, but it is somewhat simpler
                newVelocity.Normalize();
                newVelocity = data.MaxSpeed * newVelocity;
                appliedThrust = (newVelocity - originalVelocity) / deltaTime;
            }

            Vector2 positionDelta = deltaTime * originalVelocity + 0.5f * (deltaTime * deltaTime) * appliedThrust;
            Vector2 newPosition = state.Position + positionDelta;
            DynamicPosition2 resultingDynamicPosition = new DynamicPosition2(newPosition, newVelocity);

            return new DynamicTransform2(resultingDynamicPosition, resultingDynamicOrientation);
        }
    }

    public class AsteroidsControlData
    {
        public readonly float Acceleration;
        public readonly float MaxSpeed;
        public readonly float RotationAcceleration;
        public readonly float MaxRotationSpeed;

        public AsteroidsControlData(float acceleration, float speed, float rotationAcceleration, float rotationSpeed)
        {
            Acceleration = acceleration;
            MaxSpeed = speed;
            RotationAcceleration = rotationAcceleration;
            MaxRotationSpeed = rotationSpeed;
        }
    }
}
