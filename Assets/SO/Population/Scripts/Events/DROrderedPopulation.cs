
using Leopotam.EcsLite;

namespace SO.Population.Events
{
    public readonly struct DROrderedPopulation
    {
        public DROrderedPopulation(
            EcsPackedEntity targetProvincePE,
            int socialClassIndex,
            int population)
        {
            this.targetProvincePE = targetProvincePE;

            this.socialClassIndex = socialClassIndex;

            this.population = population;
        }

        public readonly EcsPackedEntity targetProvincePE;

        public readonly int socialClassIndex;

        public readonly int population;
    }
}