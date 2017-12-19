using Microsoft.Xna.Framework;
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

        public void RegisterVehicle(ushort playerID, PlayerData playerData, VehicleState initialState)
        {
            mSimulationData.RegisterPlayer(playerID, playerData);
            mPendingVehicleSpawns.Add(initialState);
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
            foreach (var vehicle in state.Vehicles)
            {
                uint controllerID = vehicle.ControllerID;
                System.Diagnostics.Debug.Assert(controllerInputs.ContainsKey(controllerID));
                ControlState inputControlState = controllerInputs[controllerID];
                var newDynamicTransform = inputControlState.ProcessState(vehicle.DynamicTransform, deltaTime);
                VehicleState newVehicleState = new VehicleState();
                newVehicleState.SetControllerID(controllerID);
                newVehicleState.SetState(vehicle.Size, newDynamicTransform, inputControlState);

                GunState currentGunState = vehicle.GunState;
                PlayerData playerData = mSimulationData.GetData(controllerID);
                GunMount gunMount = playerData.GunMount;
                bool projectileFired;
                GunState nextGunState = ProcessGunstate(gunMount, currentGunState, inputControlState.GunTriggerDown(), deltaTime, out projectileFired);
                if (projectileFired)
                {
                    Vector2 gunLocalOffset = gunMount.LocalMountOffsets[currentGunState.NextGunToFire];
                    Vector2 shotPosition = newDynamicTransform.Position + newDynamicTransform.Orientation.LocalToGlobal(gunLocalOffset);
                    Vector2 shotVelocity = gunMount.MountedGun.ShotSpeed * newDynamicTransform.Orientation.Facing + newDynamicTransform.Velocity;
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
                    DynamicPosition2 nextProjectileState = new DynamicPosition2(nextPosition, projectile.Velocity);
                    nextSimState.Projectiles.Add(nextProjectileState);
                }
            }
            return nextSimState;
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
