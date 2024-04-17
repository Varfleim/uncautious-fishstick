
using System.Collections.Generic;

using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Map.MapArea;
using SO.Map.Events.State;
using SO.Map.Events.Province;
using SO.Country;

namespace SO.Map.State
{
    public class SStateControl : IEcsRunSystem
    {
        //Миры
        readonly EcsWorldInject world = default;


        //Карта
        readonly EcsPoolInject<CMapArea> mAPool = default;

        readonly EcsPoolInject<CState> statePool = default;

        //Страны
        readonly EcsPoolInject<CCountry> countryPool = default;

        public void Run(IEcsSystems systems)
        {
            //Создание ничейных областей на старте
            StatesStartCreating();

            //Смена владельцев областей
            StatesChangeOwner();
        }

        readonly EcsFilterInject<Inc<RStateStartCreating>> stateStartCreatingRequestFilter = default;
        readonly EcsPoolInject<RStateStartCreating> stateStartCreatingRequestPool = default;
        void StatesStartCreating()
        {
            //Ддя каждого запроса стартового создания области
            foreach (int requestEntity in stateStartCreatingRequestFilter.Value)
            {
                //Берём запрос
                ref RStateStartCreating requestComp = ref stateStartCreatingRequestPool.Value.Get(requestEntity);

                //Берём родительскую зону карты
                requestComp.parentMAPE.Unpack(world.Value, out int mAEntity);
                ref CMapArea mA = ref mAPool.Value.Get(mAEntity);

                //Создаём новую область
                StateCreating(
                    ref mA, 
                    out int stateEntity);

                //Берём область
                ref CState state = ref statePool.Value.Get(stateEntity);

                //Добавляем в область все провинции
                StateAllProvinceAdd(
                    ref state,
                    ref mA);

                //ТЕСТ
                //Создаём тестовые группы населения в области
                for (int a = 0; a < 5; a++)
                {
                    //Создаём новый запрос создания ПОПа
                    Population.Events.DROrderedPopulation orderedPOP = new(
                        state.selfPE,
                        0,
                        100);

                    //Заносим его в список заказанных ПОПов
                    state.orderedPOPs.Add(orderedPOP);
                }
                //ТЕСТ

                stateStartCreatingRequestPool.Value.Del(requestEntity);
            }
        }

        readonly EcsFilterInject<Inc<RStateChangeOwner>> stateChangeOwnerRequestFilter = default;
        readonly EcsPoolInject<RStateChangeOwner> stateChangeOwnerRequestPool = default;
        void StatesChangeOwner()
        {
            //Для каждого запроса смены владельца области
            foreach(int requestEntity in stateChangeOwnerRequestFilter.Value)
            {
                //Берём запрос
                ref RStateChangeOwner requestComp = ref stateChangeOwnerRequestPool.Value.Get(requestEntity);

                //Берём область
                requestComp.statePE.Unpack(world.Value, out int stateEntity);
                ref CState state = ref statePool.Value.Get(stateEntity);

                //Меняем владельца области
                StateChangeOwner(
                    ref state,
                    ref requestComp);

                stateChangeOwnerRequestPool.Value.Del(requestEntity);
            }
        }

        void StateCreating(
            ref CMapArea mA,
            out int stateEntity,
            EcsPackedEntity ownerCountryPE = new())
        {
            //Создаём новую сущность и назначаем ей компонент области
            stateEntity = world.Value.NewEntity();
            ref CState state = ref statePool.Value.Add(stateEntity);

            //Заполняем основные данные области
            state = new(
                world.Value.PackEntity(stateEntity),
                mA.selfPE,
                ownerCountryPE);

            //Создаём новую структуру для хранения данных области в зоне карты
            DState stateData = new(
                state.selfPE,
                ownerCountryPE);

            //Заносим её в список в родительской зоне
            mA.states.Add(stateData);

            //Если PE владельца не пуста
            if(ownerCountryPE.Unpack(world.Value, out int countryEntity))
            {
                //Берём страну-владельца
                ref CCountry country = ref countryPool.Value.Get(countryEntity);

                //Заносим область в список областей страны
                country.ownedStatePEs.Add(state.selfPE);
            }

            //Создаём событие, сообщающее о создании новой области
            ObjectNewCreatedEvent(state.selfPE, ObjectNewCreatedType.State);
        }

