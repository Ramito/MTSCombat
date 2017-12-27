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
                Option option = GetBestOption(true, true);
                ExpandOption(option);
                ++mIterations;
            }
        }

        public VehicleControls GetBestControl()
        {
            Option bestOption = GetBestOption(false, false);
            mOptions.Clear();
            return new VehicleControls(bestOption.ControlOption.DriveControls, ((float)bestOption.Payout.ShotsLanded / bestOption.TimesRun > 0.05f));
        }

        List<Option> mImpactLessOptions = new List<Option>(); //TODO TODO
        private Option GetBestOption(bool exploring, bool stillExpanding)
        {
            foreach (var option in mOptions)
            {
                if (option.Payout.ShotsTaken == 0)
                {
                    mImpactLessOptions.Add(option);
                }
            }
            float explorationTerm = (stillExpanding)? (float)Math.Sqrt(2.0 * Math.Log(mIterations)) : 0f;
            if (mImpactLessOptions.Count > 0)
            {
                float bestPositionValue = float.PositiveInfinity;
                Option bestHitlessOption = null;
                foreach (var impactlessOption in mImpactLessOptions)
                {
                    float accumulated = impactlessOption.Payout.AccumulatedPositioningValue;
                    float average = accumulated / (float)impactlessOption.TimesRun;
                    average -= (explorationTerm / impactlessOption.TimesRun);
                    if (average <= bestPositionValue)
                    {
                        bestPositionValue = average;
                        bestHitlessOption = impactlessOption;
                    }
                }
                mImpactLessOptions.Clear();
                return bestHitlessOption;
            }
            else
            {
                int leastHits = int.MaxValue;
                float bestThreat = float.PositiveInfinity;
                Option bestOption = null;
                foreach (var option in mOptions)
                {
                    float threat = option.Payout.ProjectileThreat;
                    threat += (explorationTerm / option.TimesRun);
                    if (option.Payout.ShotsTaken < leastHits)
                    {
                        leastHits = option.Payout.ShotsTaken;
                        bestThreat = threat;
                        bestOption = option;
                    }
                    else if ((option.Payout.ShotsTaken == leastHits) && (threat <= bestThreat))
                    {
                        bestThreat = threat;
                        bestOption = option;
                    }
                }
                return bestOption;
            }
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
            const float kDeltaExpansion = 1.25f;
            SimulationState iterationState = simState;
            const int kIterations = 15;
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

            float ownShotDistance = MonteCarloVehicleAI.ShotDistanceSq(controlledDynamicState, controlledGun, targetDynamicState.DynamicPosition);
            float targetShotDistance = MonteCarloVehicleAI.ShotDistanceSq(targetDynamicState, targetsGun, controlledDynamicState.DynamicPosition);
            float positionalValue = ownShotDistance / targetShotDistance;
            payout.AccumulatedPositioningValue += positionalValue;
        }

        private void ComputeResidueRolloutHits(SimulationState finalState, ref OptionPayout payout)
        {
            var remainingOwnProjectiles = finalState.GetProjectiles(mControlledID);
            if (remainingOwnProjectiles.Count > 0)
            {
                var targetState = finalState.GetVehicle(mTargetID);
                foreach (var projectile in remainingOwnProjectiles)
                {
                    if (SimulationProcessor.ProjectileHitsVehicle(targetState.DynamicTransform, mSimData.GetVehiclePrototype(mTargetID), projectile, 1000f))
                    {
                        payout.ShotsLanded += 1;
                    }
                }
            }
            var remainingEnemyProjectiles = finalState.GetProjectiles(mTargetID);
            if (remainingEnemyProjectiles.Count > 0)
            {
                var controlledState = finalState.GetVehicle(mControlledID);
                var controlledPrototype = mSimData.GetVehiclePrototype(mControlledID);
                foreach (var projectile in remainingEnemyProjectiles)
                {
                    Vector2 relativePosition = controlledState.DynamicTransform.Position - projectile.Position;
                    Vector2 relativeVelocity = controlledState.DynamicTransform.Velocity - projectile.Velocity;
                    if (Vector2.Dot(relativePosition, relativeVelocity) < 0f)
                    {
                        if (SimulationProcessor.ProjectileHitsVehicle(controlledState.DynamicTransform, controlledPrototype, projectile, 1000f))
                        {
                            payout.ShotsTaken += 1;
                        }
                        else
                        {
                            float timeToImpact;
                            float distanceSq = MonteCarloVehicleAI.ShotDistanceSq(projectile, controlledState.DynamicTransform.DynamicPosition, out timeToImpact);
                            payout.ProjectileThreat += 1f / (timeToImpact * timeToImpact * distanceSq);
                        }
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
            public int ShotsTaken;  //Deterministic
            public int ShotsLanded; //Random
            public float ProjectileThreat;
            public float AccumulatedPositioningValue;

            public void InitializeForExpand()
            {
                ShotsTaken = 0;
                ShotsLanded = 0;
                ProjectileThreat = 0f;
                AccumulatedPositioningValue = 0f;
            }

            public void InitializeForAccumulation()
            {
                ShotsTaken = int.MaxValue;
                ShotsLanded = 0;
                ProjectileThreat = float.MaxValue;
                AccumulatedPositioningValue = 0f;
            }

            public void Accumulate(OptionPayout otherPayout)
            {
                ShotsTaken = Math.Min(otherPayout.ShotsTaken, ShotsTaken);
                ShotsLanded += otherPayout.ShotsLanded;
                ProjectileThreat = Math.Min(ProjectileThreat, otherPayout.ProjectileThreat);
                AccumulatedPositioningValue += otherPayout.AccumulatedPositioningValue;
            }
        }
    }
}
