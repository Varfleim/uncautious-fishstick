
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
            EcsPackedEntity characterPE,
            EcsPackedEntity sAPE,
            StrategicAreaChangeOwnerType requestType)
        {
            this.characterPE = characterPE;

            this.sAPE = sAPE;

            this.requestType = requestType;
        }

        public readonly EcsPackedEntity characterPE;

        public readonly EcsPackedEntity sAPE;

        public readonly StrategicAreaChangeOwnerType requestType;
    }
}