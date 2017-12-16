namespace MTSCombat.Simulation
{
    public struct DynamicOrientation2
    {
        public readonly Orientation2 Orientation;
        public readonly float AngularVelocity;

        public DynamicOrientation2(Orientation2 orientation) : this(orientation, 0f) { }

        public DynamicOrientation2(Orientation2 orientation, float angularVelocity)
        {
            Orientation = orientation;
            AngularVelocity = angularVelocity;
        }
    }
}
