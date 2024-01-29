
using Leopotam.EcsLite;

namespace SO.Economy.RFO.Events
{
    public enum RegionChangeOwnerType : byte
    {
        Initialization
    }

    public readonly struct RRFOChangeOwner
    {
        public RRFOChangeOwner(
            EcsPackedEntity factionPE,
            EcsPackedEntity regionPE,
            RegionChangeOwnerType actionType)
        {
            this.factionPE = factionPE;

            this.regionPE = regionPE;

            this.actionType = actionType;
        }

        public readonly EcsPackedEntity factionPE;

        public readonly EcsPackedEntity regionPE;

        public readonly RegionChangeOwnerType actionType;
    }
}