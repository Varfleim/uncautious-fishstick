
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Character.Events;
using SO.Map;
using SO.Map.Generation;

namespace SO.Character
{
    public class SCharacterControl : IEcsRunSystem
    {
        //����
        readonly EcsWorldInject world = default;

        //���������
        readonly EcsPoolInject<CCharacter> characterPool = default;


        //������� ����������
        readonly EcsFilterInject<Inc<RCharacterCreating>> characterCreatingRequestFilter = default;
        readonly EcsPoolInject<RCharacterCreating> characterCreatingRequestPool = default;


        //������
        readonly EcsCustomInject<UI.InputData> inputData = default;
        readonly EcsCustomInject<RuntimeData> runtimeData = default;

        public void Run(IEcsSystems systems)
        {
            //��� ������� ������� �������� ���������
            foreach (int requestEntity in characterCreatingRequestFilter.Value)
            {
                //���� ������
                ref RCharacterCreating requestComp = ref characterCreatingRequestPool.Value.Get(requestEntity);

                //������ ������ ���������
                CharacterCreating(
                    ref requestComp);

                characterCreatingRequestPool.Value.Del(requestEntity);
            }
        }

        void CharacterCreating(
            ref RCharacterCreating requestComp)
        {
            //������ ����� �������� � ��������� �� ��������� ���������
            int characterEntity = world.Value.NewEntity();
            ref CCharacter character = ref characterPool.Value.Add(characterEntity);

            //��������� �������� ������ ���������
            character = new(
                world.Value.PackEntity(characterEntity), runtimeData.Value.charactersCount++,
                requestComp.characterName);

            //����
            inputData.Value.playerCharacterPE = character.selfPE;
            //����

            //����������� ������������� ���������� ������� ���������
            CharacterStartRegionInitializerRequest(character.selfPE);

            //������ �������, ���������� � �������� ������ ���������
            ObjectNewCreatedEvent(character.selfPE, ObjectNewCreatedType.Character);
        }

        readonly EcsPoolInject<RRegionInitializer> regionInitializerRequestPool = default;
        readonly EcsPoolInject<RRegionInitializerOwner> regionInitializerOwnerRequestPool = default;
        void CharacterStartRegionInitializerRequest(
            EcsPackedEntity characterPE)
        {
            //������ ����� �������� � ��������� �� ������ ���������� ��������������
            int requestEntity = world.Value.NewEntity();
            ref RRegionInitializer requestCoreComp = ref regionInitializerRequestPool.Value.Add(requestEntity);

            //��������� ������ �������
            requestCoreComp = new();
            
            //��������� ��������� ���������
            ref RRegionInitializerOwner requestOwnerComp = ref regionInitializerOwnerRequestPool.Value.Add(requestEntity);

            //��������� ������ �������
            requestOwnerComp = new(characterPE);
        }

        readonly EcsPoolInject<EObjectNewCreated> objectNewCreatedEventPool = default;
        void ObjectNewCreatedEvent(
            EcsPackedEntity objectPE, ObjectNewCreatedType objectType)
        {
            //������ ����� �������� � ��������� �� ������� �������� ������ �������
            int eventEntity = world.Value.NewEntity();
            ref EObjectNewCreated eventComp = ref objectNewCreatedEventPool.Value.Add(eventEntity);

            //��������� ������ �������
            eventComp = new(objectPE, objectType);
        }
    }
}