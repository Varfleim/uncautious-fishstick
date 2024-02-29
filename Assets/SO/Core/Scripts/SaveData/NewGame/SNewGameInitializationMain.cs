
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Map.Events;
using SO.Character.Events;

namespace SO
{
    public class SNewGameInitializationMain : IEcsRunSystem
    {
        //����
        readonly EcsWorldInject world = default;

        //������� �����
        readonly EcsPoolInject<RMapGenerating> mapGeneratingRequestPool = default;

        //������� ����������
        readonly EcsPoolInject<RCharacterCreating> characterCreatingRequestPool = default;

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

                //����������� �������� ��������� ���������
                CharacterCreatingRequest("TestCharacter");

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
        
        void CharacterCreatingRequest(
            string characterName)
        {
            //������ ����� �������� � ��������� �� ������ �������� ���������
            int requestEntity = world.Value.NewEntity();
            ref RCharacterCreating requestComp = ref characterCreatingRequestPool.Value.Add(requestEntity);

            //��������� ������ �������
            requestComp = new(characterName);
        }
    }
}