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
        private readonly List<VehicleDriveControls>  mControlOptionCache;
        private readonly List<Option> mOptions;
        private readonly Dictionary<uint, VehicleControls> mControlInputMock;
        private readonly Random mRandom = new Random();

        private int mIterations;

        public MonteCarloTreeEvaluator(uint controlledID, uint targetID, float deltaTime, SimulationData simData)
        {
            mControlledID = controlledID;
            mTargetID = targetID;
            mDeltaTime = deltaTime;
            mSimData = simData;
            mEnemyProjectiles = new List<DynamicPosition2>();
            mControlOptionCache = new List<VehicleDriveControls>(3 * 3 * 3);
            mOptions = new List<Option>(mControlOptionCache.Capacity);
            mControlInputMock = new Dictionary<uint, VehicleControls>(2);
        }

        public void ResetAndSetup(SimulationState currentSimState)
        {
            mTargetRootState = currentSimState.GetVehicle(mTargetID);
            mEnemyProjectiles.Clear();
            mEnemyProjectiles.AddRange(currentSimState.GetProjectiles(mTargetID));

            VehiclePrototype controlledPrototype = mSimData.GetVehiclePrototype(mControlledID);
            VehicleState controlledState = currentSimState.GetVehicle(mControlledID);

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
            Option bestOption = GetBestOption();
            mOptions.Clear();
            return bestOption.ControlOption.DriveControls;
        }

        private Option GetBestOption()
        {
            float bestPayout = float.PositiveInfinity;
            Option bestOption = null;
            foreach (var option in mOptions)
            {
                float payout = option.AveragePayout;
                if (payout <= bestPayout)
                {
                    if (payout == bestPayout)
                    {
                        if (bestOption != null)
                        {
                            if (option.ControlOption.DriveControls.NormSq() >= bestOption.ControlOption.DriveControls.NormSq())
                            {
                                continue;
                            }
                        }
                    }
                    bestPayout = payout;
                    bestOption = option;
                }
            }
            Debug.Assert(bestOption != null);
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
            const float kDeltaContraction = 0.75f;
            const float kDeltaExpansion = 1.75f;
            float randomDeltaFactor = kDeltaContraction + ((kDeltaExpansion - kDeltaContraction) * (float)mRandom.NextDouble());
            float randomDelta = randomDeltaFactor * mDeltaTime;
            float payout = float.MaxValue;
            SimulationState iterationState = simState;
            int iterations = 11;
            for (int i = 0; i < iterations; ++i)
            {
                VehicleState controlledVehicle = iterationState.GetVehicle(mControlledID);
                VehiclePrototype controlledPrototype = mSimData.GetVehiclePrototype(mControlledID);

                VehicleState targetVehicle = iterationState.GetVehicle(mTargetID);
                VehiclePrototype targetPrototype = mSimData.GetVehiclePrototype(mTargetID);

                mControlInputMock[mControlledID] = new VehicleControls(GetRandomControl(controlledVehicle, controlledPrototype, mRandom, randomDelta));
                mControlInputMock[mTargetID] = new VehicleControls(GetRandomControl(targetVehicle, targetPrototype, mRandom, randomDelta));
                iterationState = SimulationProcessor.ProcessState(iterationState, mSimData, mControlInputMock, randomDelta);
                if (iterationState.GetRegisteredHits(mTargetID) != 0)
                {
                    float timeToHit = (i * randomDelta);
                    payout = int.MaxValue - (timeToHit * timeToHit);
                     break;
                }
                DynamicTransform2 controlledDynamicState = controlledVehicle.DynamicTransform;
                GunData controlledGun = controlledPrototype.Guns.MountedGun;
                DynamicTransform2 targetDynamicState = targetVehicle.DynamicTransform;
                GunData targetsGun = targetPrototype.Guns.MountedGun;
                float offensivePayout = MonteCarloVehicleAI.ShotDistance(controlledDynamicState, controlledGun, targetDynamicState.DynamicPosition);
                float defensivePayout = MonteCarloVehicleAI.ShotDistance(targetDynamicState, targetsGun, controlledDynamicState.DynamicPosition);
                payout = Math.Min(payout, 10f * offensivePayout - defensivePayout);
            }
            return payout;
        }

        private SimulationState GetPrimedState(Option option)
        {
            SimulationState simState = new SimulationState(2);
            simState.AddVehicle(mControlledID, option.ResultingState);
            simState.SetProjectileCount(mTargetID, mEnemyProjectiles.Count);
            simState.GetProjectiles(mTargetID).AddRange(mEnemyProjectiles);
            VehicleState targetRandomState = GetRandomNextState(mTargetRootState, mSimData.GetVehiclePrototype(mTargetID), mRandom, mDeltaTime);
            simState.AddVehicle(mTargetID, targetRandomState);
            return simState;
        }

        private static VehicleDriveControls GetRandomControl(VehicleState fromState, VehiclePrototype prototype, Random randomizer, float deltaTime)
        {
            int randomChoice = randomizer.Next(0, prototype.ControlConfig.PossibleDeltas.Length);
            return prototype.ControlConfig.GetControlFromDeltas(prototype.ControlConfig.PossibleDeltas[randomChoice], fromState.ControlState, deltaTime);
        }

        private static VehicleState GetRandomNextState(VehicleState fromState, VehiclePrototype prototype, Random randomizer, float deltaTime)
        {
            VehicleDriveControls randomControl = GetRandomControl(fromState, prototype, randomizer, deltaTime);
            DynamicTransform2 resultingDynamic = prototype.VehicleDrive(fromState.DynamicTransform, randomControl, deltaTime);
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
                AveragePayout = float.MaxValue;
            }
        }
    }
}
