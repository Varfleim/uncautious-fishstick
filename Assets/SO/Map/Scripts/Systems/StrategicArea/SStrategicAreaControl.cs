
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
        //����
        readonly EcsWorldInject world = default;


        //�����
        readonly EcsPoolInject<CStrategicArea> sAPool = default;


        //���������
        readonly EcsPoolInject<CCharacter> characterPool = default;

        public void Run(IEcsSystems systems)
        {
            //����� ���������� �������������� ��������
            StrategicAreaChangeOwner();
        }

        readonly EcsFilterInject<Inc<RStrategicAreaChangeOwner>> sAChangeOwnerRequestFilter = default;
        readonly EcsPoolInject<RStrategicAreaChangeOwner> sAChangeOwnerRequestPool = default;
        void StrategicAreaChangeOwner()
        {
            //��� ������� ������� ����� ��������� �������������� �������
            foreach(int requestEntity in sAChangeOwnerRequestFilter.Value)
            {
                //���� ������
                ref RStrategicAreaChangeOwner requestComp = ref sAChangeOwnerRequestPool.Value.Get(requestEntity);

                //���� ���������, ������� ���������� ���������� �������
                requestComp.characterPE.Unpack(world.Value, out int characterEntity);
                ref CCharacter newOwnerCharacter = ref characterPool.Value.Get(characterEntity);

                //���� �������
                requestComp.sAPE.Unpack(world.Value, out int sAEntity);
                ref CStrategicArea sA = ref sAPool.Value.Get(sAEntity);

                //���� ����� ��������� ���������� ��� �������������
                if(requestComp.requestType == StrategicAreaChangeOwnerType.Initialization)
                {
                    StrategicAreaChangeOwnerInitialization();
                }


                //����������� �������, ����� ��� �������� ������� �� ������, ��������� ��� ���������� ��� PE
                //������ �������, ���������� � ����� ��������� �������
                StrategicAreaChangeOwnerEvent(
                    sA.selfPE,
                    newOwnerCharacter.selfPE, sA.ownerCharacterPE);


                //��������� ���������-��������� �������
                sA.ownerCharacterPE = newOwnerCharacter.selfPE;

                //����
                //������� PE ������� � ������ ���������
                newOwnerCharacter.ownedSAPEs.Add(sA.selfPE);
                //����

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
            //������ ����� �������� � ��������� �� ������� ����� ��������� �������������� �������
            int eventEntity = world.Value.NewEntity();
            ref EStrategicAreaChangeOwner eventComp = ref sAChangeOwnerEventPool.Value.Add(eventEntity);

            //��������� ������ �������
            eventComp = new(
                sAPE,
                newOwnerCharacterPE, oldOwnerCharacterPE);
        }
    }
}