namespace MTSCombat.Simulation
{
    public class VehicleState
    {
        public readonly DynamicTransform2 DynamicTransform;
        public readonly VehicleDriveControls ControlState;
        public readonly GunState GunState;

        public VehicleState(DynamicTransform2 transform, VehicleDriveControls controlState) : this(transform, controlState, new GunState()) { }

        public VehicleState(DynamicTransform2 transform, VehicleDriveControls controlState, GunState gunState)
        {
            DynamicTransform = transform;
            ControlState = controlState;
            GunState = gunState;
        }
    }
}
