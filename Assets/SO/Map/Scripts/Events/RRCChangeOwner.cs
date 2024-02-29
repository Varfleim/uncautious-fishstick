
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
            EcsPackedEntity characterPE,
            EcsPackedEntity regionPE,
            RCChangeOwnerType requestType)
        {
            this.characterPE = characterPE;

            this.regionPE = regionPE;

            this.requestType = requestType;
        }

        public readonly EcsPackedEntity characterPE;

        public readonly EcsPackedEntity regionPE;

        public readonly RCChangeOwnerType requestType;
    }
}