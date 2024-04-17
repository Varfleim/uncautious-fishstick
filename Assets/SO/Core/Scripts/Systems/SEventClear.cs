
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Map.Events;
using SO.Warfare.Fleet.Movement;

namespace SO
{
    public class SEventClear : IEcsRunSystem
    {
        //������� �����
        readonly EcsFilterInject<Inc<EProvinceChangeOwner>> provinceChangeOwnerEventFilter = default;
        readonly EcsPoolInject<EProvinceChangeOwner> provinceChangeOwnerEventPool = default;

        //������� �������� ����
        readonly EcsFilterInject<Inc<ETaskForceChangeProvince>> tFChangeProvinceEventFilter = default;
        readonly EcsPoolInject<ETaskForceChangeProvince> tFChangeProvinceEventPool = default;

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

            //��� ������� ������� ����� ��������� ���������
            foreach (int eventEntity in provinceChangeOwnerEventFilter.Value)
            {
                //������� ��������� �������
                provinceChangeOwnerEventPool.Value.Del(eventEntity);
            }

            //��� ������� ������� ����� ��������� ����������� �������
            foreach (int eventEntity in tFChangeProvinceEventFilter.Value)
            {
                //������� ��������� �������
                tFChangeProvinceEventPool.Value.Del(eventEntity);
            }
        }
    }
}