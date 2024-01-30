
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Map;
using SO.Map.RFO.Events;

namespace SO.Map.RFO
{
    public class SRFOInitialization : IEcsRunSystem
    {
        //Миры
        readonly EcsWorldInject world = default;


        //Карта
        readonly EcsPoolInject<CRegion> regionPool = default;

        //Экономика
        readonly EcsPoolInject<CRegionFO> rFOPool = default;


        //События экономики
        readonly EcsFilterInject<Inc<CRegion, SRRFOCreating>> rFOCreatingSelfRequestFilter = default;
        readonly EcsPoolInject<SRRFOCreating> rFOCreatingSelfRequestPool = default;

        public void Run(IEcsSystems systems)
        {
            //Для каждого региона с самозапросом создания RFO
            foreach (int regionEntity in rFOCreatingSelfRequestFilter.Value)
            {
                //Берём регион и самозапрос
                ref CRegion region = ref regionPool.Value.Get(regionEntity);
                ref SRRFOCreating selfRequestComp = ref rFOCreatingSelfRequestPool.Value.Get(regionEntity);

                //Создаём RFO
                RFOCreating(
                    ref region, ref selfRequestComp);

                //Удаляем самозапрос
                rFOCreatingSelfRequestPool.Value.Del(regionEntity);
            }
        }

        void RFOCreating(
            ref CRegion region, ref SRRFOCreating requestComp)
        {
            //Берём сущность региона и назначаем ему компонент RFO
            region.selfPE.Unpack(world.Value, out int regionEntity);
            ref CRegionFO rFO = ref rFOPool.Value.Add(regionEntity);

            //Заполняем основные данные RFO
            rFO = new(
                region.selfPE);
        }
    }
}