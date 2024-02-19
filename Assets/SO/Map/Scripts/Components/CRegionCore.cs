
using UnityEngine;

using System.Collections.Generic;

using Leopotam.EcsLite;

namespace SO.Map
{
    public struct CRegionCore
    {
        public CRegionCore(
            EcsPackedEntity selfPE, int index,
            Vector3 center)
        {
            this.selfPE = selfPE;
            this.index = index;


            this.center = center;

            neighbourRegionPEs = new EcsPackedEntity[6];


            parentStrategicAreaPE = new();


            ownerFactionPE = new();

            rFOPEs = new DRegionFactionObject[0];


            crossCost = 1;

            taskForcePEs = new();
        }

        public readonly EcsPackedEntity selfPE;
        public readonly int Index
        {
            get
            {
                return index;
            }
        }
        readonly int index;

        #region RegionData
        public Vector3 center;

        public static readonly List<EcsPackedEntity> tempNeighbours = new List<EcsPackedEntity>(6);
        public EcsPackedEntity[] neighbourRegionPEs;
        #endregion

        #region StrategicAreaData
        public EcsPackedEntity ParentStrategicAreaPE
        {
            get
            {
                return parentStrategicAreaPE;
            }
        }
        EcsPackedEntity parentStrategicAreaPE;

        public void SetParentStrategicArea(
            EcsPackedEntity strategicAreaPE)
        {
            parentStrategicAreaPE = strategicAreaPE;
        }
        #endregion

        #region FactionData
        public EcsPackedEntity ownerFactionPE;

        public DRegionFactionObject[] rFOPEs;
        #endregion

        #region TaskForceData
        public float crossCost;

        public List<EcsPackedEntity> taskForcePEs;
        #endregion
    }
}