        void StateChangeOwner(
            ref CState oldState,
            ref RStateChangeOwner requestComp)
        {
            //Если смена владельца происходит при инициализации
            if(requestComp.requestType == StateChangeOwnerType.Initialization)
            {
                StateChangeOwnerInitialization();
            }

            //Берём родительскую зону карты
            oldState.parentMAPE.Unpack(world.Value, out int mAEntity);
            ref CMapArea mA = ref mAPool.Value.Get(mAEntity);

            //Определяем, существует ли уже в зоне область, принадлежащая целевой стране
            bool isNewStateExist = false;

            //Переменная для сущности целевой области
            int newStateEntity = -1;

            //Для каждой области в зоне
            for(int a = 0; a < mA.states.Count; a++)
            {
                //Если область принадлежит целевой стране
                if (mA.states[a].ownerCountryPE.EqualsTo(requestComp.newOwnerCountryPE))
                {
                    //Берём сущность целевой области
                    mA.states[a].statePE.Unpack(world.Value, out newStateEntity);

                    //Указываем, что зона существует
                    isNewStateExist = true;

                    break;
                }
            }

            //Если целевой области не существует
            if(isNewStateExist == false)
            {
                //Создаём новую область
                StateCreating(
                    ref mA,
                    out newStateEntity,
                    requestComp.newOwnerCountryPE);
            }

            //Берём целевую область
            ref CState newState = ref statePool.Value.Get(newStateEntity);


            //Запрашиваем смену владельца всех провинций, из которых состоит исходная область
            StateAllProvincesChangeOwner(
                ref oldState, ref newState);

            //Поскольку минимальной единицей территории является провинция,
            //событие смены владельца должно вызываться на уровне провинции,
            //чтобы быть наиболее подробным
        }

        void StateChangeOwnerInitialization()
        {

        }

        void StateAllProvincesChangeOwner(
            ref CState oldState, ref CState newState)
        {
            //Создаём список провинций для переноса
            List<EcsPackedEntity> provincePEs = new();

            //Для каждой провинции в исходной области в обратном порядке
            for(int a = oldState.provincePEs.Count - 1; a >= 0; a--)
            {
                //Заносим провинцию в список переносимых
                provincePEs.Add(oldState.provincePEs[a]);
            }

            //Запрашиваем смену владельца провинций
            ProvinceChangeOwnerRequest(
                newState.ownerCountryPE, newState.selfPE,
                oldState.ownerCountryPE,
                provincePEs,
                ProvinceChangeOwnerType.Test);
        }

        /// <summary>
        /// Вызываться должно только при начале игры, когда только создаются ничейные области
        /// </summary>
        /// <param name="state"></param>
        /// <param name="parentMAPE"></param>
        void StateAllProvinceAdd(
            ref CState state,
            ref CMapArea mA)
        {
            //Создаём список провинций
            List<EcsPackedEntity> provincePEs = new();

            //Для каждой провинции в зоне
            for(int a = 0; a < mA.provincePEs.Length; a++)
            {
                //Заносим провинцию в список переносимых
                provincePEs.Add(mA.provincePEs[a]);
            }

            //Запрашиваем смену владельца провинций
            ProvinceChangeOwnerRequest(
                state.selfPE,
                provincePEs,
                ProvinceChangeOwnerType.Initialization);
        }



        readonly EcsPoolInject<RProvinceChangeOwner> provinceChangeOwnerRequestPool = default;
        void ProvinceChangeOwnerRequest(
            EcsPackedEntity newOwnerCountryPE, EcsPackedEntity statePE,
            EcsPackedEntity oldOwnerCountryPE,
            List<EcsPackedEntity> provincePEs,
            ProvinceChangeOwnerType requestType)
        {
            //Создаём новую сущность и назначаем ей запрос смены владельца провинций
            int requestEntity = world.Value.NewEntity();
            ref RProvinceChangeOwner requestComp = ref provinceChangeOwnerRequestPool.Value.Add(requestEntity);

            //Заполняем данные запроса
            requestComp = new(
                newOwnerCountryPE, statePE,
                oldOwnerCountryPE,
                provincePEs,
                requestType);
        }

        void ProvinceChangeOwnerRequest(
            EcsPackedEntity statePE,
            List<EcsPackedEntity> provincePEs,
            ProvinceChangeOwnerType requestType)
        {
            ProvinceChangeOwnerRequest(
                new(), statePE,
                new(),
                provincePEs,
                requestType);
        }

        readonly EcsPoolInject<EObjectNewCreated> objectNewCreatedEventPool = default;
        void ObjectNewCreatedEvent(
            EcsPackedEntity objectPE, ObjectNewCreatedType objectType)
        {
            //Создаём новую сущность и назначаем ей событие создания нового объекта
            int eventEntity = world.Value.NewEntity();
            ref EObjectNewCreated eventComp = ref objectNewCreatedEventPool.Value.Add(eventEntity);

            //Заполняем данные события
            eventComp = new(objectPE, objectType);
        }
    }
}