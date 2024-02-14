
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

            elevation = 0;
            waterLevel = 0;
            terrainTypeIndex = 0;

            neighbourRegionPEs = new EcsPackedEntity[6];


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

        public int Elevation
        {
            get
            {
                return elevation;
            }
            set
            {
                if (elevation != value)
                {
                    elevation = value;
                }
            }
        }
        int elevation;
        public int ViewElevation
        {
            get
            {
                return elevation >= waterLevel ? elevation : waterLevel;
            }
        }

        public int WaterLevel
        {
            get
            {
                return waterLevel;
            }
            set
            {
                if (waterLevel == value)
                {
                    return;
                }
                waterLevel = value;
            }
        }
        int waterLevel;
        public bool IsUnderwater
        {
            get
            {
                return waterLevel > elevation;
            }
        }

        public int TerrainTypeIndex
        {
            get
            {
                return terrainTypeIndex;
            }
            set
            {
                if (terrainTypeIndex != value)
                {
                    terrainTypeIndex = value;
                }
            }
        }
        int terrainTypeIndex;


        public static readonly List<EcsPackedEntity> tempNeighbours = new List<EcsPackedEntity>(6);
        public EcsPackedEntity[] neighbourRegionPEs;
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