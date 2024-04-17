
using UnityEngine;

using System.Collections.Generic;

using Leopotam.EcsLite;

namespace SO.Map.Province
{
    /// <summary>
    /// Компонент, хранящий общие данные провинции
    /// </summary>
    public struct CProvinceCore
    {
        public CProvinceCore(
            EcsPackedEntity selfPE, int index,
            Vector3 center)
        {
            this.selfPE = selfPE;
            this.index = index;


            this.center = center;

            neighbourProvincePEs = new EcsPackedEntity[6];


            parentMapAreaPE = new();

            parentStatePE = new();


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

        #region ProvinceData
        public Vector3 center;

        public static readonly List<EcsPackedEntity> tempNeighbours = new List<EcsPackedEntity>(6);
        public EcsPackedEntity[] neighbourProvincePEs;
        #endregion

        #region MapAreaData
        public EcsPackedEntity ParentMapAreaPE
        {
            get
            {
                return parentMapAreaPE;
            }
        }
        EcsPackedEntity parentMapAreaPE;

        public void SetParentMapArea(
            EcsPackedEntity mapAreaPE)
        {
            parentMapAreaPE = mapAreaPE;
        }
        #endregion

        #region StateData
        public EcsPackedEntity ParentStatePE
        {
            get
            {
                return parentStatePE;
            }
        }
        EcsPackedEntity parentStatePE;

        public void SetParentState(
            EcsPackedEntity statePE)
        {
            parentStatePE = statePE;
        }
        #endregion

        #region TaskForceData
        public float crossCost;

        public List<EcsPackedEntity> taskForcePEs;
        #endregion
    }
}