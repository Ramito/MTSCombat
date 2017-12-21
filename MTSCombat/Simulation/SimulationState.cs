using System.Collections.Generic;
using System.Diagnostics;

namespace MTSCombat.Simulation
{
    public sealed class SimulationState
    {
        public readonly uint[] IndexToID;
        public readonly VehicleState[] Vehicles;
        public readonly List<DynamicPosition2>[] Projectiles;
        public readonly int[] RegisteredHits;

        public SimulationState(int playerCount)
        {
            IndexToID = new uint[playerCount];
            for (int i = 0; i < playerCount; ++i)
            {
                IndexToID[i] = uint.MaxValue;
            }
            Vehicles = new VehicleState[playerCount];
            Projectiles = new List<DynamicPosition2>[playerCount];
            RegisteredHits = new int[playerCount];
        }

        public int GetIndexFor(uint id)
        {
            int availableIndex = -1;
            for (int i = IndexToID.Length - 1; i >= 0; --i)
            {
                uint registeredID = IndexToID[i];
                if (registeredID == id)
                {
                    return i;
                }
                else if (registeredID == uint.MaxValue)
                {
                    availableIndex = i;
                }
            }
            IndexToID[availableIndex] = id;
            return availableIndex;
        }

        public void AddVehicle(uint id, VehicleState state)
        {
            int availableIndex = GetIndexFor(id);
            Debug.Assert(Vehicles[availableIndex] == null);
            Vehicles[availableIndex] = state;
        }

        public VehicleState GetVehicle(uint id)
        {
            return Vehicles[GetIndexFor(id)];
        }

        public List<DynamicPosition2> GetProjectiles(uint id)
        {
            return Projectiles[GetIndexFor(id)];
        }

        public void SetProjectileCount(uint shooterID, int projectileCount)
        {
            int availableIndex = GetIndexFor(shooterID);
            Debug.Assert(Projectiles[availableIndex] == null);
            Projectiles[availableIndex] = new List<DynamicPosition2>(projectileCount);
        }

        public int GetRegisteredHits(uint id)
        {
            int index = GetIndexFor(id);
            return RegisteredHits[index];
        }
    }
}
