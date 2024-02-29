
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Map;
using SO.Character;
using SO.Warfare.Fleet.Events;

namespace SO.Warfare.Fleet
{
    public class STaskForceControl : IEcsRunSystem
    {
        //Миры
        readonly EcsWorldInject world = default;


        //Карта
        readonly EcsPoolInject<CRegionCore> rCPool = default;

        //Персонажи
        readonly EcsPoolInject<CCharacter> characterPool = default;


        //Военное дело
        readonly EcsPoolInject<CTaskForce> tFPool = default;

        public void Run(IEcsSystems systems)
        {
            //Создаём оперативные группы
            TaskForceCreating();
        }

        readonly EcsFilterInject<Inc<RTaskForceCreating>> tFCreatingRequestFilter = default;
        readonly EcsPoolInject<RTaskForceCreating> tFCreatingRequestPool = default;
        void TaskForceCreating()
        {
            //Для каждого запроса создания оперативной группы
            foreach(int requestEntity in tFCreatingRequestFilter.Value)
            {
                //Берём запрос
                ref RTaskForceCreating requestComp = ref tFCreatingRequestPool.Value.Get(requestEntity);

                //Берём персонажа-владельца оперативной группы
                requestComp.ownerCharacterPE.Unpack(world.Value, out int characterEntity);
                ref CCharacter character = ref characterPool.Value.Get(characterEntity);

                //Создаём новую сущность и назначаем ей компонент оперативной группы
                int taskForceEntity = world.Value.NewEntity();
                ref CTaskForce tF = ref tFPool.Value.Add(taskForceEntity);

                //Заполняем основные данные группы
                tF = new(
                    world.Value.PackEntity(taskForceEntity),
                    character.selfPE);

                //ТЕСТ
                //Заносим PE группы в список групп персонажа
                character.ownedTaskForces.Add(tF.selfPE);

                //Берём стартовый регион персонажа
                character.ownedRCPEs[0].Unpack(world.Value, out int regionEntity);
                ref CRegionCore rC = ref rCPool.Value.Get(regionEntity);

                //Размещаем оперативную группу в стартовом регионе персонажа
                tF.currentRegionPE = rC.selfPE;

                //Заносим группу в список групп в регионе
                rC.taskForcePEs.Add(tF.selfPE);

                UnityEngine.Debug.LogWarning(rC.Index);
                //ТЕСТ

                //Создаём событие, сообщающее о создании новой оперативной группы
                ObjectNewCreatedEvent(tF.selfPE, ObjectNewCreatedType.TaskForce);

                tFCreatingRequestPool.Value.Del(requestEntity);
            }
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