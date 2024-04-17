
using System.Collections.Generic;

using Leopotam.EcsLite;

namespace SO.Map.Events.Province
{
    public enum ProvinceChangeOwnerType : byte
    {
        Initialization,
        Test
    }

    public readonly struct RProvinceChangeOwner
    {
        public RProvinceChangeOwner(
            EcsPackedEntity newOwnerCountryPE, EcsPackedEntity newStatePE,
            EcsPackedEntity oldOwnerCountryPE,
            List<EcsPackedEntity> provincePEs,
            ProvinceChangeOwnerType requestType)
        {
            this.newOwnerCountryPE = newOwnerCountryPE;
            this.newStatePE = newStatePE;

            this.oldOwnerCountryPE = oldOwnerCountryPE;

            this.provincePEs = provincePEs;

            this.requestType = requestType;
        }

        public readonly EcsPackedEntity newOwnerCountryPE;
        public readonly EcsPackedEntity newStatePE;

        public readonly EcsPackedEntity oldOwnerCountryPE;

        /// <summary>
        /// Список развёрнут для удобства удаления из списка провинций CState
        /// </summary>
        public readonly List<EcsPackedEntity> provincePEs;

        public readonly ProvinceChangeOwnerType requestType;
    }
}