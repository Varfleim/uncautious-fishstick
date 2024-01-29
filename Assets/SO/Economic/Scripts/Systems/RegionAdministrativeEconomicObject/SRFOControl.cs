
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Faction;
using SO.Economy.RFO.Events;

namespace SO.Economy.RFO
{
    public class SRFOControl : IEcsRunSystem
    {
        //Миры
        readonly EcsWorldInject world = default;


        //Фракции
        readonly EcsPoolInject<CFaction> factionPool = default;

        //Экономика
        readonly EcsPoolInject<CRegionFO> rFOPool = default;


        //События экономики
        readonly EcsFilterInject<Inc<RRFOChangeOwner>> rFOChangeOwnerRequestFilter = default;
        readonly EcsPoolInject<RRFOChangeOwner> rFOChangeOwnerRequestPool = default;

        public void Run(IEcsSystems systems)
        {
            //Смена владельцев RFO
            RFOChangeOwners();
        }

        void RFOChangeOwners()
        {
            //Для каждого запроса смены владельца RFO
            foreach(int requestEntity in rFOChangeOwnerRequestFilter.Value)
            {
                //Берём запрос
                ref RRFOChangeOwner requestComp = ref rFOChangeOwnerRequestPool.Value.Get(requestEntity);

                //Берём фракцию, которая становится владельцем RFO
                requestComp.factionPE.Unpack(world.Value, out int factionEntity);
                ref CFaction faction = ref factionPool.Value.Get(factionEntity);

                //Берём RFO
                requestComp.regionPE.Unpack(world.Value, out int regionEntity);
                ref CRegionFO rFO = ref rFOPool.Value.Get(regionEntity);

                //Если смена владельца происходит при инициализации
                if(requestComp.actionType == RegionChangeOwnerType.Initialization)
                {
                    RFOChangeOwnerInitialization();
                }

                //Указываем фракцию-владельца RFO
                rFO.ownerFactionPE = faction.selfPE;

                //Заносим PE RFO в список фракции
                faction.ownedRFOPEs.Add(rFO.selfPE);

                rFOChangeOwnerRequestPool.Value.Del(requestEntity);
            }
        }

        void RFOChangeOwnerInitialization()
        {

        }
    }
}