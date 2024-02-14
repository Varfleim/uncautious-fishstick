
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Warfare.Fleet.Movement.Events;

namespace SO.Warfare.Fleet.Movement
{
    public class STaskForcePathfindingSelfRequestDelete : IEcsRunSystem
    {
        //События военного дела
        readonly EcsFilterInject<Inc<SRTaskForceFindPath>> tFFindPathSelfRequestFilter = default;
        readonly EcsPoolInject<SRTaskForceFindPath> tFFindPathSelfRequestPool = default;

        public void Run(IEcsSystems systems)
        {
            //Для каждого самозапроса поиска пути
            foreach(int selfRequestEntity in tFFindPathSelfRequestFilter.Value)
            {
                //Удаляем запрос с сущности
                tFFindPathSelfRequestPool.Value.Del(selfRequestEntity);
            }
        }
    }
}