
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Map.State;
using SO.Population.Events;

namespace SO.Population
{
    public class SPopulationControl : IEcsRunSystem
    {
        //Миры
        readonly EcsWorldInject world = default;


        //Карта
        readonly EcsFilterInject<Inc<CState>> stateFilter = default;
        readonly EcsPoolInject<CState> statePool = default;

        //Население
        readonly EcsPoolInject<CPopulation> pOPPool = default;

        public void Run(IEcsSystems systems)
        {
            //Создание групп населения
            PopulationCreating();
        }

        void PopulationCreating()
        {
            //Для каждой области
            foreach (int currentStateEntity in stateFilter.Value)
            {
                //Берём область
                ref CState currentState = ref statePool.Value.Get(currentStateEntity);

                //Пока в области есть заказанные группы населения
                for (int a = 0; a < currentState.orderedPOPs.Count; a++)
                {
                    //Берём заказанный ПОП
                    DROrderedPopulation orderedPOP = currentState.orderedPOPs[a];

                    //Определяем область, в которой создаётся ПОП
                    int targetStateEntity;

                    //Если у ПОПа нет целевой области 
                    if (orderedPOP.targetStatePE.Unpack(world.Value, out targetStateEntity) == false)
                    {
                        //ПОП создаётся в текущей области

                        //Указываем в качестве целевой текущую
                        targetStateEntity = currentStateEntity;
                    }
                    //Иначе ПОП создаётся в целевой области

                    //Берём целевую область
                    ref CState targetState = ref statePool.Value.Get(targetStateEntity);

                    //Если в область уже есть идентичный ПОП
                    if (FindIdenticalPopulation(
                        ref targetState,
                        ref orderedPOP,
                        out int oldPOPEntity))
                    {
                        //Берём его
                        ref CPopulation oldPOP = ref pOPPool.Value.Get(oldPOPEntity);

                        //Добавляем в него население
                        PopulationAddToOld(
                            ref orderedPOP,
                            ref oldPOP);
                    }
                    //Иначе
                    else
                    {
                        //Создаём новый ПОП в области
                        PopulationCreateNew(
                            ref orderedPOP,
                            ref targetState);
                    }
                }
                currentState.orderedPOPs.Clear();
            }
        }

        bool FindIdenticalPopulation(
            ref CState state,
            ref DROrderedPopulation orderedPOP,
            out int oldPOPEntity)
        {
            //Задаём несуществующий индекс
            oldPOPEntity = -1;

            //Для каждой группы населения в области
            for (int a = 0; a < state.pOPPEs.Count; a++)
            {
                //Берём ПОП
                state.pOPPEs[a].Unpack(world.Value, out oldPOPEntity);
                ref CPopulation oldPOP = ref pOPPool.Value.Get(oldPOPEntity);

                //Если существующий ПОП идентичен проверяемому
                if(PopulationFunctions.IsPopulationIdentical(
                    ref orderedPOP, 
                    ref oldPOP))
                {
                    //Возвращаем, что это идентичный ПОП
                    return true;
                }
            }

            //Возвращаем, что идентичного ПОПа нет
            return false;
        }

        void PopulationCreateNew(
            ref DROrderedPopulation orderedPOP,
            ref CState state)
        {
            //Создаём новую сущность и назначаем ей компонент ПОПа
            int pOPEntity = world.Value.NewEntity();
            ref CPopulation pOP = ref pOPPool.Value.Add(pOPEntity);

            //Заполняем основные данные ПОПа
            pOP = new(
                world.Value.PackEntity(pOPEntity),
                orderedPOP.socialClassIndex);

            //Добавляем ПОПу его население
            pOP.UpdatePopulation(ref orderedPOP);

            //Заносим PE ПОПа в список области
            state.pOPPEs.Add(pOP.selfPE);

            //Создаём событие, сообщающее о создании нового ПОПа
            ObjectNewCreatedEvent(pOP.selfPE, ObjectNewCreatedType.Population);
        }

        void PopulationAddToOld(
            ref DROrderedPopulation orderedPOP,
            ref CPopulation oldPOP)
        {
            //Добавляем ПОПу новое население
            oldPOP.UpdatePopulation(ref orderedPOP);
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