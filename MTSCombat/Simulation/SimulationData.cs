﻿using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace MTSCombat.Simulation
{
    public sealed class SimulationData
    {
        //Experiment to separate state that will need to be replicated and global data
        private readonly Dictionary<uint, PlayerData> mDataMap;

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

        public PlayerData GetPlayerData(uint playerID)
        {
            return mDataMap[playerID];
        }

        public bool CollisionWithArenaBounds(float size, Vector2 position, out float penetration, out Vector2 collisionNormal)
        {
            if (position.X <= size)
            {
                penetration = size - position.X;
                collisionNormal = Vector2.UnitX;
                return true;
            }
            if (position.X > (ArenaWidth - size))
            {
                penetration = position.X - (ArenaWidth - size);
                collisionNormal = -Vector2.UnitX;
                return true;
            }
            if (position.Y <= size)
            {
                penetration = size - position.Y;
                collisionNormal = Vector2.UnitY;
                return true;
            }
            if (position.Y > (ArenaHeight - size))
            {
                penetration = position.Y - (ArenaHeight - size);
                collisionNormal = -Vector2.UnitY;
                return true;
            }
            penetration = 0f;
            collisionNormal = Vector2.Zero;
            return false;
        }

        public bool InsideArena(Vector2 position)
        {
            return (position.X >= 0f)
                && (position.Y >= 0f)
                && (position.X <= ArenaWidth)
                && (position.Y <= ArenaHeight);
        }
    }

    public sealed class PlayerData
    {
        public readonly VehiclePrototype Prototype;

        public PlayerData(VehiclePrototype vehiclePrototype)
        {
            Prototype = vehiclePrototype;
        }
    }
}
