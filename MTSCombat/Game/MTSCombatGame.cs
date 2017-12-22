using Microsoft.Xna.Framework.Input;
using MTSCombat.Simulation;
using System.Collections.Generic;

namespace MTSCombat
{
    public sealed class MTSCombatGame
    {
        public const float kWorkingPrecision = 0.0001f;

        public SimulationState ActiveState { get; private set; }

        private ushort RegisteredPlayers = 0;
        private Dictionary<uint, VehicleControls> mActiveInput;
        public SimulationData SimulationData { get; private set; }

        public const uint kDefaultPlayerID = 0;
        public const uint kDefaultAIID = 1;

        MonteCarloVehicleAI mAI;

        public MTSCombatGame(int expectedVehicles, int arenaWidth, int arenaHeight)
        {
            ActiveState = new SimulationState(expectedVehicles);
            mActiveInput = new Dictionary<uint, VehicleControls>(expectedVehicles);
            SimulationData = new SimulationData(expectedVehicles, arenaWidth, arenaHeight);
        }

        public uint AddVehicle(VehiclePrototype prototype, VehicleState vehicle)
        {
            uint assignedID = RegisteredPlayers;
            ++RegisteredPlayers;
            SimulationData.RegisterPlayer(assignedID, prototype);
            ActiveState.AddVehicle(assignedID, vehicle);
            ActiveState.SetProjectileCount(assignedID, 1);
            return assignedID;
        }

        public void Tick(float deltaTime)
        {
            if (mAI == null)
            {
                mAI = new MonteCarloVehicleAI(kDefaultAIID, kDefaultPlayerID, deltaTime, SimulationData);   //TODO: inconsistently assuming delta time fixed here and otherwise elsewhere
            }
            VehicleControls playerControl;
            if (mActiveInput.TryGetValue(kDefaultPlayerID, out playerControl))
            {
                StandardPlayerInput playerInput = StandardPlayerInput.ProcessKeyboard(Keyboard.GetState());
                VehiclePrototype prototype = SimulationData.GetVehiclePrototype(kDefaultPlayerID);
                VehicleDriveControls newDriveControl = prototype.ControlConfig.GetNextFromPlayerInput(playerControl.DriveControls, playerInput, deltaTime);
                mActiveInput[kDefaultPlayerID] = new VehicleControls(newDriveControl, playerInput.TriggerInput);
            }
            else
            {
                VehiclePrototype prototype = SimulationData.GetVehiclePrototype(kDefaultPlayerID);
                mActiveInput[kDefaultPlayerID] = new VehicleControls(prototype.ControlConfig.DefaultControl);
            }
            VehicleControls aiControlInput = mAI.ComputeControl(ActiveState);
            mActiveInput[kDefaultAIID] = aiControlInput;

            ActiveState = SimulationProcessor.ProcessState(ActiveState, SimulationData, mActiveInput, deltaTime);
        }
    }
}
