namespace SO.Faction.Events
{
    public readonly struct RFactionCreating
    {
        public RFactionCreating(
            string factionName)
        {
            this.factionName = factionName;
        }

        public readonly string factionName;
    }
}