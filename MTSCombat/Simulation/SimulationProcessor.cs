﻿using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace MTSCombat.Simulation
{
    public sealed class SimulationProcessor
    {
        private readonly SimulationData mSimulationData;
        private List<VehicleState> mPendingVehicleSpawns;

        public SimulationProcessor(int players, int arenaWidth, int arenaHeight)
        {
            mSimulationData = new SimulationData(players, arenaWidth, arenaHeight);
            mPendingVehicleSpawns = new List<VehicleState>(players);
        }

        public void RegisterVehicle(uint playerID, PlayerData playerData, VehicleState initialState)
        {
            mSimulationData.RegisterPlayer(playerID, playerData);
            mPendingVehicleSpawns.Add(initialState);
        }

        public GunData GetGunDataFor(uint playerID)
        {
            return mSimulationData.GetData(playerID).GunMount.MountedGun;
        }

        //Player input gets processed into a ControlState, and AI will provide a control state
        public SimulationState ProcessState(SimulationState state, Dictionary<uint, ControlState> controllerInputs, float deltaTime)
        {
            int currentVehicleCount = state.Vehicles.Count;
            int currentProjectileCount = state.Projectiles.Count;
            int spawningVehicles = mPendingVehicleSpawns.Count;
            SimulationState nextSimState = new SimulationState(currentVehicleCount + spawningVehicles, currentProjectileCount + currentProjectileCount);
            foreach (var spawningVehicle in mPendingVehicleSpawns)
            {
                state.Vehicles.Add(spawningVehicle);
                controllerInputs[spawningVehicle.ControllerID] = spawningVehicle.ControlState;  //Default control state on first frame
            }
            mPendingVehicleSpawns.Clear();
            foreach (var currentVehicleState in state.Vehicles)
            {
                uint controllerID = currentVehicleState.ControllerID;
                System.Diagnostics.Debug.Assert(controllerInputs.ContainsKey(controllerID));
                ControlState inputControlState = controllerInputs[controllerID];

                VehicleState newVehicleState = ProcessVehicle(currentVehicleState, inputControlState, deltaTime);

                GunState currentGunState = currentVehicleState.GunState;
                PlayerData playerData = mSimulationData.GetData(controllerID);
                GunMount gunMount = playerData.GunMount;
                bool projectileFired;
                GunState nextGunState = ProcessGunstate(gunMount, currentGunState, inputControlState.GunTriggerDown(), deltaTime, out projectileFired);
                if (projectileFired)
                {
                    Vector2 gunLocalOffset = gunMount.LocalMountOffsets[currentGunState.NextGunToFire];
                    Vector2 shotPosition = newVehicleState.DynamicTransform.Position + newVehicleState.DynamicTransform.Orientation.LocalToGlobal(gunLocalOffset);
                    Vector2 shotVelocity = gunMount.MountedGun.ShotSpeed * newVehicleState.DynamicTransform.Orientation.Facing + newVehicleState.DynamicTransform.Velocity;
                    DynamicPosition2 projectileState = new DynamicPosition2(shotPosition, shotVelocity);
                    state.Projectiles.Add(projectileState);
                }
                newVehicleState.SetGunState(nextGunState);

                nextSimState.Vehicles.Add(newVehicleState);
            }
            foreach (var projectile in state.Projectiles)
            {
                Vector2 nextPosition = projectile.Position + deltaTime * projectile.Velocity;
                if (InsideArena(nextPosition))
                {
                    bool hit = false;
                    DynamicPosition2 nextProjectileState = new DynamicPosition2(nextPosition, projectile.Velocity);
                    foreach (var vehicleToHit in nextSimState.Vehicles)
                    {
                        if (ProjectileHitsVehicle(vehicleToHit, nextProjectileState))
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

        private VehicleState ProcessVehicle(VehicleState currentVehicleState, ControlState inputControlState, float deltaTime)
        {
            var newDynamicTransform = inputControlState.ProcessState(currentVehicleState.DynamicTransform, deltaTime);

            //Check collisions!
            float penetration;
            Vector2 collisionAxis;
            if (CollisionWithArena(currentVehicleState.Size, newDynamicTransform.Position, out penetration, out collisionAxis))
            {
                Vector2 newPosition = newDynamicTransform.Position + 2f * penetration * collisionAxis;
                Vector2 newVelocity = newDynamicTransform.Velocity - 2f * Vector2.Dot(newDynamicTransform.Velocity, collisionAxis) * collisionAxis;
                DynamicPosition2 newDynamicPosition = new DynamicPosition2(newPosition, newVelocity);
                newDynamicTransform = new DynamicTransform2(newDynamicPosition, newDynamicTransform.DynamicOrientation);
            }
            VehicleState newVehicleState = new VehicleState();
            newVehicleState.SetControllerID(currentVehicleState.ControllerID);
            newVehicleState.SetState(currentVehicleState.Size, newDynamicTransform, inputControlState);
            return newVehicleState;
        }

        private bool CollisionWithArena(float size, Vector2 position, out float penetration, out Vector2 collisionNormal)
        {
            if (position.X <= size)
            {
                penetration = size - position.X;
                collisionNormal = Vector2.UnitX;
                return true;
            }
            if (position.X > (mSimulationData.ArenaWidth - size))
            {
                penetration = position.X - (mSimulationData.ArenaWidth - size);
                collisionNormal = -Vector2.UnitX;
                return true;
            }
            if (position.Y <= size)
            {
                penetration = size - position.Y;
                collisionNormal = Vector2.UnitY;
                return true;
            }
            if (position.Y > (mSimulationData.ArenaHeight - size))
            {
                penetration = position.Y - (mSimulationData.ArenaHeight - size);
                collisionNormal = -Vector2.UnitY;
                return true;
            }
            penetration = 0f;
            collisionNormal = Vector2.Zero;
            return false;
        }

        private bool ProjectileHitsVehicle(VehicleState vehicleState, DynamicPosition2 projectileState)
        {
            float distanceSq = (projectileState.Position - vehicleState.DynamicTransform.Position).LengthSquared();
            return distanceSq <= (vehicleState.Size * vehicleState.Size);
        }

        private bool InsideArena(Vector2 position)
        {
            return ((position.X >= 0f) && (position.X <= mSimulationData.ArenaWidth))
            && ((position.Y >= 0f) && (position.Y <= mSimulationData.ArenaHeight));
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

    public sealed class SimulationData
    {
        //Experiment to separate state that will need to be replicated and global data
        private Dictionary<uint, PlayerData> mDataMap;

        public readonly float ArenaWidth;
        public readonly float ArenaHeight;

        public SimulationData(int expectedPlayers, int arenaWidth, int arenaHeight)
        {
            mDataMap = new Dictionary<uint, PlayerData>(expectedPlayers);
            ArenaWidth = arenaWidth;
            ArenaHeight = arenaHeight;
        }

        public void RegisterPlayer(uint playerID, PlayerData playerData)
        {
            mDataMap[playerID] = playerData;
        }

        public PlayerData GetData(uint playerID)
        {
            return mDataMap[playerID];
        }
    }

    public sealed class PlayerData
    {
        public readonly GunMount GunMount;
        //Vehicle prototype needs to be here

        public PlayerData(GunMount gunMount)
        {
            GunMount = gunMount;
        }
    }
}
