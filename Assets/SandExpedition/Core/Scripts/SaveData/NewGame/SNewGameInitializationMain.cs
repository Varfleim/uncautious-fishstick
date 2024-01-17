
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SCM.Map.Events;

namespace SCM
{
    public class SNewGameInitializationMain : IEcsRunSystem
    {
        //����
        readonly EcsWorldInject world = default;

        //������� �����
        readonly EcsPoolInject<RMapGeneration> mapGenerationRequestPool = default;

        //����� �������
        readonly EcsFilterInject<Inc<RStartNewGame>> startNewGameRequestFilter = default;
        readonly EcsPoolInject<RStartNewGame> startNewGameRequestPool = default;

        public void Run(IEcsSystems systems)
        {
            //��� ������� ������� ������ ����� ����
            foreach(int requestEntity in startNewGameRequestFilter.Value)
            {
                //���� ������
                ref RStartNewGame requestComp = ref startNewGameRequestPool.Value.Get(requestEntity);

                //����������� ��������� �����
                MapGenerationRequest(50);

                UnityEngine.Debug.LogWarning("����� ����");
            }
        }

        void MapGenerationRequest(
            int subdivisions)
        {
            //������ ����� �������� � ��������� �� ������ �������� �����
            int requestEntity = world.Value.NewEntity();
            ref RMapGeneration requestComp = ref mapGenerationRequestPool.Value.Add(requestEntity);

            //��������� ������ �������
            requestComp = new(subdivisions);
        }
    }
}