
using Leopotam.EcsLite;

namespace SO.Warfare.Fleet.Events
{
    public readonly struct RTaskForceCreating
    {
        public RTaskForceCreating(
            EcsPackedEntity ownerCountryPE)
        {
            this.ownerCountryPE = ownerCountryPE;
        }

        public readonly EcsPackedEntity ownerCountryPE;
    }
}