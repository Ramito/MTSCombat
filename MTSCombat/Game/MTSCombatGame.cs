using Microsoft.Xna.Framework.Input;
using MTSCombat.Simulation;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MTSCombat
{
    public sealed class MTSCombatGame
    {
        public const float kWorkingPrecision = 0.0001f;

        public SimulationState ActiveState { get; private set; }

        private ushort RegisteredPlayers = 0;
        private Dictionary<uint, VehicleControls> mActiveInput;
        public SimulationData SimulationData { get; private set; }

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
            if (mAIIterator.Count == 0)
            {
                CreateAITask(0, 1, deltaTime, 550);
                CreateAITask(1, 0, deltaTime, 550);
            }

            foreach (uint id in mAIIterator)
            {
                mActiveInput[id] = mControlResults[id].Take();
            }
            ActiveState = SimulationProcessor.ProcessState(ActiveState, SimulationData, mActiveInput, deltaTime);
            foreach (uint id in mAIIterator)
            {
                mControlRequests[id].Add(ActiveState);  //TODO: Should copy!
            }
        }

        private List<uint> mAIIterator = new List<uint>(2);
        private Dictionary<uint, BlockingCollection<VehicleControls>> mControlResults = new Dictionary<uint, BlockingCollection<VehicleControls>>(2);
        private Dictionary<uint, BlockingCollection<SimulationState>> mControlRequests = new Dictionary<uint, BlockingCollection<SimulationState>>(2);

        private void CreateAITask(uint aiID, uint targetID, float deltaTime, int iterations)
        {
            mAIIterator.Add(aiID);
            var resultPlacement = new BlockingCollection<VehicleControls>(1);
            mControlResults[aiID] = resultPlacement;
            var requestPlacement = new BlockingCollection<SimulationState>(1);
            mControlRequests[aiID] = requestPlacement;
            MonteCarloVehicleAI ai = new MonteCarloVehicleAI(aiID, targetID, deltaTime, SimulationData, iterations);   //TODO: inconsistently assuming delta time fixed here and otherwise elsewhere
            Action aiAction = () =>
            {
                while (true)
                {
                    SimulationState state = requestPlacement.Take();
                    VehicleControls controls = ai.ComputeControl(state);
                    resultPlacement.Add(controls);
                }
            };
            Task aiTask = new Task(aiAction);
            requestPlacement.Add(ActiveState);
            aiTask.Start();
        }
    }
}
