namespace MTSCombat.Simulation
{
    public struct GunState
    {
        public readonly int NextGunToFire;
        public readonly float TimeToNextShot;

        public GunState(int nextGunToFire, float timeBeforeNextShot)
        {
            NextGunToFire = nextGunToFire;
            TimeToNextShot = timeBeforeNextShot;
        }
    }
}
