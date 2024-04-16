
using Leopotam.EcsLite;

namespace SO.Warfare.Fleet.Movement
{
    public readonly struct ETaskForceChangeProvince
    {
        public ETaskForceChangeProvince(
            EcsPackedEntity tFPE)
        {
            this.tFPE = tFPE;
        }

        public readonly EcsPackedEntity tFPE;
    }
}