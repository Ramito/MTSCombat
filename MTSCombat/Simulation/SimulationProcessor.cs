﻿using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MTSCombat.Simulation
{
    public sealed class SimulationProcessor
    {
        private Dictionary<uint, VehicleState> mPendingVehicleSpawns;

        public SimulationProcessor(int players)
        {
            mPendingVehicleSpawns = new Dictionary<uint, VehicleState>(players);
        }

        public void RegisterVehicle(uint playerID, VehicleState initialState)
        {
            mPendingVehicleSpawns.Add(playerID, initialState);
        }

        //Player input gets processed into a ControlState, and AI will provide a control state
        public SimulationState ProcessState(SimulationState state, SimulationData simulationData, Dictionary<uint, VehicleControls> controllerInputs, float deltaTime)
        {
            int currentVehicleCount = state.Vehicles.Count;
            int currentProjectileCount = state.Projectiles.Count;
            int spawningVehicles = mPendingVehicleSpawns.Count;
            SimulationState nextSimState = new SimulationState(currentVehicleCount + spawningVehicles, currentProjectileCount + currentProjectileCount);
            foreach (var kvp in mPendingVehicleSpawns)
            {
                state.Vehicles.Add(kvp.Key, kvp.Value);
                controllerInputs[kvp.Key] = new VehicleControls(simulationData.GetPlayerData(kvp.Key).Prototype.ControlConfig.DefaultControl);  //Default control state on first frame
            }
            mPendingVehicleSpawns.Clear();
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
                    Vector2 gunLocalOffset = gunMount.LocalMountOffsets[currentGunState.NextGunToFire];
                    Vector2 shotPosition = newDynamicTransform.Position + newDynamicTransform.Orientation.LocalToGlobal(gunLocalOffset);
                    Vector2 shotVelocity = gunMount.MountedGun.ShotSpeed * newDynamicTransform.Orientation.Facing + newDynamicTransform.Velocity;
                    DynamicPosition2 projectileState = new DynamicPosition2(shotPosition, shotVelocity);
                    state.Projectiles.Add(projectileState);
                }

                VehicleState newVehicleState = new VehicleState();
                newVehicleState.SetDriveState(newDynamicTransform, inputControlState.DriveControls);
                newVehicleState.SetGunState(nextGunState);

                nextSimState.Vehicles.Add(controllerID, newVehicleState);
            }
            //TODO: The above resulting transforms can be put in a collection ready for collision detection below!
            foreach (var projectile in state.Projectiles)
            {
                Vector2 nextPosition = projectile.Position + deltaTime * projectile.Velocity;
                if (simulationData.InsideArena(nextPosition))
                {
                    bool hit = false;
                    DynamicPosition2 nextProjectileState = new DynamicPosition2(nextPosition, projectile.Velocity);
                    foreach (var vehicleToHit in nextSimState.Vehicles)
                    {
                        //TODO: I dislike this chunk
                        if (ProjectileHitsVehicle(vehicleToHit.Value.DynamicTransform, simulationData.GetPlayerData(vehicleToHit.Key).Prototype, nextProjectileState))
                        {
                            hit = true;
                            break;
                        }
                    }
                    if (!hit)
                    {
                        nextSimState.Projectiles.Add(nextProjectileState);
                    }
                }
            }
            return nextSimState;
        }

        private DynamicTransform2 ProcessVehicleDrive(DynamicTransform2 currentVehicleState, VehiclePrototype prototype, VehicleDriveControls controlState, float deltaTime)
        {
            var newDynamicTransform = prototype.VehicleDrive(currentVehicleState, controlState, deltaTime);
            //Gun recoil would go here
            return newDynamicTransform;
        }

        private DynamicTransform2 ProcessCollision(DynamicTransform2 newDynamicTransform, VehiclePrototype prototype, SimulationData simulationData)
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

        private bool ProjectileHitsVehicle(DynamicTransform2 vehicleTransformState, VehiclePrototype prototype, DynamicPosition2 projectileState)
        {
            float distanceSq = (projectileState.Position - vehicleTransformState.Position).LengthSquared();
            return distanceSq <= (prototype.VehicleSize * prototype.VehicleSize);
        }

        private GunState ProcessGunstate(GunMount mount, GunState gunState, bool triggerDown, float deltaTime, out bool projectileFired)
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
