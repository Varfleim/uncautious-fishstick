
using Leopotam.EcsLite;

namespace SO.Map.Generation
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