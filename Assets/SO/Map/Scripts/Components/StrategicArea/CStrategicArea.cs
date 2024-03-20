
using System.Collections.Generic;

using UnityEngine;

using Leopotam.EcsLite;

namespace SO.Map.StrategicArea
{
    public struct CStrategicArea
    {
        public CStrategicArea(
            EcsPackedEntity selfPE,
            Color selfColor)
        {
            this.selfPE = selfPE;

            this.selfColor = selfColor;


            regionPEs = new EcsPackedEntity[0];

            neighbourSAPEs = new EcsPackedEntity[0];

            elevation = 0;


            ownerCharacterPE = new();
        }

        public readonly EcsPackedEntity selfPE;

        public readonly Color selfColor;

        #region StrategicAreaData
        public EcsPackedEntity[] regionPEs;

        public EcsPackedEntity[] neighbourSAPEs;

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

        #region CharacterData
        public EcsPackedEntity ownerCharacterPE;
        #endregion
    }
}