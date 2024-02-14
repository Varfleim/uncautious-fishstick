
using System.Collections.Generic;

using Leopotam.EcsLite;

namespace SO.Warfare.Fleet.Movement
{
    public enum TaskForceMovementTargetType : byte
    {
        None,
        Region,
        TaskForce
    }

    public struct CTaskForceMovement
    {
        public CTaskForceMovement(
            EcsPackedEntity targetPE, TaskForceMovementTargetType targetType)
        {
            this.targetPE = targetPE;
            this.targetType = targetType;

            pathRegionPEs = new();

            traveledDistance = 0;
            isTraveled = false;
        }

        public EcsPackedEntity targetPE;
        public TaskForceMovementTargetType targetType;

        public List<EcsPackedEntity> pathRegionPEs;

        public float traveledDistance;
        public bool isTraveled;
    }
}