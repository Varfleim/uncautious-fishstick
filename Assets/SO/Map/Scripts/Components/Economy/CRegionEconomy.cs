
using System.Collections.Generic;

using Leopotam.EcsLite;

using SO.Population.Events;

namespace SO.Map.Economy
{
    /// <summary>
    /// Компонент, хранящий данные о населении и сооружениях региона
    /// </summary>
    public struct CRegionEconomy
    {
        public CRegionEconomy(
            EcsPackedEntity selfPE)
        {
            this.selfPE = selfPE;

            pOPs = new();
            orderedPOPs = new();
        }

        public readonly EcsPackedEntity selfPE;

        #region Population
        public List<EcsPackedEntity> pOPs;
        public List<DROrderedPopulation> orderedPOPs;
        #endregion
    }
}