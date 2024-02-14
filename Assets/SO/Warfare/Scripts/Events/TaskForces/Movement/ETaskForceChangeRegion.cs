
using Leopotam.EcsLite;

namespace SO.Warfare.Fleet.Movement
{
    public readonly struct ETaskForceChangeRegion
    {
        public ETaskForceChangeRegion(
            EcsPackedEntity tFPE)
        {
            this.tFPE = tFPE;
        }

        public readonly EcsPackedEntity tFPE;
    }
}