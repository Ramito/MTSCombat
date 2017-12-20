namespace MTSCombat.Simulation
{
    public sealed class VehiclePrototype
    {
        public readonly float VehicleSize;
        public readonly VehicleDrive VehicleDrive;
        public readonly VehicleControlsConfig ControlConfig;
        public readonly GunMount Guns;

        public VehiclePrototype(float size, VehicleDrive drive, VehicleControlsConfig controlConfig, GunMount guns)
        {
            VehicleSize = size;
            VehicleDrive = drive;
            ControlConfig = controlConfig;
            Guns = guns;
        }
    }

    public delegate DynamicTransform2 VehicleDrive(DynamicTransform2 state, VehicleDriveControls controls, float deltaTime);
}
