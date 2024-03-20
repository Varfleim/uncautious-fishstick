
using Leopotam.EcsLite;

namespace SO.Map.Events
{
    public enum RegionChangeOwnerType : byte
    {
        Initialization,
        Test
    }

    public readonly struct RRegionChangeOwner
    {
        public RRegionChangeOwner(
            EcsPackedEntity characterPE,
            EcsPackedEntity regionPE,
            RegionChangeOwnerType requestType)
        {
            this.characterPE = characterPE;

            this.regionPE = regionPE;

            this.requestType = requestType;
        }

        public readonly EcsPackedEntity characterPE;

        public readonly EcsPackedEntity regionPE;

        public readonly RegionChangeOwnerType requestType;
    }
}