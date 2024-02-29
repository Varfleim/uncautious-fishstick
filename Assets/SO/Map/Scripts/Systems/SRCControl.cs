using System;
using System.Collections.Generic;

using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Character;
using SO.Map.Events;

namespace SO.Map
{
    public class SRCControl : IEcsRunSystem
    {
        //����
        readonly EcsWorldInject world = default;


        //�����
        readonly EcsFilterInject<Inc<CRegionCore>> regionFilter = default;
        readonly EcsPoolInject<CRegionCore> rCPool = default;

        readonly EcsPoolInject<CExplorationRegionFractionObject> exRFOPool = default;

        //���������
        readonly EcsPoolInject<CCharacter> characterPool = default;

        readonly EcsPoolInject<CExplorationObserver> explorationObserverPool = default;


        //������
        readonly EcsCustomInject<RuntimeData> runtimeData = default;

        public void Run(IEcsSystems systems)
        {
            //�������� ExRFO
            ExRFOCreating();

            //����� ���������� RC
            RCChangeOwners();
        }

        readonly EcsFilterInject<Inc<CCharacter, SRExRFOsCreating>> exRFOsCreatingSelfRequestFilter = default;
        readonly EcsPoolInject<SRExRFOsCreating> exRFOsCreatingSelfRequestPool = default;
        void ExRFOCreating()
        {
            //���� ������ ������������ �� ����
            if (exRFOsCreatingSelfRequestFilter.Value.GetEntitiesCount() > 0)
            {
                //������ ��������� ������ DRFO
                List<DRegionCharacterObject> tempRFO = new();

                //���������� ����� ���������� ����������
                int charactersCount = runtimeData.Value.charactersCount;

                //��� ������� �������
                foreach (int regionEntity in regionFilter.Value)
                {
                    //���� RC
                    ref CRegionCore rC = ref rCPool.Value.Get(regionEntity);

                    //������� ��������� ������
                    tempRFO.Clear();

                    //��� ������� ��������� � ������������ �������� ExRFO
                    foreach (int characterEntity in exRFOsCreatingSelfRequestFilter.Value)
                    {
                        //���� ��������� � ����������
                        ref CCharacter character = ref characterPool.Value.Get(characterEntity);
                        ref SRExRFOsCreating selfRequestComp = ref exRFOsCreatingSelfRequestPool.Value.Get(characterEntity);

                        //������ ����� �������� � ��������� �� ��������� ExRFO
                        int rFOEntity = world.Value.NewEntity();
                        ref CExplorationRegionFractionObject exRFO = ref exRFOPool.Value.Add(rFOEntity);

                        //��������� �������� ������ ExRFO
                        exRFO = new(
                            world.Value.PackEntity(rFOEntity),
                            character.selfPE,
                            rC.selfPE);

                        //������ DRFO ��� �������� ������ ��������� ��������������� � RC
                        DRegionCharacterObject rFO = new(
                            exRFO.selfPE);

                        //������� ��� �� ��������� ������
                        tempRFO.Add(rFO);
                    }

                    //��������� ������ ������ �������
                    int oldArraySize = rC.rFOPEs.Length;

                    //��������� ������
                    Array.Resize(
                        ref rC.rFOPEs,
                        charactersCount);

                    //��� ������� DRFO �� ��������� �������
                    for (int a = 0; a < tempRFO.Count; a++)
                    {
                        //��������� DRFO � ������ �� �������
                        rC.rFOPEs[oldArraySize++] = tempRFO[a];
                    }
                }

                //��� ������� ����������� �������� ExRFO
                foreach (int characterEntity in exRFOsCreatingSelfRequestFilter.Value)
                {
                    exRFOsCreatingSelfRequestPool.Value.Del(characterEntity);
                }
            }
        }

        readonly EcsFilterInject<Inc<RRCChangeOwner>> rCChangeOwnerRequestFilter = default;
        readonly EcsPoolInject<RRCChangeOwner> rCChangeOwnerRequestPool = default;
        void RCChangeOwners()
        {
            //��� ������� ������� ����� ��������� RC
            foreach (int requestEntity in rCChangeOwnerRequestFilter.Value)
            {
                //���� ������
                ref RRCChangeOwner requestComp = ref rCChangeOwnerRequestPool.Value.Get(requestEntity);

                //���� ���������, ������� ���������� ���������� RC
                requestComp.characterPE.Unpack(world.Value, out int characterEntity);
                ref CCharacter character = ref characterPool.Value.Get(characterEntity);

                //���� RC
                requestComp.regionPE.Unpack(world.Value, out int regionEntity);
                ref CRegionCore rC = ref rCPool.Value.Get(regionEntity);

                //���� ����� ��������� ���������� ��� �������������
                if (requestComp.requestType == RCChangeOwnerType.Initialization)
                {
                    RCChangeOwnerInitialization();
                }


                //������ �������, ���������� � ����� ��������� RC
                RegionCoreChangeOwnerEvent(
                    rC.selfPE,
                    character.selfPE, rC.ownerCharacterPE);


                //��������� ���������-��������� RC
                rC.ownerCharacterPE = character.selfPE;

                //����
                //������� PE ������� � ������ ���������
                character.ownedRCPEs.Add(rC.selfPE);

                //���� ExRFO ��������� ������
                rC.rFOPEs[character.selfIndex].rFOPE.Unpack(world.Value, out int rFOEntity);
                ref CExplorationRegionFractionObject exRFO = ref exRFOPool.Value.Get(rFOEntity);

                //��������� RFO ��������� ����������� � ��������� ������ �� ���� � ������ ���������
                ref CExplorationObserver exObserver = ref explorationObserverPool.Value.Add(rFOEntity);
                character.observerPEs.Add(exRFO.selfPE);
                //����

                rCChangeOwnerRequestPool.Value.Del(requestEntity);
            }
        }

        void RCChangeOwnerInitialization()
        {

        }

        readonly EcsPoolInject<ERegionCoreChangeOwner> rCChangeOwnerEventPool = default;
        void RegionCoreChangeOwnerEvent(
            EcsPackedEntity regionPE,
            EcsPackedEntity newOwnerCharacterPE, EcsPackedEntity oldOwnerCharacterPE = new())
        {
            //������ ����� �������� � ��������� �� ������� ����� ��������� RC
            int eventEntity = world.Value.NewEntity();
            ref ERegionCoreChangeOwner eventComp = ref rCChangeOwnerEventPool.Value.Add(eventEntity);

            //��������� ������ �������
            eventComp = new(
                regionPE,
                newOwnerCharacterPE, oldOwnerCharacterPE);
        }
    }
}