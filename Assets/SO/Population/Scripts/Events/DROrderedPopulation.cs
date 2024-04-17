
using Leopotam.EcsLite;

namespace SO.Population.Events
{
    public readonly struct DROrderedPopulation
    {
        public DROrderedPopulation(
            EcsPackedEntity targetStatePE,
            int socialClassIndex,
            int population)
        {
            this.targetStatePE = targetStatePE;

            this.socialClassIndex = socialClassIndex;

            this.population = population;
        }

        public readonly EcsPackedEntity targetStatePE;

        public readonly int socialClassIndex;

        public readonly int population;
    }
}