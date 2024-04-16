
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Map.Economy;
using SO.Map.Province;
using SO.Population.Events;

namespace SO.Population
{
    public class SPopulationControl : IEcsRunSystem
    {
        //Миры
        readonly EcsWorldInject world = default;


        //Карта
        readonly EcsFilterInject<Inc<CProvinceEconomy>> provinceFilter = default;
        readonly EcsPoolInject<CProvinceEconomy> pEPool = default;

        //Население
        readonly EcsPoolInject<CPopulation> pOPPool = default;

        public void Run(IEcsSystems systems)
        {
            //Создание групп населения
            PopulationCreating();
        }

        void PopulationCreating()
        {
            //Для каждой провинции
            foreach (int currentProvinceEntity in provinceFilter.Value)
            {
                //Берём провинцию
                ref CProvinceEconomy currentPE = ref pEPool.Value.Get(currentProvinceEntity);

                //Пока в провинции есть заказанные группы населения
                for (int a = 0; a < currentPE.orderedPOPs.Count; a++)
                {
                    //Берём заказанный ПОП
                    DROrderedPopulation orderedPOP = currentPE.orderedPOPs[a];

                    //Определяем провинцию, в котором создаётся ПОП
                    int targetProvinceEntity;

                    //Если у ПОПа нет целевой провинции 
                    if (orderedPOP.targetProvincePE.Unpack(world.Value, out targetProvinceEntity) == false)
                    {
                        //ПОП создаётся в текущей провинции

                        //Указываем в качестве целевой текущую
                        targetProvinceEntity = currentProvinceEntity;
                    }
                    //Иначе ПОП создаётся в целевой провинции

                    //Берём целевую провинцию
                    ref CProvinceEconomy targetPE = ref pEPool.Value.Get(targetProvinceEntity);

                    //Если в провинции уже есть идентичный ПОП
                    if (FindIdenticalPopulation(
                        ref targetPE,
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
                        //Создаём новый ПОП в провинции
                        PopulationCreateNew(
                            ref orderedPOP,
                            ref targetPE);
                    }
                }
                currentPE.orderedPOPs.Clear();
            }
        }

        bool FindIdenticalPopulation(
            ref CProvinceEconomy pE,
            ref DROrderedPopulation orderedPOP,
            out int oldPOPEntity)
        {
            //Задаём несуществующий индекс
            oldPOPEntity = -1;

            //Для каждой группы населения в провинции
            for (int a = 0; a < pE.pOPs.Count; a++)
            {
                //Берём ПОП
                pE.pOPs[a].Unpack(world.Value, out oldPOPEntity);
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
            ref CProvinceEconomy pE)
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

            //Заносим PE ПОПа в список провинции
            pE.pOPs.Add(pOP.selfPE);

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