
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Map;
using SO.Warfare.Fleet.Missions.Events;

namespace SO.Warfare.Fleet.Movement
{
    public class STaskForceMovementStop : IEcsRunSystem
    {
        //Миры
        readonly EcsWorldInject world = default;


        //Карта
        readonly EcsPoolInject<CRegionCore> rCPool = default;

        //Военное дело
        readonly EcsPoolInject<CTaskForce> tFPool = default;

        readonly EcsFilterInject<Inc<CTaskForce, CTaskForceMovement>> tFMovementFilter = default;
        readonly EcsPoolInject<CTaskForceMovement> tFMovementPool = default;


        //События военного дела
        readonly EcsPoolInject<SRTaskForceTargetCheck> tFTargetCheckSelfRequestPool = default;

        public void Run(IEcsSystems systems)
        {
            //Проверяем оперативные группы, которые могли закончить движение
            TaskForceMovementStopCheck();
        }

        void TaskForceMovementStopCheck()
        {
            //Для каждой оперативной группы с компонентом движения
            foreach(int tFEntity in tFMovementFilter.Value)
            {
                //Берём компонент движения
                ref CTaskForceMovement tFMovement = ref tFMovementPool.Value.Get(tFEntity);

                //Если группа прекратила движение
                if(tFMovement.isTraveled == true)
                {
                    //Отмечаем, что группа может продолжить движение
                    tFMovement.isTraveled = false;

                    //Берём группу
                    ref CTaskForce tF = ref tFPool.Value.Get(tFEntity);

                    //Если группа имеет следующий регион в маршруте
                    if (tFMovement.pathRegionPEs.Count > 0)
                    {
                        //Берём текущий регион группы
                        tF.currentRegionPE.Unpack(world.Value, out int currentRegionEntity);
                        ref CRegionCore currentRC = ref rCPool.Value.Get(currentRegionEntity);

                        //Удаляем группу из списка групп в регионе
                        currentRC.taskForcePEs.Remove(tF.selfPE);

                        //Берём следующий регион в маршруте группы
                        tFMovement.pathRegionPEs[tFMovement.pathRegionPEs.Count - 1].Unpack(world.Value, out int nextRegionEntity);
                        ref CRegionCore nextRC = ref rCPool.Value.Get(nextRegionEntity);

                        //Заносим группу в список групп в регионе
                        nextRC.taskForcePEs.Add(tF.selfPE);

                        //Меняем текущий регион группы
                        tF.currentRegionPE = nextRC.selfPE;

                        //Удаляем следующий (уже текущий) регион из маршрута
                        tFMovement.pathRegionPEs.RemoveAt(tFMovement.pathRegionPEs.Count - 1);

                        //Сохраняем предыдущий регион группы
                        tF.previousRegionPE = currentRC.selfPE;

                        //Создаём событие, сообщающее о смене региона оперативной группой
                        TaskForceChangeRegionEvent(ref tF);
                    }

                    //Если в маршруте группы больше не осталось регионов
                    if(tFMovement.pathRegionPEs.Count == 0)
                    {
                        //Запрашиваем проверку цели
                        //TaskForceTargetCheckSelfRequest(tFEntity);

                        //Удаляем компонент движения
                        tFMovementPool.Value.Del(tFEntity);

                        //Если группа имеет компонент движения к движущейся цели, удаляем и его

                    }
                }
            }
        }

        void TaskForceTargetCheckSelfRequest(
            int tFEntity)
        {
            //Назначаем оперативной группе самозапрос проверки цели
            ref SRTaskForceTargetCheck selfRequestComp = ref tFTargetCheckSelfRequestPool.Value.Add(tFEntity);

            //Заполняем данные самозапроса
            selfRequestComp = new(0);
        }

        readonly EcsPoolInject<ETaskForceChangeRegion> tFChangeOwnerEventPool = default;
        void TaskForceChangeRegionEvent(
            ref CTaskForce tF)
        {
            //Создаём новую сущность и назначаем ей событие смены региона оперативной группой
            int eventEntity = world.Value.NewEntity();
            ref ETaskForceChangeRegion eventComp = ref tFChangeOwnerEventPool.Value.Add(eventEntity);

            //Заполняем данные события
            eventComp = new(tF.selfPE);
        }
    }
}