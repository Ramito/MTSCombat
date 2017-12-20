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

        MonteCarloVehicleAI mAI = new MonteCarloVehicleAI();

        public MTSCombatGame(int expectedVehicles, int arenaWidth, int arenaHeight)
        {
            ActiveState = new SimulationState(expectedVehicles);
            mActiveInput = new Dictionary<uint, VehicleControls>(expectedVehicles);
            SimulationData = new SimulationData(expectedVehicles, arenaWidth, arenaHeight);
        }

        public uint AddVehicle(VehiclePrototype prototype, VehicleState vehicle)
        {
            PlayerData playerData = new PlayerData(prototype);
            uint assignedID = RegisteredPlayers;
            ++RegisteredPlayers;
            SimulationData.RegisterPlayer(assignedID, playerData);
            ActiveState.Vehicles.Add(assignedID, vehicle);
            ActiveState.SetProjectileCount(assignedID, 1);
            return assignedID;
        }

        public void Tick(float deltaTime)
        {
            VehicleControls playerControl;
            if (mActiveInput.TryGetValue(kDefaultPlayerID, out playerControl))
            {
                StandardPlayerInput playerInput = StandardPlayerInput.ProcessKeyboard(Keyboard.GetState());
                VehiclePrototype prototype = SimulationData.GetPlayerData(kDefaultPlayerID).Prototype;
                VehicleDriveControls newDriveControl = prototype.ControlConfig.GetNextFromPlayerInput(playerControl.DriveControls, playerInput, deltaTime);
                mActiveInput[kDefaultPlayerID] = new VehicleControls(newDriveControl, playerInput.TriggerInput);
            }
            else
            {
                VehiclePrototype prototype = SimulationData.GetPlayerData(kDefaultPlayerID).Prototype;
                mActiveInput[kDefaultPlayerID] = new VehicleControls(prototype.ControlConfig.DefaultControl);
            }
            VehicleControls currentAIControl;
            if (mActiveInput.TryGetValue(kDefaultAIID, out currentAIControl))
            {
                VehicleControls aiControlInput = mAI.ComputeControl(kDefaultAIID, ActiveState, SimulationData, deltaTime);
                mActiveInput[kDefaultAIID] = aiControlInput;
            }
            else
            {
                VehiclePrototype prototype = SimulationData.GetPlayerData(kDefaultAIID).Prototype;
                mActiveInput[kDefaultAIID] = new VehicleControls(prototype.ControlConfig.DefaultControl);
            }
            ActiveState = SimulationProcessor.ProcessState(ActiveState, SimulationData, mActiveInput, deltaTime);
        }
    }
}
