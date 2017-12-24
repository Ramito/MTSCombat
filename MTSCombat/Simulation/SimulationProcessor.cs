using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MTSCombat.Simulation
{
    public static class SimulationProcessor
    {
        //Player input gets processed into a ControlState, and AI will provide a control state
        public static SimulationState ProcessState(SimulationState state, SimulationData simulationData, Dictionary<uint, VehicleControls> controllerInputs, float deltaTime)
        {
            int currentVehicleCount = state.Vehicles.Length;
            SimulationState nextSimState = new SimulationState(currentVehicleCount);
            int totalProjectileCount = 0;
            for (int i = 0; i < state.Projectiles.Length; ++i)
            {
                var projectiles = state.Projectiles[i];
                int localCount = 0;
                if (projectiles != null)
                {
                    localCount = projectiles.Count;
                }
                totalProjectileCount += localCount;
                nextSimState.SetProjectileCount(state.IndexToID[i], localCount + 1);
            }
            for (int vehicleIndex = 0; vehicleIndex < state.Vehicles.Length; ++vehicleIndex)
            {
                uint controllerID = state.IndexToID[vehicleIndex];
                Debug.Assert(controllerInputs.ContainsKey(controllerID));

                VehiclePrototype prototype = simulationData.GetVehiclePrototype(controllerID);
                VehicleControls inputControlState = controllerInputs[controllerID];
                VehicleState currentVehicleState = state.Vehicles[vehicleIndex];

                DynamicTransform2 newDynamicTransform = ProcessVehicleDrive(currentVehicleState.DynamicTransform, prototype, inputControlState.DriveControls, deltaTime);
                newDynamicTransform = ProcessCollision(newDynamicTransform, prototype, simulationData);

                GunState currentGunState = currentVehicleState.GunState;
                GunMount gunMount = prototype.Guns;
                bool projectileFired;
                GunState nextGunState = ProcessGunstate(gunMount, currentGunState, inputControlState.GunTriggerDown, deltaTime, out projectileFired);
                if (projectileFired)
                {
                    DynamicPosition2 projectileState = CreateProjectileState(newDynamicTransform, gunMount, currentGunState.NextGunToFire, deltaTime);
                    SpawnProjectile(vehicleIndex, nextSimState, projectileState);
                }

                VehicleState newVehicleState = new VehicleState(newDynamicTransform, inputControlState.DriveControls, nextGunState);

                nextSimState.AddVehicle(controllerID, newVehicleState);
            }
            //TODO: The above resulting transforms can be put in a collection ready for collision detection below!
            for (int projectileIndex = 0; projectileIndex < state.Projectiles.Length; ++projectileIndex)
            {
                var projectiles = state.Projectiles[projectileIndex];
                if (projectiles != null)
                {
                    foreach (var projectile in projectiles)
                    {
                        bool hit = false;
                        for (int targetVehicleIndex = 0; targetVehicleIndex < state.Vehicles.Length; ++targetVehicleIndex)
                        {
                            if (targetVehicleIndex != projectileIndex)
                            {
                                VehicleState vehicleToHit = state.Vehicles[targetVehicleIndex];
                                if (ProjectileHitsVehicle(vehicleToHit.DynamicTransform, simulationData.GetVehiclePrototype(state.IndexToID[targetVehicleIndex]), projectile, deltaTime))
                                {
                                    hit = true;
                                    break;
                                }
                            }
                        }
                        if (!hit)
                        {
                            Vector2 nextPosition = projectile.Position + deltaTime * projectile.Velocity;
                            if (simulationData.InsideArena(nextPosition))
                            {
                                DynamicPosition2 nextProjectileState = new DynamicPosition2(nextPosition, projectile.Velocity);
                                nextSimState.Projectiles[projectileIndex].Add(nextProjectileState);
                            }
                        }
                        else
                        {
                            RegisterHit(nextSimState, projectileIndex);
                        }
                    }
                }
            }
            return nextSimState;
        }

        public static DynamicPosition2 CreateProjectileState(DynamicTransform2 shooterDynamicState, GunMount gunMount, int firingBarrel, float deltaTime)
        {
            Vector2 gunLocalOffset = gunMount.LocalMountOffsets[firingBarrel];
            Vector2 shotPosition = shooterDynamicState.Position + shooterDynamicState.Orientation.LocalToGlobal(gunLocalOffset);
            Vector2 shotVelocity = gunMount.MountedGun.ShotSpeed * shooterDynamicState.Orientation.Facing + shooterDynamicState.Velocity;
            DynamicPosition2 projectileState = new DynamicPosition2(shotPosition + deltaTime * shotVelocity, shotVelocity);
            return projectileState;
        }

        public static void SpawnProjectile(int index, SimulationState state, DynamicPosition2 projectileState)
        {
            state.Projectiles[index].Add(projectileState);
        }

        private static void RegisterHit(SimulationState simState, int index)
        {
            int currentCount = simState.RegisteredHits[index];
            simState.RegisteredHits[index] = currentCount + 1;
        }

        private static DynamicTransform2 ProcessVehicleDrive(DynamicTransform2 currentVehicleState, VehiclePrototype prototype, VehicleDriveControls controlState, float deltaTime)
        {
            var newDynamicTransform = prototype.VehicleDrive(currentVehicleState, controlState, deltaTime);
            //Gun recoil would go here
            return newDynamicTransform;
        }

        private static DynamicTransform2 ProcessCollision(DynamicTransform2 newDynamicTransform, VehiclePrototype prototype, SimulationData simulationData)
        {
            float penetration;
            Vector2 collisionAxis;
            if (simulationData.CollisionWithArenaBounds(prototype.VehicleSize, newDynamicTransform.Position, out penetration, out collisionAxis))
            {
                const float kBounce = 2f;   //2 is rigid bounce, 1 is no bounce
                Vector2 newPosition = newDynamicTransform.Position + kBounce * penetration * collisionAxis;
                Vector2 newVelocity = newDynamicTransform.Velocity - kBounce * Vector2.Dot(newDynamicTransform.Velocity, collisionAxis) * collisionAxis;
                DynamicPosition2 newDynamicPosition = new DynamicPosition2(newPosition, newVelocity);
                newDynamicTransform = new DynamicTransform2(newDynamicPosition, newDynamicTransform.DynamicOrientation);
            }
            return newDynamicTransform;
        }

        private static bool ProjectileHitsVehicle(DynamicTransform2 vehicleTransformState, VehiclePrototype prototype, DynamicPosition2 projectileState, float deltaTime)
        {
            Vector2 projectileToVehicle = vehicleTransformState.Position - projectileState.Position;
            float currentDistanceSq = projectileToVehicle.LengthSquared();
            Vector2 relativeVelocities = vehicleTransformState.Velocity - projectileState.Velocity;
            float dot = Vector2.Dot(projectileToVehicle, relativeVelocities);
            if (dot <= 0f)
            {
                return currentDistanceSq <= prototype.VehicleSize * prototype.VehicleSize;
            }
            float relativeVelocityModuleSq = relativeVelocities.LengthSquared();
            float timeToClosest = Math.Min(-dot / relativeVelocityModuleSq, deltaTime);
            float closestDistanceSq = currentDistanceSq + timeToClosest * ((2f * dot) + (timeToClosest * relativeVelocityModuleSq));
            return closestDistanceSq <= prototype.VehicleSize * prototype.VehicleSize;
        }

        private static GunState ProcessGunstate(GunMount mount, GunState gunState, bool triggerDown, float deltaTime, out bool projectileFired)
        {
            int nextGunToFire = gunState.NextGunToFire;
            float timeToNextShot = Math.Max(gunState.TimeToNextShot - deltaTime, 0f);
            projectileFired = triggerDown && (timeToNextShot == 0f);
            if (projectileFired)
            {
                int gunCount = mount.LocalMountOffsets.Length;
                timeToNextShot = mount.MountedGun.DelayBetweenShots / (float)gunCount;
                nextGunToFire = (nextGunToFire + 1) % gunCount;
            }
            return new GunState(nextGunToFire, timeToNextShot);
        }
    }
}
