namespace MTSCombat.Simulation
{
    public struct GunData
    {
        public readonly float DelayBetweenShots;
        public readonly float ShotSpeed;

        public GunData(float delayBetweenShots, float shotSpeed)
        {
            DelayBetweenShots = delayBetweenShots;
            ShotSpeed = shotSpeed;
        }
    }
}
