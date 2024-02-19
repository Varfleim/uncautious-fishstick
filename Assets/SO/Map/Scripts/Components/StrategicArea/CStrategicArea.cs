
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

            elevation = 0;

            regionPEs = new EcsPackedEntity[0];

            neighbourSAPEs = new EcsPackedEntity[0];
        }

        public readonly EcsPackedEntity selfPE;

        public readonly Color selfColor;

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

        public EcsPackedEntity[] regionPEs;

        public EcsPackedEntity[] neighbourSAPEs;
    }
}