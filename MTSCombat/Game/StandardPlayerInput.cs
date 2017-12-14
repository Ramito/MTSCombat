using Microsoft.Xna.Framework.Input;

namespace MTSCombat
{
    public struct StandardPlayerInput
    {
        public readonly float HorizontalInput;
        public readonly float VerticalInput;
        public readonly float RotationInput;
        public readonly bool TriggerInput;

        public StandardPlayerInput(float horizontal, float vertical, float rotation, bool trigger)
        {
            HorizontalInput = horizontal;
            VerticalInput = vertical;
            RotationInput = rotation;
            TriggerInput = trigger;
        }

        //TODO: Move this factory method outside the class to reduce coupling with monogame
        public static StandardPlayerInput ProcessKeyboard(KeyboardState state)
        {
            float horizontal = 0f;
            if (state.IsKeyDown(Keys.D))
            {
                horizontal += 1f;
            }
            if (state.IsKeyDown(Keys.A))
            {
                horizontal += -1f;
            }
            float vertical = 0f;
            if (state.IsKeyDown(Keys.W))
            {
                vertical += 1f;
            }
            if (state.IsKeyDown(Keys.S))
            {
                vertical += -1f;
            }
            float rotation = 0f;
            if (state.IsKeyDown(Keys.Q))
            {
                rotation += 1f;
            }
            if (state.IsKeyDown(Keys.E))
            {
                rotation += -1f;
            }
            bool trigger = state.IsKeyDown(Keys.Space);
            return new StandardPlayerInput(horizontal, vertical, rotation, trigger);
        }
    }

}
