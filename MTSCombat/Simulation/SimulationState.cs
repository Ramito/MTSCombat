using System.Collections.Generic;

namespace MTSCombat.Simulation
{
    public sealed class SimulationState
    {
        public readonly Dictionary<uint, VehicleState> Vehicles;
        public readonly List<DynamicPosition2> Projectiles;

        public SimulationState(int vehicleCount, int projectileCount)
        {
            Vehicles = new Dictionary<uint, VehicleState>(vehicleCount);
            Projectiles = new List<DynamicPosition2>(projectileCount);
        }
    }
}
