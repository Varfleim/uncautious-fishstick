
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Warfare.Fleet;
using SO.Warfare.Fleet.Missions;
using SO.Warfare.Fleet.Movement.Events;

namespace SO.Warfare.Fleet.Movement
{
    public class STaskForcePathfindingRequestAssign : IEcsRunSystem
    {
        //Миры
        readonly EcsWorldInject world = default;

        //Военное дело
        readonly EcsPoolInject<CTaskForce> tFPool = default;
        
        readonly EcsPoolInject<CTFTargetMission> tFTargetMissionPool = default;

        readonly EcsPoolInject<CTaskForceMovement> taskForceMovementPool = default;

        public void Run(IEcsSystems systems)
        {
            //Проверяем все запросы движения
            TaskForceMovementRequest();

            //Проверяем оперативные группы, имеющие целевую миссию
            TaskForceTargetMissionMovement();
        }

        readonly EcsFilterInject<Inc<RTaskForceMovement>> tFMovementRequestFilter = default;
        readonly EcsPoolInject<RTaskForceMovement> tFMovementRequestPool = default;
        void TaskForceMovementRequest()
        {
            //Для каждого запроса перемещения оперативной группы
            foreach(int requestEntity in tFMovementRequestFilter.Value)
            {
                //Берём запрос
                ref RTaskForceMovement requestComp = ref tFMovementRequestPool.Value.Get(requestEntity);

                //Берём группу
                requestComp.tFPE.Unpack(world.Value, out int tFEntity);
                ref CTaskForce tF = ref tFPool.Value.Get(tFEntity);

                //Если группа не имеет компонента движения
                if(taskForceMovementPool.Value.Has(tFEntity) == false)
                {
                    //Назначаем компонент движения группе
                    TaskForceAssignMovement(
                        tFEntity,
                        requestComp.targetPE, requestComp.targetType);
                }

                tFMovementRequestPool.Value.Del(requestEntity);
            }
        }

        readonly EcsFilterInject<Inc<CTaskForce, CTFTargetMission>, Exc<CTaskForceMovement>> tFTargetMissionFilter = default;
        void TaskForceTargetMissionMovement()
        {
            //Для каждой оперативной группы с целевой миссией, но без компонента движения
            foreach (int tFEntity in tFTargetMissionFilter.Value)
            {
                //Берём группу и целевую миссию
                ref CTaskForce tF = ref tFPool.Value.Get(tFEntity);
                ref CTFTargetMission tFTargetMission = ref tFTargetMissionPool.Value.Get(tFEntity);

                //Если группа находится в режиме движения
                if (tFTargetMission.missionStatus == TaskForceMissionStatus.Movement)
                {
                    //Если группа может предоставить данные цели движения
                    if(TaskForceGetMovementTarget(
                        ref tF, tFEntity,
                        out EcsPackedEntity targetPE, out TaskForceMovementTargetType targetType))
                    {
                        //Назначаем компонент движения группе
                        TaskForceAssignMovement(
                            tFEntity,
                            targetPE, targetType);
                    }
                    //Иначе
                    else
                    {
                        UnityEngine.Debug.LogWarning("Ошибка получения данных для передвижения!");
                    }
                }
            }
        }

        bool TaskForceGetMovementTarget(
            ref CTaskForce tF, int tFEntity,
            out EcsPackedEntity targetPE, out TaskForceMovementTargetType targetType)
        {
            //Переменные по умолчанию пусты
            targetPE = new();
            targetType = TaskForceMovementTargetType.None;

            return false;
        }

        void TaskForceAssignMovement(
            int tFEntity,
            EcsPackedEntity movementTargetPE, TaskForceMovementTargetType movementTargetType)
        {
            //Берём группу
            ref CTaskForce tF = ref tFPool.Value.Get(tFEntity);

            //Назначаем группе компонент движения
            ref CTaskForceMovement tFMovement = ref taskForceMovementPool.Value.Add(tFEntity);

            //Заполняем данные компонента
            tFMovement = new(movementTargetPE, movementTargetType);

            //Если цель движения - это провинция
            if(movementTargetType == TaskForceMovementTargetType.Province)
            {
                //Запрашиваем поиск пути до целевой провинции
                TaskForceFindPathSelfRequest(tFEntity, movementTargetPE);
            }
            //Иначе, если цель движения - оперативная группа
            else if(movementTargetType == TaskForceMovementTargetType.TaskForce)
            {

            }
        }

        readonly EcsPoolInject<SRTaskForceFindPath> tFFindPathSelfRequestPool = default;
        void TaskForceFindPathSelfRequest(
            int tFEntity,
            EcsPackedEntity targetProvincePE)
        {
            //Назначаем сущности оперативной группы самозапрос поиска пути
            ref SRTaskForceFindPath selfRequestComp = ref tFFindPathSelfRequestPool.Value.Add(tFEntity);

            //Заполняем данные запроса
            selfRequestComp = new(targetProvincePE);
        }
    }
}