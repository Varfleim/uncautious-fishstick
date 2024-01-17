
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using Leopotam.EcsLite.ExtendedSystems;

namespace SCM
{
    public class SEventControl : IEcsRunSystem
    {
        //����
        readonly EcsWorldInject world = default;

        //����� �������
        readonly EcsFilterInject<Inc<RStartNewGame>> startNewGameRequestFilter = default;

        readonly EcsPoolInject<EcsGroupSystemState> ecsGroupSystemStatePool = default;

        public void Run(IEcsSystems systems)
        {
            //��� ������� ������� ������ ����� ����
            foreach(int requestEntity in startNewGameRequestFilter.Value)
            {
                //����������� ���������� ������ ������ "NewGame"
                EcsGroupSystemStateEvent("NewGame", false);

                UnityEngine.Debug.LogWarning("��������� ������� ����� ����");

                world.Value.DelEntity(requestEntity);
            }
        }

        void EcsGroupSystemStateEvent(
            string systemGroupName,
            bool systemGroupState)
        {
            //������ ����� �������� � ��������� �� ������� ����� ��������� ������ ������
            int eventEntity = world.Value.NewEntity();
            ref EcsGroupSystemState eventComp = ref ecsGroupSystemStatePool.Value.Add(eventEntity);

            //��������� �������� ������ ������ � ������ ���������
            eventComp.Name = systemGroupName;
            eventComp.State = systemGroupState;
        }
    }
}