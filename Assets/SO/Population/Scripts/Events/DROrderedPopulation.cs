
using Leopotam.EcsLite;

namespace SO.Population.Events
{
    public readonly struct DROrderedPopulation
    {
        public DROrderedPopulation(
            EcsPackedEntity targetRegionPE,
            int socialClassIndex,
            int population)
        {
            this.targetRegionPE = targetRegionPE;

            this.socialClassIndex = socialClassIndex;

            this.population = population;
        }

        public readonly EcsPackedEntity targetRegionPE;

        public readonly int socialClassIndex;

        public readonly int population;
    }
}