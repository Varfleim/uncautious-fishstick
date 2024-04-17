
using Leopotam.EcsLite;

namespace SO.Map.MapArea
{
    public struct DState
    {
        public DState(
            EcsPackedEntity statePE,
            EcsPackedEntity ownerCountryPE)
        {
            this.statePE = statePE;

            this.ownerCountryPE = ownerCountryPE;
        }

        public readonly EcsPackedEntity statePE;

        public readonly EcsPackedEntity ownerCountryPE;
    }
}