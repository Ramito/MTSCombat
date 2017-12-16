﻿using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace MTSCombat.Simulation
{
    public sealed class SimulationState
    {
        public readonly List<VehicleState> Vehicles;
        public readonly List<Tuple<DynamicTransform2, Vector2>> Projectiles;

        public SimulationState(int vehicleCount, int projectileCount)
        {
            Vehicles = new List<VehicleState>(vehicleCount);
            Projectiles = new List<Tuple<DynamicTransform2, Vector2>>(projectileCount);
        }

        public ControlState GetCurrentControlStateForController(uint controllerID)
        {
            foreach (var vehicle in Vehicles)
            {
                if (vehicle.ControllerID == controllerID)
                {
                    return vehicle.ControlState;
                }
            }
            return null;
        }
    }
}
