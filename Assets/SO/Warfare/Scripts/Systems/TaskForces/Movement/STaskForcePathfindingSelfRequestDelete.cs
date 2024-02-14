
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Warfare.Fleet.Movement.Events;

namespace SO.Warfare.Fleet.Movement
{
    public class STaskForcePathfindingSelfRequestDelete : IEcsRunSystem
    {
        //������� �������� ����
        readonly EcsFilterInject<Inc<SRTaskForceFindPath>> tFFindPathSelfRequestFilter = default;
        readonly EcsPoolInject<SRTaskForceFindPath> tFFindPathSelfRequestPool = default;

        public void Run(IEcsSystems systems)
        {
            //��� ������� ����������� ������ ����
            foreach(int selfRequestEntity in tFFindPathSelfRequestFilter.Value)
            {
                //������� ������ � ��������
                tFFindPathSelfRequestPool.Value.Del(selfRequestEntity);
            }
        }
    }
}