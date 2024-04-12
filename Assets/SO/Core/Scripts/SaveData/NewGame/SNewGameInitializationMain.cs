
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Country.Events;
using SO.Map.Generation;

namespace SO
{
    public class SNewGameInitializationMain : IEcsRunSystem
    {
        //����
        readonly EcsWorldInject world = default;

        //������� �����
        readonly EcsPoolInject<RMapGenerating> mapGeneratingRequestPool = default;

        //������� �����
        readonly EcsPoolInject<RCountryCreating> countryCreatingRequestPool = default;

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

                //����������� �������� �������� ������
                CountryCreatingRequest("TestCountry");

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
        
        void CountryCreatingRequest(
            string countryName)
        {
            //������ ����� �������� � ��������� �� ������ �������� ������
            int requestEntity = world.Value.NewEntity();
            ref RCountryCreating requestComp = ref countryCreatingRequestPool.Value.Add(requestEntity);

            //��������� ������ �������
            requestComp = new(countryName);
        }
    }
}