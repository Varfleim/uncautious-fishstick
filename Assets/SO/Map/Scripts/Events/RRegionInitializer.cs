
using Leopotam.EcsLite;

namespace SO.Map.Events
{
    public struct RRegionInitializer
    {
        public RRegionInitializer(int a)
        {
            regionPE = new();
        }

        public EcsPackedEntity regionPE;
    }
}