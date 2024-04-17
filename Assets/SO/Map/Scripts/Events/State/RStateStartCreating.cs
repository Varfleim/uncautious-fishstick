
using Leopotam.EcsLite;

namespace SO.Map.Events.State
{
    public readonly struct RStateStartCreating
    {
        public RStateStartCreating(
            EcsPackedEntity parentMAPE)
        {
            this.parentMAPE = parentMAPE;
        }

        public readonly EcsPackedEntity parentMAPE;
    }
}