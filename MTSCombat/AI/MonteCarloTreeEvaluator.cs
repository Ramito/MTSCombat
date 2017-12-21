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
        private readonly VehicleState mTargetRootState;
        private readonly List<DynamicPosition2> mEnemyProjectiles;
        private readonly List<Option> mOptions;
        private readonly List<VehicleDriveControls> mControlOptionCache;
        private readonly Dictionary<uint, VehicleControls> mControlInputMock;
        private readonly Random mRandom = new Random();

        private int mIterations;

        public MonteCarloTreeEvaluator(uint controlledID, uint targetID, SimulationState currentSimState, SimulationData simData, float deltaTime)
        {
            mControlledID = controlledID;
            mTargetID = controlledID;
            mDeltaTime = deltaTime;
            mSimData = simData;
            mTargetRootState = currentSimState.Vehicles[targetID];
            mEnemyProjectiles = new List<DynamicPosition2>(currentSimState.Projectiles[targetID]);

            VehiclePrototype controlledPrototype = simData.GetVehiclePrototype(controlledID);
            VehicleState controlledState = currentSimState.Vehicles[controlledID];

            mControlOptionCache = new List<VehicleDriveControls>(3 * 3 * 3);
            controlledPrototype.ControlConfig.GetPossibleControlChanges(controlledState.ControlState, deltaTime, mControlOptionCache);
            mOptions = new List<Option>(mControlOptionCache.Count);
            foreach (var driveControlOption in mControlOptionCache)
            {
                VehicleControls controlOption = new VehicleControls(driveControlOption);
                DynamicTransform2 resultingDynamicState = controlledPrototype.VehicleDrive(controlledState.DynamicTransform, driveControlOption, deltaTime);
                VehicleState resultingVehicleState = new VehicleState(resultingDynamicState, driveControlOption);   //We should not care for the gun state... I thnk...

                Option option = new Option(controlOption, resultingVehicleState);
                mOptions.Add(option);
            }
            mControlOptionCache.Clear();
            mControlInputMock = new Dictionary<uint, VehicleControls>(2);
            mIterations = 0;
        }

        public void Expand(int iterations)
        {
            foreach (var option in mOptions)
            {
                ExpandOption(option);
            }
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
            float bestPayout = float.MinValue;
            Option bestOption = null;
            float logTimesRun = (float)Math.Log(mIterations + 1);
            foreach (var option in mOptions)
            {
                float payout = option.AveragePayout + (logTimesRun / option.TimesRun);
                if (payout > bestPayout)
                {
                    bestPayout = payout;
                    bestOption = option;
                }
            }
            return bestOption;
        }



        private void ExpandOption(Option option)
        {
            SimulationState simState = GetPrimedState(option);
            float statePayout = RolloutStateAndGetPayout(simState);
            ++option.TimesRun;
            float totalPayout = option.AveragePayout * option.TimesRun;
            float averageFactor = 1f / (option.TimesRun + 1);
            option.AveragePayout += ((totalPayout * averageFactor) + (statePayout * averageFactor));
        }

        private float RolloutStateAndGetPayout(SimulationState simState)
        {
            float payout = 0f;
            SimulationState iterationState = simState;
            const float kSecondsToSimulate = 2.5f;
            int kIterations = (int) (0.5f + (kSecondsToSimulate / mDeltaTime));
            for (int i = 0; i < kIterations; ++i)
            {
                mControlInputMock[mControlledID] = new VehicleControls(GetRandomControl(iterationState.Vehicles[mControlledID], mSimData.GetVehiclePrototype(mControlledID)));
                mControlInputMock[mTargetID] = new VehicleControls(GetRandomControl(iterationState.Vehicles[mTargetID], mSimData.GetVehiclePrototype(mTargetID)));
                iterationState = SimulationProcessor.ProcessState(iterationState, mSimData, mControlInputMock, mDeltaTime);
                if (iterationState.RegisteredHits.Count != 0)
                {
                    Debug.Assert(iterationState.RegisteredHits[mTargetID] != 0);
                    payout = float.MinValue;
                    break;
                }
                DynamicTransform2 targetDynamicState = iterationState.Vehicles[mTargetID].DynamicTransform;
                DynamicTransform2 controlledDynamicState = iterationState.Vehicles[mControlledID].DynamicTransform;
                GunData targetsGun = mSimData.GetVehiclePrototype(mTargetID).Guns.MountedGun;
                GunData controlledGun = mSimData.GetVehiclePrototype(mControlledID).Guns.MountedGun;
                payout += (MonteCarloVehicleAI.ShotDistance(targetDynamicState, targetsGun, controlledDynamicState.DynamicPosition) * mDeltaTime);
                payout -= (MonteCarloVehicleAI.ShotDistance(controlledDynamicState, controlledGun, targetDynamicState.DynamicPosition) * mDeltaTime);
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
