
using Leopotam.EcsLite;

namespace SO.Map.Generation
{
    public struct RProvinceInitializer
    {
        public RProvinceInitializer(int a)
        {
            provincePE = new();
        }

        public EcsPackedEntity provincePE;
    }
}