
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Map.Events.MapArea;
using SO.Map.Events.State;

namespace SO.Map.MapArea
{
    public class SMapAreaControl : IEcsRunSystem
    {
        //Миры
        readonly EcsWorldInject world = default;


        //Карта
        readonly EcsPoolInject<CMapArea> mAPool = default;

        public void Run(IEcsSystems systems)
        {
            //Смена владельцев зон карты
            MapAreasChangeOwner();
        }

        readonly EcsFilterInject<Inc<RMapAreaChangeOwner>> mAChangeOwnerRequestFilter = default;
        readonly EcsPoolInject<RMapAreaChangeOwner> mAChangeOwnerRequestPool = default;
        void MapAreasChangeOwner()
        {
            //Для каждого запроса смены владельца зоны карты
            foreach (int requestEntity in mAChangeOwnerRequestFilter.Value)
            {
                //Берём запрос
                ref RMapAreaChangeOwner requestComp = ref mAChangeOwnerRequestPool.Value.Get(requestEntity);

                //Берём зону
                requestComp.mAPE.Unpack(world.Value, out int mAEntity);
                ref CMapArea mA = ref mAPool.Value.Get(mAEntity);

                //Меняем владельца зоны
                MapAreaChangeOwner(
                    ref mA,
                    ref requestComp);

                mAChangeOwnerRequestPool.Value.Del(requestEntity);
            }
        }

        void MapAreaChangeOwner(
            ref CMapArea mA,
            ref RMapAreaChangeOwner requestComp)
        {
            //Если смена владельца происходит при инициализации
            if (requestComp.requestType == MapAreaChangeOwnerType.Initialization)
            {
                MapAreaChangeOwnerInitialization();
            }

            //Запрашиваем смену владельца всех областей, из которых состоит зона карты
            MapAreaAllStatesChangeOwner(
                ref mA,
                requestComp.newOwnerCountryPE);

            //Поскольку минимальной единицей территории является провинция,
            //событие смены владельца должно вызываться на уровне провинции,
            //чтобы быть наиболее подробным
        }

        void MapAreaChangeOwnerInitialization()
        {

        }

        void MapAreaAllStatesChangeOwner(
            ref CMapArea mA,
            EcsPackedEntity newOwnerCountryPE)
        {
            //Для каждой области в зоне
            for (int a = 0; a < mA.states.Count; a++)
            {
                //Запрашиваем смену владельца области
                StateChangeOwnerRequest(
                    newOwnerCountryPE,
                    mA.states[a].statePE,
                    StateChangeOwnerType.Test);
            }
        }

        readonly EcsPoolInject<RStateChangeOwner> stateChangeOwnerRequestPool = default;
        void StateChangeOwnerRequest(
            EcsPackedEntity targetCountryPE,
            EcsPackedEntity statePE,
            StateChangeOwnerType requestType)
        {
            //Создаём новую сущность и назначаем ей запрос смены владельца области
            int requestEntity = world.Value.NewEntity();
            ref RStateChangeOwner requestComp = ref stateChangeOwnerRequestPool.Value.Add(requestEntity);

            //Заполняем данные запроса
            requestComp = new(
                targetCountryPE,
                statePE,
                requestType);
        }
    }
}