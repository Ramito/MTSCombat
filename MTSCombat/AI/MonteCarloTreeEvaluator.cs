using Microsoft.Xna.Framework;
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
                Option option = GetBestOption(true);
                ExpandOption(option);
                ++mIterations;
            }
        }

        public VehicleControls GetBestControl()
        {
            Option bestOption = GetBestOption(false);
            mOptions.Clear();
            return new VehicleControls(bestOption.ControlOption.DriveControls, (bestOption.Payout.ShotsLanded > 0));
        }

        private Option GetBestOption(bool exploring)
        {
            float bestPayout = float.PositiveInfinity;
            Option bestOption = null;
            foreach (var option in mOptions)
            {
                float payout = option.Payout.CurrentValue(option.TimesRun, mIterations, exploring);
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
            OptionPayout payout = RolloutStateAndGetPayout(simState);
            option.TimesRun++;
            option.Payout.Accumulate(payout);
        }

        private OptionPayout RolloutStateAndGetPayout(SimulationState simState)
        {
            OptionPayout payout = new OptionPayout();
            payout.InitializeForExpand();

            const float kDeltaContraction = 0.9f;
            const float kDeltaExpansion = 2.2f;
            SimulationState iterationState = simState;
            const int kIterations = 14;
            for (int i = 0; i < kIterations; ++i)
            {
                float randomDeltaFactor = kDeltaContraction + ((kDeltaExpansion - kDeltaContraction) * (float)mRandom.NextDouble());
                float randomDelta = randomDeltaFactor * mDeltaTime;
                VehicleState controlledVehicle = iterationState.GetVehicle(mControlledID);
                VehiclePrototype controlledPrototype = mSimData.GetVehiclePrototype(mControlledID);

                VehicleState targetVehicle = iterationState.GetVehicle(mTargetID);
                VehiclePrototype targetPrototype = mSimData.GetVehiclePrototype(mTargetID);

                mControlInputMock[mControlledID] = new VehicleControls(GetRandomControl(controlledVehicle, controlledPrototype, mRandom, randomDelta));
                mControlInputMock[mTargetID] = new VehicleControls(GetRandomControl(targetVehicle, targetPrototype, mRandom, randomDelta));
                iterationState = SimulationProcessor.ProcessState(iterationState, mSimData, mControlInputMock, randomDelta);

                payout.ShotsTaken += iterationState.GetRegisteredHits(mTargetID);
                payout.ShotsLanded += iterationState.GetRegisteredHits(mControlledID);

                DynamicTransform2 controlledDynamicState = controlledVehicle.DynamicTransform;
                GunData controlledGun = controlledPrototype.Guns.MountedGun;
                DynamicTransform2 targetDynamicState = targetVehicle.DynamicTransform;
                GunData targetsGun = targetPrototype.Guns.MountedGun;

                float bestShot = MonteCarloVehicleAI.ShotDistance(controlledDynamicState, controlledGun, targetDynamicState.DynamicPosition);
                bestShot = Math.Min(payout.BestShotDistance, bestShot);
                payout.BestShotDistance = bestShot;

                float bestShotForTarget = MonteCarloVehicleAI.ShotDistance(targetDynamicState, targetsGun, controlledDynamicState.DynamicPosition);
                bestShotForTarget = Math.Min(payout.BestShotDistanceForTarget, bestShotForTarget);
                foreach (var projectile in iterationState.GetProjectiles(mTargetID))
                {
                    float projectileShotDistance = MonteCarloVehicleAI.ShotDistance(projectile, controlledVehicle.DynamicTransform.DynamicPosition);
                    bestShotForTarget = Math.Min(projectileShotDistance, bestShotForTarget);
                }
                payout.BestShotDistanceForTarget = bestShotForTarget;
            }
            return payout;
        }

        private SimulationState GetPrimedState(Option option)
        {
            SimulationState simState = new SimulationState(2);
            //Add the state resulting from this option, and a mock shot to track hits
            simState.AddVehicle(mControlledID, option.ResultingState);
            simState.SetProjectileCount(mControlledID, 1);
            DynamicPosition2 mockProjectile = SimulationProcessor.CreateProjectileState(option.ResultingState.DynamicTransform, mSimData.GetVehiclePrototype(mControlledID).Guns, 0);
            simState.GetProjectiles(mControlledID).Add(mockProjectile);
            //Add the existing enemy projectiles, if they have any hope to hit
            simState.SetProjectileCount(mTargetID, mEnemyProjectiles.Count);
            foreach (var projectile in mEnemyProjectiles)
            {
                Vector2 relativePosition = option.ResultingState.DynamicTransform.Position - projectile.Position;
                Vector2 relativeVelocity = option.ResultingState.DynamicTransform.Velocity - projectile.Velocity;
                if (Vector2.Dot(relativePosition,relativeVelocity) > 0f)
                {
                    simState.GetProjectiles(mTargetID).AddRange(mEnemyProjectiles);
                }
            }
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
            public OptionPayout Payout;

            public Option(VehicleControls controlOption, VehicleState resultingState)
            {
                ControlOption = controlOption;
                ResultingState = resultingState;
                TimesRun = 0;
                Payout.InitializeForAccumulation();
            }
        }

        private struct OptionPayout
        {
            public int ShotsTaken;
            public int ShotsLanded;
            public float BestShotDistance;
            public float BestShotDistanceForTarget;

            public void InitializeForExpand()
            {
                BestShotDistance = float.MaxValue;
                BestShotDistanceForTarget = float.MaxValue;
            }

            public void InitializeForAccumulation()
            {
                BestShotDistance = float.MinValue;
                BestShotDistanceForTarget = float.MinValue;
            }

            public void Accumulate(OptionPayout otherPayout)
            {
                ShotsTaken += otherPayout.ShotsTaken;
                ShotsLanded += otherPayout.ShotsLanded;
                BestShotDistance = Math.Max(BestShotDistance, otherPayout.BestShotDistance);
                BestShotDistanceForTarget = Math.Max(BestShotDistanceForTarget, otherPayout.BestShotDistanceForTarget);
            }

            public float CurrentValue(int timesRun, int totalIterations, bool useExplorationTerm)
            {
                float penalty = 0;
                if (ShotsTaken > 0)
                {
                    const float beingShotPenalty = 100000f;
                    penalty = beingShotPenalty * (float)ShotsTaken / (float)timesRun;
                }
                float explorationTerm = 0f;
                if (useExplorationTerm)
                {
                    explorationTerm = - 0.5f * (float)Math.Sqrt(Math.Log(totalIterations) / timesRun);
                }
                return BestShotDistance / BestShotDistanceForTarget + penalty + explorationTerm;
            }
        }
    }
}
