using System.Collections.Generic;
using System.Diagnostics;

namespace MTSCombat.Simulation
{
    public sealed class SimulationState
    {
        public readonly Dictionary<uint, VehicleState> Vehicles;
        public readonly Dictionary<uint, List<DynamicPosition2>> Projectiles;
        public readonly Dictionary<uint, int> RegisteredHits;

        public SimulationState(int vehicleCount)
        {
            Vehicles = new Dictionary<uint, VehicleState>(vehicleCount);
            Projectiles = new Dictionary<uint, List<DynamicPosition2>>(vehicleCount);
            RegisteredHits = new Dictionary<uint, int>();
        }

        public void SetProjectileCount(uint shooterID, int projectileCount)
        {
            Debug.Assert(!Projectiles.ContainsKey(shooterID));
            Projectiles[shooterID] = new List<DynamicPosition2>(projectileCount);
        }
    }
}
