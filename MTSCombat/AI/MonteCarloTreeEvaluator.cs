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

            VehiclePrototype controlledPrototype = mSimData.GetVehiclePrototype(mControlledID);
            VehicleState controlledState = currentSimState.GetVehicle(mControlledID);

            var projectiles = currentSimState.GetProjectiles(mTargetID);
            //Cache the projectile states for next frame, but onyl those with a chance of hiting
            foreach (var projectile in projectiles)
            {
                Vector2 relativePosition = controlledState.DynamicTransform.Position - projectile.Position;
                Vector2 relativeVelocity = controlledState.DynamicTransform.Velocity - projectile.Velocity;
                if (Vector2.Dot(relativePosition, relativeVelocity) < 0f)
                {
                    if (!SimulationProcessor.ProjectileHitsVehicle(controlledState.DynamicTransform, controlledPrototype, projectile, mDeltaTime))
                    {
                        DynamicPosition2 updatedProjectile = new DynamicPosition2(projectile.Position + mDeltaTime * projectile.Velocity, projectile.Velocity);
                        mEnemyProjectiles.Add(projectile);
                    }
                }
            }


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
            return new VehicleControls(bestOption.ControlOption.DriveControls, ((float)bestOption.Payout.ShotsLanded / bestOption.TimesRun > 0.25f));
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

            const float kDeltaContraction = 1f;
            const float kDeltaExpansion = 5f;
            SimulationState iterationState = simState;
            const int kIterations = 10;
            for (int i = 0; i < kIterations; ++i)
            {
                float randomDeltaFactor = kDeltaContraction + ((kDeltaExpansion - kDeltaContraction) * (float)mRandom.NextDouble());
                float randomDelta = randomDeltaFactor * mDeltaTime;
                VehicleState controlledVehicle = iterationState.GetVehicle(mControlledID);
                VehiclePrototype controlledPrototype = mSimData.GetVehiclePrototype(mControlledID);
                mControlInputMock[mControlledID] = new VehicleControls(GetRandomControl(controlledVehicle, controlledPrototype, mRandom, randomDelta));

                VehicleState targetVehicle = iterationState.GetVehicle(mTargetID);
                VehiclePrototype targetPrototype = mSimData.GetVehiclePrototype(mTargetID);
                mControlInputMock[mTargetID] = new VehicleControls(GetRandomControl(targetVehicle, targetPrototype, mRandom, randomDelta));

                iterationState = SimulationProcessor.ProcessState(iterationState, mSimData, mControlInputMock, randomDelta);

                EvaluateIterationPayout(iterationState, ref payout, controlledVehicle.DynamicTransform, controlledPrototype, targetVehicle.DynamicTransform, targetPrototype);
            }
            ComputeResidueRolloutHits(iterationState, ref payout);
            return payout;
        }

        private void EvaluateIterationPayout(
            SimulationState iterationState,
            ref OptionPayout payout,
            DynamicTransform2 controlledDynamicState,
            VehiclePrototype controlledPrototype,
            DynamicTransform2 targetDynamicState,
            VehiclePrototype targetPrototype)
        {
            payout.ShotsTaken += iterationState.GetRegisteredHits(mTargetID);
            payout.ShotsLanded += iterationState.GetRegisteredHits(mControlledID);
            GunData controlledGun = controlledPrototype.Guns.MountedGun;
            GunData targetsGun = targetPrototype.Guns.MountedGun;

            float bestShot = MonteCarloVehicleAI.ShotDistanceSq(controlledDynamicState, controlledGun, targetDynamicState.DynamicPosition);
            bestShot = Math.Min(payout.BestShotDistance, bestShot);
            payout.BestShotDistance = bestShot;

            float bestShotForTarget = MonteCarloVehicleAI.ShotDistanceSq(targetDynamicState, targetsGun, controlledDynamicState.DynamicPosition);
            bestShotForTarget = Math.Min(payout.BestShotDistanceForTarget, bestShotForTarget);
            //foreach (var projectile in iterationState.GetProjectiles(mTargetID))
            //{
            //    //Experimental: Penalize target shot distance so projectiles create more urgency to avoid
            //    float projectileShotDistanceSq = MonteCarloVehicleAI.ShotDistanceSq(projectile, controlledDynamicState.DynamicPosition);
            //    bestShotForTarget = Math.Min(projectileShotDistanceSq, bestShotForTarget);
            //}
            //payout.BestShotDistanceForTarget = bestShotForTarget;
        }

        private void ComputeResidueRolloutHits(SimulationState iterationState, ref OptionPayout payout)
        {
            var remainingOwnProjectiles = iterationState.GetProjectiles(mControlledID);
            if (remainingOwnProjectiles.Count > 0)
            {
                var targetState = iterationState.GetVehicle(mTargetID);
                foreach (var projectile in remainingOwnProjectiles)
                {
                    if (SimulationProcessor.ProjectileHitsVehicle(targetState.DynamicTransform, mSimData.GetVehiclePrototype(mTargetID), projectile, 1000f))
                    {
                        payout.ShotsLanded += 1;
                    }
                }
            }
            var remainingEnemyProjectiles = iterationState.GetProjectiles(mTargetID);
            if (remainingEnemyProjectiles.Count > 0)
            {
                var targetState = iterationState.GetVehicle(mControlledID);
                foreach (var projectile in remainingEnemyProjectiles)
                {
                    if (SimulationProcessor.ProjectileHitsVehicle(targetState.DynamicTransform, mSimData.GetVehiclePrototype(mControlledID), projectile, 1000f))
                    {
                        payout.ShotsTaken += 1;
                    }
                }
            }
        }

        private SimulationState GetPrimedState(Option option)
        {
            SimulationState simState = new SimulationState(2);
            //Add the state resulting from this option, and a mock shot to track hits
            simState.AddVehicle(mControlledID, option.ResultingState);
            simState.SetProjectileCount(mControlledID, 1);
            DynamicPosition2 mockProjectile = SimulationProcessor.CreateProjectileState(option.ResultingState.DynamicTransform, mSimData.GetVehiclePrototype(mControlledID).Guns, 0, mDeltaTime);
            simState.GetProjectiles(mControlledID).Add(mockProjectile);
            //Add the existing enemy projectiles, if they have any hope to hit
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
                ShotsTaken = 0;
                ShotsLanded = 0;
            }

            public void InitializeForAccumulation()
            {
                BestShotDistance = float.MinValue;
                BestShotDistanceForTarget = float.MaxValue;
                ShotsTaken = int.MaxValue;
                ShotsLanded = 0;
            }

            public void Accumulate(OptionPayout otherPayout)
            {
                ShotsTaken = Math.Min(otherPayout.ShotsTaken, ShotsTaken);
                ShotsLanded += otherPayout.ShotsLanded;
                BestShotDistance = Math.Max(BestShotDistance, otherPayout.BestShotDistance);
                BestShotDistanceForTarget = Math.Min(BestShotDistanceForTarget, otherPayout.BestShotDistanceForTarget);
            }

            public float CurrentValue(int timesRun, int totalIterations, bool useExplorationTerm)
            {
                float penalty = 0;
                if (ShotsTaken > 0)
                {
                    const float beingShotPenalty = 20000f;
                    penalty = ShotsTaken *  beingShotPenalty;
                }
                float explorationTerm = 0f;
                if (useExplorationTerm)
                {
                    explorationTerm = - (float)Math.Sqrt(Math.Log(totalIterations) / timesRun);
                }
                return (BestShotDistance / BestShotDistanceForTarget) + penalty + explorationTerm;
            }
        }
    }
}
