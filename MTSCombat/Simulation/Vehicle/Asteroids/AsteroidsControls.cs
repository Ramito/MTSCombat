using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Diagnostics;

namespace MTSCombat.Simulation
{


    public sealed class AsteroidsControls : ControlState
    {
        public readonly AsteroidsControlData Data;

        public readonly float RelativeThrust;
        public readonly float RelativeRotation;
        private readonly bool mTrigger;

        private static Dictionary<AsteroidsControlData, List<ControlState>> sPossibleActions = new Dictionary<AsteroidsControlData, List<ControlState>>();

        public AsteroidsControls(AsteroidsControlData data) : this(0f, 0f, false, data)
        {
            if (!sPossibleActions.ContainsKey(Data))
            {
                sPossibleActions[data] = null;  //Prevent stack overflow... dirty... I know...
                sPossibleActions[data] = PopulatePossibleActions();
            }
        }

        public AsteroidsControls(float thrust, float rotation, bool trigger, AsteroidsControlData data)
        {
            Data = data;
            RelativeThrust = thrust;
            RelativeRotation = rotation;
            mTrigger = trigger;
        }

        public override ControlState GetNextStateFromInput(StandardPlayerInput playerInput)
        {
            return new AsteroidsControls(playerInput.VerticalInput, -playerInput.HorizontalInput, playerInput.TriggerInput, Data);
        }

        public override bool GunTriggerDown()
        {
            return mTrigger;
        }

        private List<ControlState> PopulatePossibleActions()
        {
            const int kCombinations = 18;
            var possibleActions = new List<ControlState>(kCombinations);
            for (int i = 0; i < 2; ++i)
            {
                bool trigger = i == 0;
                for (int thrust = -1; thrust <= 1; ++thrust)
                {
                    float possibleThrust = (float)thrust;
                    for (int rotation = -1; rotation <= 1; ++rotation)
                    {
                        float possibleRotation = (float)rotation;
                        possibleActions.Add(new AsteroidsControls(possibleThrust, possibleRotation, trigger, Data));
                    }
                }
            }
            Debug.Assert(kCombinations == possibleActions.Count);
            return possibleActions;
        }

        public override List<ControlState> GetPossibleActions()
        {
            const int kCombinations = 18;
            Debug.Assert(kCombinations == sPossibleActions[Data].Count);
            return sPossibleActions[Data];
        }

        public override DynamicTransform2 ProcessState(DynamicTransform2 state, float deltaTime)
        {
            Vector2 thrustDirection = state.Orientation.Facing;
            Vector2 appliedThrust = Data.Acceleration * RelativeThrust * thrustDirection;
            Vector2 originalVelocity = state.Velocity;
            Vector2 newVelocity = originalVelocity + deltaTime * appliedThrust;

            float intendedSpeedSq = newVelocity.LengthSquared();
            if (intendedSpeedSq > (Data.MaxSpeed * Data.MaxSpeed))
            {
                //NOTE: This is not the most accurate approach, but it is somewhat simpler
                newVelocity.Normalize();
                newVelocity = Data.MaxSpeed * newVelocity;
                appliedThrust = (newVelocity - originalVelocity) / deltaTime;
            }

            Vector2 positionDelta = deltaTime * originalVelocity + 0.5f * (deltaTime * deltaTime) * appliedThrust;
            Vector2 newPosition = state.Position + positionDelta;
            DynamicPosition2 resultingDynamicPosition = new DynamicPosition2(newPosition, newVelocity);

            float angularVelocity = Data.RotationSpeed * RelativeRotation;
            float rotatedAmount = Data.RotationSpeed * RelativeRotation * deltaTime;
            Orientation2 currentOrientation = state.Orientation;
            Orientation2 resultingOrientation = currentOrientation.RotatedBy(rotatedAmount);
            DynamicOrientation2 resultingDynamicOrientation = new DynamicOrientation2(resultingOrientation, angularVelocity);

            return new DynamicTransform2(resultingDynamicPosition, resultingDynamicOrientation);
        }
    }

    public class AsteroidsControlData
    {
        public readonly float Acceleration;
        public readonly float MaxSpeed;
        public readonly float RotationSpeed;

        public AsteroidsControlData(float acceleration, float speed, float rotationSpeed)
        {
            Acceleration = acceleration;
            MaxSpeed = speed;
            RotationSpeed = rotationSpeed;
        }
    }
}
