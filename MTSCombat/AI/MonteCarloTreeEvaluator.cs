using MTSCombat.Simulation;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MTSCombat
{
    public sealed class MonteCarloTreeEvaluator
    {
        private readonly uint mControlledID;
        private readonly uint mTargetID;
        private readonly float mDeltaTime;
        private readonly SimulationData mSimData;
        private VehicleState mTargetRootState;
        private readonly List<DynamicPosition2> mEnemyProjectiles;
        private readonly List<Option> mOptions;
        private readonly List<VehicleDriveControls> mControlOptionCache;
        private readonly Dictionary<uint, VehicleControls> mControlInputMock;
        private readonly Random mRandom = new Random();

        private int mIterations;

        public MonteCarloTreeEvaluator(uint controlledID, uint targetID, float deltaTime, SimulationData simData)
        {
            mControlledID = controlledID;
            mTargetID = targetID;
            mDeltaTime = 2.5f * deltaTime;
            mSimData = simData;
            mEnemyProjectiles = new List<DynamicPosition2>();
            mControlOptionCache = new List<VehicleDriveControls>(3 * 3 * 3);
            mOptions = new List<Option>(mControlOptionCache.Capacity);
            mControlInputMock = new Dictionary<uint, VehicleControls>(2);
        }

        public void ResetAndSetup(SimulationState currentSimState)
        {
            mTargetRootState = currentSimState.Vehicles[mTargetID];
            mEnemyProjectiles.Clear();
            mEnemyProjectiles.AddRange(currentSimState.Projectiles[mTargetID]);

            VehiclePrototype controlledPrototype = mSimData.GetVehiclePrototype(mControlledID);
            VehicleState controlledState = currentSimState.Vehicles[mControlledID];

            mOptions.Clear();

            controlledPrototype.ControlConfig.GetPossibleControlChanges(controlledState.ControlState, mDeltaTime, mControlOptionCache);
            foreach (var driveControlOption in mControlOptionCache)
            {
                VehicleControls controlOption = new VehicleControls(driveControlOption);
                DynamicTransform2 resultingDynamicState = controlledPrototype.VehicleDrive(controlledState.DynamicTransform, driveControlOption, mDeltaTime);
                VehicleState resultingVehicleState = new VehicleState(resultingDynamicState, driveControlOption);   //We should not care for the gun state... I thnk...

                Option option = new Option(controlOption, resultingVehicleState);
                mOptions.Add(option);
            }
            mControlOptionCache.Clear();
            mIterations = 0;
        }

        public void Expand(int iterations)
        {
            foreach (var option in mOptions)
            {
                ExpandOption(option);
            }
            mIterations = 1;
            while(mIterations < iterations)
            {
                Option option = GetBestOption();
                ExpandOption(option);
                ++mIterations;
            }
        }

        public VehicleDriveControls GetBestControl()
        {
            return GetBestOption().ControlOption.DriveControls;
        }

        private Option GetBestOption()
        {
            float bestPayout = float.MaxValue;
            Option bestOption = null;
            float logTimesRun = (float)Math.Log(mIterations + 1);
            foreach (var option in mOptions)
            {
                float payout = option.AveragePayout;// - 500000 * (logTimesRun / option.TimesRun);
                if (payout < bestPayout)
                {
                    bestPayout = payout;
                    bestOption = option;
                }
            }
            if (bestOption == null)
            {
                int random = mRandom.Next(0, mOptions.Count);
                bestOption = mOptions[random];
            }
            return bestOption;
        }



        private void ExpandOption(Option option)
        {
            SimulationState simState = GetPrimedState(option);
            float statePayout = RolloutStateAndGetPayout(simState);
            float totalPayout = option.AveragePayout * option.TimesRun;
            float averageFactor = 1f / (option.TimesRun + 1);
            option.AveragePayout = ((totalPayout * averageFactor) + (statePayout * averageFactor));
            ++option.TimesRun;
        }

        private float RolloutStateAndGetPayout(SimulationState simState)
        {
            float payout = 0f;
            SimulationState iterationState = simState;
            const float kSecondsToSimulate = 2f;
            int kIterations = (int) (0.5f + (kSecondsToSimulate / mDeltaTime));
            for (int i = 0; i < kIterations; ++i)
            {
                mControlInputMock[mControlledID] = new VehicleControls(GetRandomControl(iterationState.Vehicles[mControlledID], mSimData.GetVehiclePrototype(mControlledID)));
                mControlInputMock[mTargetID] = new VehicleControls(GetRandomControl(iterationState.Vehicles[mTargetID], mSimData.GetVehiclePrototype(mTargetID)));
                iterationState = SimulationProcessor.ProcessState(iterationState, mSimData, mControlInputMock, mDeltaTime);
                if (iterationState.RegisteredHits.Count != 0)
                {
                    Debug.Assert(iterationState.RegisteredHits[mTargetID] != 0);
                    payout = float.MaxValue;
                    break;
                }
                DynamicTransform2 targetDynamicState = iterationState.Vehicles[mTargetID].DynamicTransform;
                DynamicTransform2 controlledDynamicState = iterationState.Vehicles[mControlledID].DynamicTransform;
                GunData targetsGun = mSimData.GetVehiclePrototype(mTargetID).Guns.MountedGun;
                GunData controlledGun = mSimData.GetVehiclePrototype(mControlledID).Guns.MountedGun;
                payout -= (MonteCarloVehicleAI.ShotDistance(targetDynamicState, targetsGun, controlledDynamicState.DynamicPosition) / kIterations);
                payout += (MonteCarloVehicleAI.ShotDistance(controlledDynamicState, controlledGun, targetDynamicState.DynamicPosition) / kIterations);
            }
            return payout;
        }

        private SimulationState GetPrimedState(Option option)
        {
            SimulationState simState = new SimulationState(2);
            simState.Vehicles[mControlledID] = option.ResultingState;
            simState.SetProjectileCount(mTargetID, mEnemyProjectiles.Count);
            simState.Projectiles[mTargetID].AddRange(mEnemyProjectiles);
            VehicleState targetRandomState = GetRandomNextState(mTargetRootState, mSimData.GetVehiclePrototype(mTargetID));
            simState.Vehicles[mTargetID] = targetRandomState;
            return simState;
        }

        private VehicleDriveControls GetRandomControl(VehicleState fromState, VehiclePrototype prototype)
        {
            Debug.Assert(mControlOptionCache.Count == 0);
            prototype.ControlConfig.GetPossibleControlChanges(fromState.ControlState, mDeltaTime, mControlOptionCache);
            int randomChoice = mRandom.Next(0, mControlOptionCache.Count);
            VehicleDriveControls chosenControl = mControlOptionCache[randomChoice];
            mControlOptionCache.Clear();
            return chosenControl;
        }

        private VehicleState GetRandomNextState(VehicleState fromState, VehiclePrototype prototype)
        {
            VehicleDriveControls randomControl = GetRandomControl(fromState, prototype);
            DynamicTransform2 resultingDynamic = prototype.VehicleDrive(fromState.DynamicTransform, randomControl, mDeltaTime);
            return new VehicleState(resultingDynamic, randomControl);
        }

        private class Option
        {
            public readonly VehicleControls ControlOption;
            public readonly VehicleState ResultingState;
            public int TimesRun;
            public float AveragePayout;

            public Option(VehicleControls controlOption, VehicleState resultingState)
            {
                ControlOption = controlOption;
                ResultingState = resultingState;
                TimesRun = 0;
                AveragePayout = 0f;
            }
        }
    }
}
