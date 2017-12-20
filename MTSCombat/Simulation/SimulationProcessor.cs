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
            int currentVehicleCount = state.Vehicles.Count;
            SimulationState nextSimState = new SimulationState(currentVehicleCount);
            foreach (var kvp in state.Projectiles)
            {
                uint id = kvp.Key;
                List<DynamicPosition2> projectileList;
                if (state.Projectiles.TryGetValue(id, out projectileList))
                {
                    nextSimState.SetProjectileCount(id, projectileList.Count);
                }
            }
            foreach (var currentVehicleStateKVP in state.Vehicles)
            {
                uint controllerID = currentVehicleStateKVP.Key;
                Debug.Assert(controllerInputs.ContainsKey(controllerID));

                VehiclePrototype prototype = simulationData.GetPlayerData(controllerID).Prototype;
                VehicleControls inputControlState = controllerInputs[controllerID];
                VehicleState currentState = currentVehicleStateKVP.Value;

                DynamicTransform2 newDynamicTransform = ProcessVehicleDrive(currentState.DynamicTransform, prototype, inputControlState.DriveControls, deltaTime);
                newDynamicTransform = ProcessCollision(newDynamicTransform, prototype, simulationData);

                GunState currentGunState = currentState.GunState;
                GunMount gunMount = prototype.Guns;
                bool projectileFired;
                GunState nextGunState = ProcessGunstate(gunMount, currentGunState, inputControlState.GunTriggerDown, deltaTime, out projectileFired);
                if (projectileFired)
                {
                    DynamicPosition2 projectileState = CreateProjectileState(newDynamicTransform, gunMount, currentGunState.NextGunToFire);
                    SpawnProjectile(controllerID, state, projectileState);
                }

                VehicleState newVehicleState = new VehicleState();
                newVehicleState.SetDriveState(newDynamicTransform, inputControlState.DriveControls);
                newVehicleState.SetGunState(nextGunState);

                nextSimState.Vehicles.Add(controllerID, newVehicleState);
            }
            //TODO: The above resulting transforms can be put in a collection ready for collision detection below!
            foreach (var projectileKVP in state.Projectiles)
            {
                foreach (var projectile in projectileKVP.Value)
                {
                    Vector2 nextPosition = projectile.Position + deltaTime * projectile.Velocity;
                    if (simulationData.InsideArena(nextPosition))
                    {
                        bool hit = false;
                        DynamicPosition2 nextProjectileState = new DynamicPosition2(nextPosition, projectile.Velocity);
                        foreach (var vehicleToHit in nextSimState.Vehicles)
                        {
                            if (vehicleToHit.Key != projectileKVP.Key)
                            {
                                if (ProjectileHitsVehicle(vehicleToHit.Value.DynamicTransform, simulationData.GetPlayerData(vehicleToHit.Key).Prototype, nextProjectileState))
                                {
                                    hit = true;
                                    break;
                                }
                            }
                        }
                        if (!hit)
                        {
                            nextSimState.Projectiles[projectileKVP.Key].Add(nextProjectileState);
                        }
                        else
                        {
                            RegisterHit(nextSimState, projectileKVP.Key);
                        }
                    }
                }
            }
            return nextSimState;
        }

        public static DynamicPosition2 CreateProjectileState(DynamicTransform2 shooterDynamicState, GunMount gunMount, int firingBarrel)
        {
            Vector2 gunLocalOffset = gunMount.LocalMountOffsets[firingBarrel];
            Vector2 shotPosition = shooterDynamicState.Position + shooterDynamicState.Orientation.LocalToGlobal(gunLocalOffset);
            Vector2 shotVelocity = gunMount.MountedGun.ShotSpeed * shooterDynamicState.Orientation.Facing + shooterDynamicState.Velocity;
            DynamicPosition2 projectileState = new DynamicPosition2(shotPosition, shotVelocity);
            return projectileState;
        }
        public static void SpawnProjectile(uint shooterID, SimulationState state, DynamicPosition2 projectileState)
        {
            state.Projectiles[shooterID].Add(projectileState);
        }

        private static void RegisterHit(SimulationState simState, uint hitVehicleID)
        {
            int currentCount;
            if (!simState.RegisteredHits.TryGetValue(hitVehicleID, out currentCount))
            {
                currentCount = 0;
            }
            simState.RegisteredHits[hitVehicleID] = currentCount + 1;
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
                Vector2 newPosition = newDynamicTransform.Position + 2f * penetration * collisionAxis;
                Vector2 newVelocity = newDynamicTransform.Velocity - 2f * Vector2.Dot(newDynamicTransform.Velocity, collisionAxis) * collisionAxis;
                DynamicPosition2 newDynamicPosition = new DynamicPosition2(newPosition, newVelocity);
                newDynamicTransform = new DynamicTransform2(newDynamicPosition, newDynamicTransform.DynamicOrientation);
            }
            return newDynamicTransform;
        }

        private static bool ProjectileHitsVehicle(DynamicTransform2 vehicleTransformState, VehiclePrototype prototype, DynamicPosition2 projectileState)
        {
            float distanceSq = (projectileState.Position - vehicleTransformState.Position).LengthSquared();
            return distanceSq <= (prototype.VehicleSize * prototype.VehicleSize);
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
