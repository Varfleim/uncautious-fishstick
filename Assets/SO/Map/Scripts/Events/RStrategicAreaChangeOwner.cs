
using Leopotam.EcsLite;

namespace SO.Map.Events
{
    public enum StrategicAreaChangeOwnerType : byte
    {
        Initialization,
        Test
    }

    public readonly struct RStrategicAreaChangeOwner
    {
        public RStrategicAreaChangeOwner(
            EcsPackedEntity countryPE,
            EcsPackedEntity sAPE,
            StrategicAreaChangeOwnerType requestType)
        {
            this.countryPE = countryPE;

            this.sAPE = sAPE;

            this.requestType = requestType;
        }

        public readonly EcsPackedEntity countryPE;

        public readonly EcsPackedEntity sAPE;

        public readonly StrategicAreaChangeOwnerType requestType;
    }
}