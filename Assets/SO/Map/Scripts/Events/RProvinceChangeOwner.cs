
using Leopotam.EcsLite;

namespace SO.Map.Events
{
    public enum ProvinceChangeOwnerType : byte
    {
        Initialization,
        Test
    }

    public readonly struct RProvinceChangeOwner
    {
        public RProvinceChangeOwner(
            EcsPackedEntity countryPE,
            EcsPackedEntity provincePE,
            ProvinceChangeOwnerType requestType)
        {
            this.countryPE = countryPE;

            this.provincePE = provincePE;

            this.requestType = requestType;
        }

        public readonly EcsPackedEntity countryPE;

        public readonly EcsPackedEntity provincePE;

        public readonly ProvinceChangeOwnerType requestType;
    }
}