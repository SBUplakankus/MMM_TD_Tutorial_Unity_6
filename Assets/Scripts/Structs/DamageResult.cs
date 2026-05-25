namespace Structs
{
    public readonly struct DamageResult
    {
        public bool Died { get; }
        public float DamageDealt { get; }

        private DamageResult(bool died, float damageDealt)
        {
            Died = died;
            DamageDealt = damageDealt;
        }

        public static DamageResult Alive(float damageDealt) => new(false, damageDealt);
        public static DamageResult Dead(float damageDealt) => new(true, damageDealt);
    }
}