
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Map.Province;
using SO.Warfare.Fleet.Missions.Events;

namespace SO.Warfare.Fleet.Movement
{
    public class STaskForceMovementStop : IEcsRunSystem
    {
        //Миры
        readonly EcsWorldInject world = default;


        //Карта
        readonly EcsPoolInject<CProvinceCore> pCPool = default;

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

                    //Если группа имеет следующую провинцию в маршруте
                    if (tFMovement.pathProvincePEs.Count > 0)
                    {
                        //Берём текущую провинцию группы
                        tF.currentProvincePE.Unpack(world.Value, out int currentProvinceEntity);
                        ref CProvinceCore currentPC = ref pCPool.Value.Get(currentProvinceEntity);

                        //Удаляем группу из списка групп в провинции
                        currentPC.taskForcePEs.Remove(tF.selfPE);

                        //Берём следующую провинцию в маршруте группы
                        tFMovement.pathProvincePEs[tFMovement.pathProvincePEs.Count - 1].Unpack(world.Value, out int nextProvinceEntity);
                        ref CProvinceCore nextPC = ref pCPool.Value.Get(nextProvinceEntity);

                        //Заносим группу в список групп в провинции
                        nextPC.taskForcePEs.Add(tF.selfPE);

                        //Меняем текущую провинцию группы
                        tF.currentProvincePE = nextPC.selfPE;

                        //Удаляем следующую (уже текущую) провинцию из маршрута
                        tFMovement.pathProvincePEs.RemoveAt(tFMovement.pathProvincePEs.Count - 1);

                        //Сохраняем предыдущую провинцию группы
                        tF.previousProvincePE = currentPC.selfPE;

                        //Создаём событие, сообщающее о смене провинции оперативной группой
                        TaskForceChangeProvinceEvent(ref tF);
                    }

                    //Если в маршруте группы больше не осталось провинций
                    if (tFMovement.pathProvincePEs.Count == 0)
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

        readonly EcsPoolInject<ETaskForceChangeProvince> tFChangeProvinceEventPool = default;
        void TaskForceChangeProvinceEvent(
            ref CTaskForce tF)
        {
            //Создаём новую сущность и назначаем ей событие смены провинции оперативной группой
            int eventEntity = world.Value.NewEntity();
            ref ETaskForceChangeProvince eventComp = ref tFChangeProvinceEventPool.Value.Add(eventEntity);

            //Заполняем данные события
            eventComp = new(tF.selfPE);
        }
    }
}