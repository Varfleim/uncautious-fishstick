
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Map;

namespace SO
{
    public class SEventClear : IEcsRunSystem
    {
        //������� �����
        readonly EcsFilterInject<Inc<ERCChangeOwner>> rFOChangeOwnerEventFilter = default;
        readonly EcsPoolInject<ERCChangeOwner> rFOChangeOwnerEventPool = default;

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
        }
    }
}