
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Character.Events;
using SO.Map;
using SO.Map.Generation;

namespace SO.Character
{
    public class SCharacterControl : IEcsRunSystem
    {
        //Миры
        readonly EcsWorldInject world = default;

        //Персонажи
        readonly EcsPoolInject<CCharacter> characterPool = default;


        //События персонажей
        readonly EcsFilterInject<Inc<RCharacterCreating>> characterCreatingRequestFilter = default;
        readonly EcsPoolInject<RCharacterCreating> characterCreatingRequestPool = default;


        //Данные
        readonly EcsCustomInject<UI.InputData> inputData = default;
        readonly EcsCustomInject<RuntimeData> runtimeData = default;

        public void Run(IEcsSystems systems)
        {
            //Для каждого запроса создания персонажа
            foreach (int requestEntity in characterCreatingRequestFilter.Value)
            {
                //Берём запрос
                ref RCharacterCreating requestComp = ref characterCreatingRequestPool.Value.Get(requestEntity);

                //Создаём нового персонажа
                CharacterCreating(
                    ref requestComp);

                characterCreatingRequestPool.Value.Del(requestEntity);
            }
        }

        void CharacterCreating(
            ref RCharacterCreating requestComp)
        {
            //Создаём новую сущность и назначаем ей компонент персонажа
            int characterEntity = world.Value.NewEntity();
            ref CCharacter character = ref characterPool.Value.Add(characterEntity);

            //Заполняем основные данные персонажа
            character = new(
                world.Value.PackEntity(characterEntity), runtimeData.Value.charactersCount++,
                requestComp.characterName);

            //ТЕСТ
            inputData.Value.playerCharacterPE = character.selfPE;
            //ТЕСТ

            //Запрашиваем инициализацию стартового региона персонажа
            CharacterStartRegionInitializerRequest(character.selfPE);

            //Создаём событие, сообщающее о создании нового персонажа
            ObjectNewCreatedEvent(character.selfPE, ObjectNewCreatedType.Character);
        }

        readonly EcsPoolInject<RRegionInitializer> regionInitializerRequestPool = default;
        readonly EcsPoolInject<RRegionInitializerOwner> regionInitializerOwnerRequestPool = default;
        void CharacterStartRegionInitializerRequest(
            EcsPackedEntity characterPE)
        {
            //Создаём новую сущность и назначаем ей запрос применения инициализатора
            int requestEntity = world.Value.NewEntity();
            ref RRegionInitializer requestCoreComp = ref regionInitializerRequestPool.Value.Add(requestEntity);

            //Заполняем данные запроса
            requestCoreComp = new();
            
            //Назначаем компонент владельца
            ref RRegionInitializerOwner requestOwnerComp = ref regionInitializerOwnerRequestPool.Value.Add(requestEntity);

            //Заполняем данные запроса
            requestOwnerComp = new(characterPE);
        }

        readonly EcsPoolInject<EObjectNewCreated> objectNewCreatedEventPool = default;
        void ObjectNewCreatedEvent(
            EcsPackedEntity objectPE, ObjectNewCreatedType objectType)
        {
            //Создаём новую сущность и назначаем ей событие создания нового объекта
            int eventEntity = world.Value.NewEntity();
            ref EObjectNewCreated eventComp = ref objectNewCreatedEventPool.Value.Add(eventEntity);

            //Заполняем данные события
            eventComp = new(objectPE, objectType);
        }
    }
}