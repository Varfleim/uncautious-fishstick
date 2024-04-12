
using Leopotam.EcsLite;

using SO.Population.Events;

namespace SO.Population
{
    public struct CPopulation
    {
        public CPopulation(
            EcsPackedEntity selfPE,
            int socialClassIndex)
        {
            this.selfPE = selfPE;

            this.socialClassIndex = socialClassIndex;

            population = 0;
        }

        public readonly EcsPackedEntity selfPE;

        #region Identification
        #region SocialClass
        public readonly int socialClassIndex;
        #endregion
        #endregion

        #region Population
        public void UpdatePopulation(
            ref DROrderedPopulation orderedPOP)
        {
            //Прибавляем запрошенное количество населения к текущему
            population += orderedPOP.population;

            UnityEngine.Debug.LogWarning(Population);
        }

        public readonly int Population
        {
            get
            {
                return population;
            }
        }
        int population;
        #endregion
    }
}