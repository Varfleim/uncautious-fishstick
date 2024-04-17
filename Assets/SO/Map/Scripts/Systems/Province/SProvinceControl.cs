
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Map.State;
using SO.Map.Events;
using SO.Map.Events.Province;

namespace SO.Map.Province
{
    public class SProvinceControl : IEcsRunSystem
    {
        //Миры
        readonly EcsWorldInject world = default;


        //Карта
        readonly EcsPoolInject<CState> statePool = default;

        readonly EcsPoolInject<CProvinceCore> pCPool = default;

        public void Run(IEcsSystems systems)
        {
            //Смена владельцев провинций
            ProvincesChangeOwner();
        }

        readonly EcsFilterInject<Inc<RProvinceChangeOwner>> provinceChangeOwnerRequestFilter = default;
        readonly EcsPoolInject<RProvinceChangeOwner> provinceChangeOwnerRequestPool = default;
        void ProvincesChangeOwner()
        {
            //Для каждого запроса смены владельца провинций
            foreach (int requestEntity in provinceChangeOwnerRequestFilter.Value)
            {
                //Берём запрос
                ref RProvinceChangeOwner requestComp = ref provinceChangeOwnerRequestPool.Value.Get(requestEntity);

                //Для каждой провинции в запросе
                for (int a = 0; a < requestComp.provincePEs.Count; a++)
                {
                    //Берём провинцию
                    requestComp.provincePEs[a].Unpack(world.Value, out int provinceEntity);
                    ref CProvinceCore pC = ref pCPool.Value.Get(provinceEntity);

                    //Меняем владельца провинции
                    ProvinceChangeOwner(
                        ref pC,
                        ref requestComp);
                }

                provinceChangeOwnerRequestPool.Value.Del(requestEntity);
            }
        }

        void ProvinceChangeOwner(
            ref CProvinceCore pC,
            ref RProvinceChangeOwner requestComp)
        {
            //Если смена владельца происходит при инициализации
            if (requestComp.requestType == ProvinceChangeOwnerType.Initialization)
            {
                ProvinceChangeOwnerInitialization();
            }
            //Иначе 
            else
            {
                //Берём исходную область
                pC.ParentStatePE.Unpack(world.Value, out int oldStateEntity);
                ref CState oldState = ref statePool.Value.Get(oldStateEntity);

                //Удаляем провинцию из списка исходной области
                oldState.RemoveProvinceFromList(pC.selfPE);
            }

            //Указываем новую область, к которой относится провинция
            pC.SetParentState(requestComp.newStatePE);


            //Берём целевую область
            requestComp.newStatePE.Unpack(world.Value, out int newStateEntity);
            ref CState newState = ref statePool.Value.Get(newStateEntity);

            //Заносим провинцию в список целевой области
            newState.provincePEs.Add(pC.selfPE);


            //Создаём событие, сообщающее о смене владельца провинции
            ProvinceChangeOwnerEvent(
                pC.selfPE,
                requestComp.newOwnerCountryPE, requestComp.oldOwnerCountryPE);
        }

        void ProvinceChangeOwnerInitialization()
        {

        }

        readonly EcsPoolInject<EProvinceChangeOwner> provinceChangeOwnerEventPool = default;
        void ProvinceChangeOwnerEvent(
            EcsPackedEntity provincePE,
            EcsPackedEntity newOwnerCountryPE, EcsPackedEntity oldOwnerCountryPE = new())
        {
            //Создаём новую сущность и назначаем ей событие смены владельца PC
            int eventEntity = world.Value.NewEntity();
            ref EProvinceChangeOwner eventComp = ref provinceChangeOwnerEventPool.Value.Add(eventEntity);

            //Заполняем данные события
            eventComp = new(
                provincePE,
                newOwnerCountryPE, oldOwnerCountryPE);
        }
    }
}