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
        //����
        readonly EcsWorldInject world = default;


        //�����
        readonly EcsPoolInject<CRegionCore> rCPool = default;

        //���������
        readonly EcsPoolInject<CCharacter> characterPool = default;

        public void Run(IEcsSystems systems)
        {
            //����� ���������� ��������
            RegionChangeOwners();
        }

        readonly EcsFilterInject<Inc<RRegionChangeOwner>> regionChangeOwnerRequestFilter = default;
        readonly EcsPoolInject<RRegionChangeOwner> regionChangeOwnerRequestPool = default;
        void RegionChangeOwners()
        {
            //��� ������� ������� ����� ��������� �������
            foreach (int requestEntity in regionChangeOwnerRequestFilter.Value)
            {
                //���� ������
                ref RRegionChangeOwner requestComp = ref regionChangeOwnerRequestPool.Value.Get(requestEntity);

                //���� ���������, ������� ���������� ���������� �������
                requestComp.characterPE.Unpack(world.Value, out int characterEntity);
                ref CCharacter character = ref characterPool.Value.Get(characterEntity);

                //���� ������
                requestComp.regionPE.Unpack(world.Value, out int regionEntity);
                ref CRegionCore rC = ref rCPool.Value.Get(regionEntity);

                //���� ����� ��������� ���������� ��� �������������
                if (requestComp.requestType == RegionChangeOwnerType.Initialization)
                {
                    RCChangeOwnerInitialization();
                }


                //������ �������, ���������� � ����� ��������� �������
                RegionChangeOwnerEvent(
                    rC.selfPE,
                    character.selfPE, rC.ownerCharacterPE);


                //��������� ���������-��������� �������
                rC.ownerCharacterPE = character.selfPE;

                //����
                //������� PE ������� � ������ ���������
                character.ownedRCPEs.Add(rC.selfPE);
                //����

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
            //������ ����� �������� � ��������� �� ������� ����� ��������� RC
            int eventEntity = world.Value.NewEntity();
            ref ERegionChangeOwner eventComp = ref regionChangeOwnerEventPool.Value.Add(eventEntity);

            //��������� ������ �������
            eventComp = new(
                regionPE,
                newOwnerCharacterPE, oldOwnerCharacterPE);
        }
    }
}