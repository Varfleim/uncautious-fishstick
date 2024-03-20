
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Map.Events;
using SO.Warfare.Fleet.Movement;

namespace SO
{
    public class SEventClear : IEcsRunSystem
    {
        //������� �����
        readonly EcsFilterInject<Inc<ERegionChangeOwner>> regionChangeOwnerEventFilter = default;
        readonly EcsPoolInject<ERegionChangeOwner> regionChangeOwnerEventPool = default;

        readonly EcsFilterInject<Inc<EStrategicAreaChangeOwner>> sAChangeOwnerEventFilter = default;
        readonly EcsPoolInject<EStrategicAreaChangeOwner> sAChangeOwnerEventPool = default;

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

            //��� ������� ������� ����� ��������� �������
            foreach (int eventEntity in regionChangeOwnerEventFilter.Value)
            {
                //������� ��������� �������
                regionChangeOwnerEventPool.Value.Del(eventEntity);
            }

            //��� ������� ������� ����� ��������� �������������� �������
            foreach (int eventEntity in sAChangeOwnerEventFilter.Value)
            {
                //������� ��������� �������
                sAChangeOwnerEventPool.Value.Del(eventEntity);
            }

            //��� ������� ������� ����� ������� ����������� �������
            foreach (int eventEntity in tFChangeRegionEventFilter.Value)
            {
                //������� ��������� �������
                tFChangeRegionEventPool.Value.Del(eventEntity);
            }
        }
    }
}