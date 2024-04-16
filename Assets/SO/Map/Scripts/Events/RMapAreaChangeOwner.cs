
using Leopotam.EcsLite;

namespace SO.Map.Events
{
    public enum MapAreaChangeOwnerType : byte
    {
        Initialization,
        Test
    }

    public readonly struct RMapAreaChangeOwner
    {
        public RMapAreaChangeOwner(
            EcsPackedEntity countryPE,
            EcsPackedEntity mAPE,
            MapAreaChangeOwnerType requestType)
        {
            this.countryPE = countryPE;

            this.mAPE = mAPE;

            this.requestType = requestType;
        }

        public readonly EcsPackedEntity countryPE;

        public readonly EcsPackedEntity mAPE;

        public readonly MapAreaChangeOwnerType requestType;
    }
}