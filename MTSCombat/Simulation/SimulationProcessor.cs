using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace MTSCombat.Simulation
{
    public sealed class SimulationProcessor
    {
        //Player input gets processed into a ControlState, and AI will provide a control state
        public SimulationState ProcessState(SimulationState state, Dictionary<uint, ControlState> controllerInputs, float deltaTime)
        {
            int currentVehicleCount = state.Vehicles.Count;
            int currentProjectileCount = state.Projectiles.Count;
            SimulationState nextSimState = new SimulationState(currentVehicleCount, currentProjectileCount + currentProjectileCount);
            foreach (var vehicle in state.Vehicles)
            {
                uint controllerID = vehicle.ControllerID;
                System.Diagnostics.Debug.Assert(controllerInputs.ContainsKey(controllerID));
                ControlState inputControlState = controllerInputs[controllerID];
                var processOutput = inputControlState.ProcessState(vehicle.DynamicTransform, deltaTime);
                VehicleState newVehicleState = new VehicleState();
                newVehicleState.SetControllerID(controllerID);
                newVehicleState.SetState(vehicle.Size, processOutput, inputControlState);
                nextSimState.Vehicles.Add(newVehicleState);
            }
            foreach (var projectile in state.Projectiles)
            {
                Vector2 nextPosition = projectile.Position + deltaTime * projectile.Velocity;
                DynamicPosition2 nextProjectileState = new DynamicPosition2(nextPosition, projectile.Velocity);
                nextSimState.Projectiles.Add(nextProjectileState);
            }
            return nextSimState;
        }
    }
}
