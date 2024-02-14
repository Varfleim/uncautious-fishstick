
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Map;
using SO.Warfare.Fleet.Missions.Events;
using SO.Warfare.Fleet.Movement;

namespace SO.Warfare.Fleet.Missions
{
    public class STaskForceMissionsControl : IEcsRunSystem
    {
        //Миры
        readonly EcsWorldInject world = default;


        //Карта
        readonly EcsPoolInject<CRegionCore> rCPool = default;

        //Военное дело
        readonly EcsPoolInject<CTaskForce> tFPool = default;
        readonly EcsPoolInject<CTFTargetMission> tFTargetMissionPool = default;

        public void Run(IEcsSystems systems)
        {
            //Меняем миссии оперативных групп
            TaskForceChangeMission();
        }

        readonly EcsFilterInject<Inc<RTaskForceChangeMission>> tFChangeMissionRequestFilter = default;
        readonly EcsPoolInject<RTaskForceChangeMission> tFChangeMissionRequestPool = default;
        void TaskForceChangeMission()
        {
            //Для каждого запроса смены миссии оперативной группы
            foreach (int requestEntity in tFChangeMissionRequestFilter.Value)
            {
                //Берём запрос
                ref RTaskForceChangeMission requestComp = ref tFChangeMissionRequestPool.Value.Get(requestEntity);

                //Берём группу
                requestComp.tFPE.Unpack(world.Value, out int tFEntity);
                ref CTaskForce tF = ref tFPool.Value.Get(tFEntity);

                //Очищаем текущую миссию
                TaskForceClearMission(ref tF);

                //Если запрашивается смена миссии на удержание
                if(requestComp.requestType == TaskForceChangeMissionType.MissionHold)
                {
                    //Меняем миссию группы на удержание
                    TaskForceChangeMissionHold(ref tF);
                }

                tFChangeMissionRequestPool.Value.Del(tFEntity);
            }
        }

        void TaskForceClearMission(
            ref CTaskForce tF)
        {
            //Берём сущность группы
            tF.selfPE.Unpack(world.Value, out int tFEntity);

            //Если у группы есть компонент целевой миссии
            if (tFTargetMissionPool.Value.Has(tFEntity)) 
            {
                //Удаляем его
                tFTargetMissionPool.Value.Del(tFEntity);
            }

            //Очищаем текущую миссию группы
            tF.currentMissionType = TaskForceMissionType.None;
        }

        void TaskForceChangeMissionHold(
            ref CTaskForce tF)
        {
            //Указываем тип текущей миссии группы
            tF.currentMissionType = TaskForceMissionType.Hold;
        }
    }
}