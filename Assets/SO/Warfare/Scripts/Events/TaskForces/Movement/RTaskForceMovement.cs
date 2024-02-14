
using Leopotam.EcsLite;

namespace SO.Warfare.Fleet.Movement.Events
{
    public readonly struct RTaskForceMovement
    {
        public RTaskForceMovement(
            EcsPackedEntity tFPE,
            EcsPackedEntity targetPE, TaskForceMovementTargetType targetType)
        {
            this.tFPE = tFPE;

            this.targetPE = targetPE;
            this.targetType = targetType;
        }

        public readonly EcsPackedEntity tFPE;

        public readonly EcsPackedEntity targetPE;
        public readonly TaskForceMovementTargetType targetType;
    }
}