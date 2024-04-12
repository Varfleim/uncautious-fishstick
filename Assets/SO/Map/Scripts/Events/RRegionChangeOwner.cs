
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
            EcsPackedEntity countryPE,
            EcsPackedEntity regionPE,
            RegionChangeOwnerType requestType)
        {
            this.countryPE = countryPE;

            this.regionPE = regionPE;

            this.requestType = requestType;
        }

        public readonly EcsPackedEntity countryPE;

        public readonly EcsPackedEntity regionPE;

        public readonly RegionChangeOwnerType requestType;
    }
}