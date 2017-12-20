namespace MTSCombat.Simulation
{
    public class VehicleState
    {
        public DynamicTransform2 DynamicTransform { get; private set; }
        public StandardVehicleControls ControlState { get; private set; }
        public bool GunTriggerState { get; private set; }
        public GunState GunState { get; private set; }

        public void SetDriveState(DynamicTransform2 transform, StandardVehicleControls controlState)
        {
            DynamicTransform = transform;
            ControlState = controlState;
        }

        public void SetGunState(bool triggerState, GunState gunState)
        {
            GunTriggerState = triggerState;
            GunState = gunState;
        }
    }
}
