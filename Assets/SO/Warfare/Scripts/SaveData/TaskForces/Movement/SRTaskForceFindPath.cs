
using Leopotam.EcsLite;

namespace SO.Warfare.Fleet.Movement.Events
{
    public struct SRTaskForceFindPath
    {
        public SRTaskForceFindPath(
            EcsPackedEntity targetRegionPE)
        {
            this.targetRegionPE = targetRegionPE;
        }

        public readonly EcsPackedEntity targetRegionPE;
    }
}