
using Leopotam.EcsLite;

namespace SO.Warfare.Fleet.Movement.Events
{
    public struct SRTaskForceFindPath
    {
        public SRTaskForceFindPath(
            EcsPackedEntity targetProvincePE)
        {
            this.targetProvincePE = targetProvincePE;
        }

        public readonly EcsPackedEntity targetProvincePE;
    }
}