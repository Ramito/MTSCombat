using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTSCombat.Simulation
{
    public sealed class SimulationProcessor
    {
        //Player input gets processed into a ControlState, and AI will provide a control state
        public SimulationState ProcessState(SimulationState state, Dictionary<uint, ControlState> controllerInputs)
        {
            int currentVehicleCount = state.Vehicles.Count;
            int currentProjectileCount = state.Projectiles.Count;
            SimulationState nextSimState = new SimulationState(currentVehicleCount, currentProjectileCount + currentProjectileCount);
            foreach (var vehicle in state.Vehicles)
            {
                uint controllerID = vehicle.ControllerID;
                System.Diagnostics.Debug.Assert(controllerInputs.ContainsKey(controllerID));
                ControlState inputControlState = controllerInputs[controllerID];
                var stateTuple = new Tuple<Transform2, DynamicState>(vehicle.Transform, vehicle.DynamicState);
                var processOutput = inputControlState.ProcessState(stateTuple);
                VehicleState newVehicleState = new VehicleState();
                newVehicleState.SetControllerID(controllerID);
                newVehicleState.SetState(processOutput.Item1, processOutput.Item2, inputControlState);
                nextSimState.Vehicles.Add(newVehicleState);
            }
            //TODO PROJECTILES!
            return nextSimState;
        }
    }
}
