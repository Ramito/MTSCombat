namespace MTSCombat.Simulation
{
    public class VehicleState
    {
        public DynamicTransform2 DynamicTransform { get; private set; }
        public VehicleDriveControls ControlState { get; private set; }
        public GunState GunState { get; private set; }

        public void SetDriveState(DynamicTransform2 transform, VehicleDriveControls controlState)
        {
            DynamicTransform = transform;
            ControlState = controlState;
        }

        public void SetGunState(GunState gunState)
        {
            GunState = gunState;
        }
    }
}
