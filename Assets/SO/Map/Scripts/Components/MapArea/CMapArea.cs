
using System.Collections.Generic;

using UnityEngine;

using Leopotam.EcsLite;

namespace SO.Map.MapArea
{
    public struct CMapArea
    {
        public CMapArea(
            EcsPackedEntity selfPE,
            Color selfColor)
        {
            this.selfPE = selfPE;

            this.selfColor = selfColor;


            provincePEs = new EcsPackedEntity[0];

            neighbourMAPEs = new EcsPackedEntity[0];

            elevation = 0;


            ownerCountryPE = new();
        }

        public readonly EcsPackedEntity selfPE;

        public readonly Color selfColor;

        #region MapAreaData
        public EcsPackedEntity[] provincePEs;

        public EcsPackedEntity[] neighbourMAPEs;

        public int Elevation
        {
            get
            {
                return elevation;
            }
            set
            {
                elevation = value;
            }
        }
        int elevation;
        #endregion

        #region CountryData
        public EcsPackedEntity ownerCountryPE;
        #endregion
    }
}