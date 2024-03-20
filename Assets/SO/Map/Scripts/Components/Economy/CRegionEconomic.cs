
using Leopotam.EcsLite;

namespace SO.Map.Economy
{
    /// <summary>
    /// Компонент, хранящий данные о населении и сооружениях региона
    /// </summary>
    public struct CRegionEconomic
    {
        public CRegionEconomic(
            EcsPackedEntity selfPE)
        {
            this.selfPE = selfPE;
        }

        public readonly EcsPackedEntity selfPE;
    }
}