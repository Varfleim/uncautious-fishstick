
using Leopotam.EcsLite;

namespace SO.Map.Events
{
    public enum RCChangeOwnerType : byte
    {
        Initialization
    }

    public readonly struct RRCChangeOwner
    {
        public RRCChangeOwner(
            EcsPackedEntity factionPE,
            EcsPackedEntity regionPE,
            RCChangeOwnerType requestType)
        {
            this.factionPE = factionPE;

            this.regionPE = regionPE;

            this.requestType = requestType;
        }

        public readonly EcsPackedEntity factionPE;

        public readonly EcsPackedEntity regionPE;

        public readonly RCChangeOwnerType requestType;
    }
}