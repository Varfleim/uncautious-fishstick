
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Map.Economy;
using SO.Map.Region;
using SO.Population.Events;

namespace SO.Population
{
    public class SPopulationControl : IEcsRunSystem
    {
        //Миры
        readonly EcsWorldInject world = default;


        //Карта
        readonly EcsFilterInject<Inc<CRegionEconomy>> regionFilter = default;
        readonly EcsPoolInject<CRegionEconomy> rEPool = default;

        //Население
        readonly EcsPoolInject<CPopulation> pOPPool = default;

        public void Run(IEcsSystems systems)
        {
            //Создание групп населения
            PopulationCreating();
        }

        void PopulationCreating()
        {
            //Для каждого региона
            foreach(int currentRegionEntity in regionFilter.Value)
            {
                //Берём регион
                ref CRegionEconomy currentRE = ref rEPool.Value.Get(currentRegionEntity);

                //Пока в регионе есть заказанные группы населения
                for(int a = 0; a < currentRE.orderedPOPs.Count; a++)
                {
                    //Берём заказанный ПОП
                    DROrderedPopulation orderedPOP = currentRE.orderedPOPs[a];

                    //Определяем регион, в котором создаётся ПОП
                    int targetRegionEntity;

                    //Если у ПОПа нет целевого региона
                    if(orderedPOP.targetRegionPE.Unpack(world.Value, out targetRegionEntity) == false)
                    {
                        //ПОП создаётся в текущем регионе

                        //Указываем в качестве целевого текущий регион
                        targetRegionEntity = currentRegionEntity;
                    }
                    //Иначе ПОП создаётся в целевом регионе

                    //Берём целевой регион
                    ref CRegionEconomy targetRE = ref rEPool.Value.Get(targetRegionEntity);

                    //Если в регионе уже есть идентичный ПОП
                    if(FindIdenticalPopulation(
                        ref targetRE,
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
                        //Создаём новый ПОП в регионе
                        PopulationCreateNew(
                            ref orderedPOP,
                            ref targetRE);
                    }
                }
                currentRE.orderedPOPs.Clear();
            }
        }

        bool FindIdenticalPopulation(
            ref CRegionEconomy rE,
            ref DROrderedPopulation orderedPOP,
            out int oldPOPEntity)
        {
            //Задаём несуществующий индекс
            oldPOPEntity = -1;

            //Для каждой группы населения в регионе
            for(int a = 0; a < rE.pOPs.Count; a++)
            {
                //Берём ПОП
                rE.pOPs[a].Unpack(world.Value, out oldPOPEntity);
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
            ref CRegionEconomy rE)
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

            //Заносим PE ПОПа в список региона
            rE.pOPs.Add(pOP.selfPE);

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