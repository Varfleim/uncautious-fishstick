using System;
using System.Collections.Generic;

using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Character;
using SO.Map.Events;

namespace SO.Map.Region
{
    public class SRegionControl : IEcsRunSystem
    {
        //Миры
        readonly EcsWorldInject world = default;


        //Карта
        readonly EcsPoolInject<CRegionCore> rCPool = default;

        //Персонажи
        readonly EcsPoolInject<CCharacter> characterPool = default;

        public void Run(IEcsSystems systems)
        {
            //Смена владельцев регионов
            RegionChangeOwners();
        }

        readonly EcsFilterInject<Inc<RRegionChangeOwner>> regionChangeOwnerRequestFilter = default;
        readonly EcsPoolInject<RRegionChangeOwner> regionChangeOwnerRequestPool = default;
        void RegionChangeOwners()
        {
            //Для каждого запроса смены владельца региона
            foreach (int requestEntity in regionChangeOwnerRequestFilter.Value)
            {
                //Берём запрос
                ref RRegionChangeOwner requestComp = ref regionChangeOwnerRequestPool.Value.Get(requestEntity);

                //Берём персонажа, который становится владельцем региона
                requestComp.characterPE.Unpack(world.Value, out int characterEntity);
                ref CCharacter character = ref characterPool.Value.Get(characterEntity);

                //Берём регион
                requestComp.regionPE.Unpack(world.Value, out int regionEntity);
                ref CRegionCore rC = ref rCPool.Value.Get(regionEntity);

                //Если смена владельца происходит при инициализации
                if (requestComp.requestType == RegionChangeOwnerType.Initialization)
                {
                    RCChangeOwnerInitialization();
                }


                //Создаём событие, сообщающее о смене владельца региона
                RegionChangeOwnerEvent(
                    rC.selfPE,
                    character.selfPE, rC.ownerCharacterPE);


                //Указываем персонажа-владельца региона
                rC.ownerCharacterPE = character.selfPE;

                //ТЕСТ
                //Заносим PE региона в список персонажа
                character.ownedRCPEs.Add(rC.selfPE);
                //ТЕСТ

                regionChangeOwnerRequestPool.Value.Del(requestEntity);
            }
        }

        void RCChangeOwnerInitialization()
        {

        }

        readonly EcsPoolInject<ERegionChangeOwner> regionChangeOwnerEventPool = default;
        void RegionChangeOwnerEvent(
            EcsPackedEntity regionPE,
            EcsPackedEntity newOwnerCharacterPE, EcsPackedEntity oldOwnerCharacterPE = new())
        {
            //Создаём новую сущность и назначаем ей событие смены владельца RC
            int eventEntity = world.Value.NewEntity();
            ref ERegionChangeOwner eventComp = ref regionChangeOwnerEventPool.Value.Add(eventEntity);

            //Заполняем данные события
            eventComp = new(
                regionPE,
                newOwnerCharacterPE, oldOwnerCharacterPE);
        }
    }
}