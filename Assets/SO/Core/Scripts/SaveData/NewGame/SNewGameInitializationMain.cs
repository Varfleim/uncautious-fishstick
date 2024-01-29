
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Map.Events;
using SO.Faction.Events;

namespace SO
{
    public class SNewGameInitializationMain : IEcsRunSystem
    {
        //����
        readonly EcsWorldInject world = default;

        //������� �����
        readonly EcsPoolInject<RMapGenerating> mapGeneratingRequestPool = default;

        //������� �������
        readonly EcsPoolInject<RFactionCreating> factionCreatingRequestPool = default;

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
                MapGeneratingRequest(50);

                //����������� �������� �������� �������
                FactionCreatingRequest("TestFaction");

                UnityEngine.Debug.LogWarning("����� ����");
            }
        }

        void MapGeneratingRequest(
            int subdivisions)
        {
            //������ ����� �������� � ��������� �� ������ �������� �����
            int requestEntity = world.Value.NewEntity();
            ref RMapGenerating requestComp = ref mapGeneratingRequestPool.Value.Add(requestEntity);

            //��������� ������ �������
            requestComp = new(subdivisions);
        }
        
        void FactionCreatingRequest(
            string factionName)
        {
            //������ ����� �������� � ��������� �� ������ �������� �������
            int requestEntity = world.Value.NewEntity();
            ref RFactionCreating requestComp = ref factionCreatingRequestPool.Value.Add(requestEntity);

            //��������� ������ �������
            requestComp = new(factionName);
        }
    }
}