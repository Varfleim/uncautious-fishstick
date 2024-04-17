
using Leopotam.EcsLite;

namespace SO.Map.Events.MapArea
{
    public enum MapAreaChangeOwnerType : byte
    {
        Initialization,
        Test
    }

    public readonly struct RMapAreaChangeOwner
    {
        public RMapAreaChangeOwner(
            EcsPackedEntity newOwnerCountryPE,
            EcsPackedEntity mAPE,
            MapAreaChangeOwnerType requestType)
        {
            this.newOwnerCountryPE = newOwnerCountryPE;

            this.mAPE = mAPE;

            this.requestType = requestType;
        }

        public readonly EcsPackedEntity newOwnerCountryPE;

        public readonly EcsPackedEntity mAPE;

        public readonly MapAreaChangeOwnerType requestType;
    }
}