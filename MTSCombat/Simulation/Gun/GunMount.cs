using Microsoft.Xna.Framework;

namespace MTSCombat.Simulation
{
    public sealed class GunMount
    {
        public readonly GunData MountedGun;
        public readonly Vector2[] LocalMountOffsets;

        public GunMount(GunData gunData, Vector2[] offsets)
        {
            MountedGun = gunData;
            LocalMountOffsets = offsets;
        }
    }
}
