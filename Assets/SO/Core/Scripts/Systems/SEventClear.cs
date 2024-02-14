
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Map;
using SO.Warfare.Fleet.Movement;

namespace SO
{
    public class SEventClear : IEcsRunSystem
    {
        //������� �����
        readonly EcsFilterInject<Inc<ERegionCoreChangeOwner>> rFOChangeOwnerEventFilter = default;
        readonly EcsPoolInject<ERegionCoreChangeOwner> rFOChangeOwnerEventPool = default;

        //������� �������� ����
        readonly EcsFilterInject<Inc<ETaskForceChangeRegion>> tFChangeRegionEventFilter = default;
        readonly EcsPoolInject<ETaskForceChangeRegion> tFChangeRegionEventPool = default;

        //����� �������
        readonly EcsFilterInject<Inc<EObjectNewCreated>> objectNewCreatedEventFilter = default;
        readonly EcsPoolInject<EObjectNewCreated> objectNewCreatedEventPool = default;

        public void Run(IEcsSystems systems)
        {
            //��� ������� ������� �������� ������ �������
            foreach(int eventEntity in objectNewCreatedEventFilter.Value)
            {
                //������� ��������� �������
                objectNewCreatedEventPool.Value.Del(eventEntity);
            }

            //��� ������� ������� ����� ��������� RFO
            foreach (int eventEntity in rFOChangeOwnerEventFilter.Value)
            {
                //������� ��������� �������
                rFOChangeOwnerEventPool.Value.Del(eventEntity);
            }

            //��� ������� ������� ����� ������� ����������� �������
            foreach(int eventEntity in tFChangeRegionEventFilter.Value)
            {
                //������� ��������� �������
                tFChangeRegionEventPool.Value.Del(eventEntity);
            }
        }
    }
}