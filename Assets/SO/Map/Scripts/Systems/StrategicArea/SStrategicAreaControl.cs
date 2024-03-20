
using System.Collections;
using System.Collections.Generic;

using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Character;
using SO.Map.Events;

namespace SO.Map.StrategicArea
{
    public class SStrategicAreaControl : IEcsRunSystem
    {
        //Миры
        readonly EcsWorldInject world = default;


        //Карта
        readonly EcsPoolInject<CStrategicArea> sAPool = default;


        //Персонажи
        readonly EcsPoolInject<CCharacter> characterPool = default;

        public void Run(IEcsSystems systems)
        {
            //Смена владельцев стратегических областей
            StrategicAreaChangeOwner();
        }

        readonly EcsFilterInject<Inc<RStrategicAreaChangeOwner>> sAChangeOwnerRequestFilter = default;
        readonly EcsPoolInject<RStrategicAreaChangeOwner> sAChangeOwnerRequestPool = default;
        void StrategicAreaChangeOwner()
        {
            //Для каждого запроса смены владельца стратегической области
            foreach(int requestEntity in sAChangeOwnerRequestFilter.Value)
            {
                //Берём запрос
                ref RStrategicAreaChangeOwner requestComp = ref sAChangeOwnerRequestPool.Value.Get(requestEntity);

                //Берём персонажа, который становится владельцем области
                requestComp.characterPE.Unpack(world.Value, out int characterEntity);
                ref CCharacter newOwnerCharacter = ref characterPool.Value.Get(characterEntity);

                //Берём область
                requestComp.sAPE.Unpack(world.Value, out int sAEntity);
                ref CStrategicArea sA = ref sAPool.Value.Get(sAEntity);

                //Если смена владельца происходит при инициализации
                if(requestComp.requestType == StrategicAreaChangeOwnerType.Initialization)
                {
                    StrategicAreaChangeOwnerInitialization();
                }


                //Запрашиваем событие, когда ещё владелец области не изменён, поскольку нам необходима его PE
                //Создаём событие, сообщающее о смене владельца области
                StrategicAreaChangeOwnerEvent(
                    sA.selfPE,
                    newOwnerCharacter.selfPE, sA.ownerCharacterPE);


                //Указываем персонажа-владельца области
                sA.ownerCharacterPE = newOwnerCharacter.selfPE;

                //ТЕСТ
                //Заносим PE области в список персонажа
                newOwnerCharacter.ownedSAPEs.Add(sA.selfPE);
                //ТЕСТ

                sAChangeOwnerRequestPool.Value.Del(requestEntity);
            }
        }

        void StrategicAreaChangeOwnerInitialization()
        {

        }

        readonly EcsPoolInject<EStrategicAreaChangeOwner> sAChangeOwnerEventPool = default;
        void StrategicAreaChangeOwnerEvent(
            EcsPackedEntity sAPE,
            EcsPackedEntity newOwnerCharacterPE, EcsPackedEntity oldOwnerCharacterPE)
        {
            //Создаём новую сущность и назначаем ей событие смены владельца стратегической области
            int eventEntity = world.Value.NewEntity();
            ref EStrategicAreaChangeOwner eventComp = ref sAChangeOwnerEventPool.Value.Add(eventEntity);

            //Заполняем данные события
            eventComp = new(
                sAPE,
                newOwnerCharacterPE, oldOwnerCharacterPE);
        }
    }
}