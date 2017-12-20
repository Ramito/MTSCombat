namespace MTSCombat.Simulation
{
    public sealed class VehiclePrototype
    {
        public readonly float VehicleSize;
        public readonly VehicleDrive VehicleDrive;
        public readonly SVCConfig ControlConfig;
        public readonly GunMount Guns;

        public VehiclePrototype(float size, VehicleDrive drive, SVCConfig controlConfig, GunMount guns)
        {
            VehicleSize = size;
            VehicleDrive = drive;
            ControlConfig = controlConfig;
            Guns = guns;
        }
    }
}
