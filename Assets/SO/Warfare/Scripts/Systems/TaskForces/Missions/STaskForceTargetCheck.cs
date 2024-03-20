
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Map.Region;
using SO.Warfare.Fleet.Missions.Events;

namespace SO.Warfare.Fleet.Missions
{
    public class STaskForceTargetCheck : IEcsRunSystem
    {
        //Миры
        //readonly EcsWorldInject world = default;


        //Военное дело
        readonly EcsPoolInject<CTaskForce> tFPool = default;

        readonly EcsPoolInject<CTFTargetMission> tFTargetMissionPool = default;


        //События военного дела
        readonly EcsPoolInject<SRTaskForceTargetCheck> tFTargetCheckSelfRequestPool = default;

        public void Run(IEcsSystems systems)
        {
            //Проверяем оперативные группы с целевыми миссиями
            TaskForceTargetMissionTargetCheck();
        }

        readonly EcsFilterInject<Inc<CTaskForce, CTFTargetMission, SRTaskForceTargetCheck>> tFTargetMissionTargetCheckSelfRequestFilter = default;
        void TaskForceTargetMissionTargetCheck()
        {
            //Для каждой оперативной группы с целевой миссией и самозапросом проверки цели
            foreach(int tFEntity in tFTargetMissionTargetCheckSelfRequestFilter.Value)
            {
                //Берём группу, целевую миссию и компонент движения
                ref CTaskForce tF = ref tFPool.Value.Get(tFEntity);
                ref CTFTargetMission tFTargetMission = ref tFTargetMissionPool.Value.Get(tFEntity);

                //Если группа находится в режиме движения
                if(tFTargetMission.missionStatus == TaskForceMissionStatus.Movement)
                {

                }

                tFTargetCheckSelfRequestPool.Value.Del(tFEntity);
            }
        }
    }
}