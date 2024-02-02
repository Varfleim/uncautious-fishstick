using System;
using System.Collections.Generic;

using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Faction;
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

        //�������
        readonly EcsPoolInject<CFaction> factionPool = default;

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

        readonly EcsFilterInject<Inc<CFaction, SRExRFOsCreating>> exRFOsCreatingSelfRequestFilter = default;
        readonly EcsPoolInject<SRExRFOsCreating> exRFOsCreatingSelfRequestPool = default;
        void ExRFOCreating()
        {
            //���� ������ ������������ �� ����
            if (exRFOsCreatingSelfRequestFilter.Value.GetEntitiesCount() > 0)
            {
                //������ ��������� ������ DRFO
                List<DRegionFactionObject> tempRFO = new();

                //���������� ����� ���������� �������
                int factionsCount = runtimeData.Value.factionsCount;

                //��� ������� �������
                foreach (int regionEntity in regionFilter.Value)
                {
                    //���� RC
                    ref CRegionCore rC = ref rCPool.Value.Get(regionEntity);

                    //������� ��������� ������
                    tempRFO.Clear();

                    //��� ������ ������� � ������������ �������� ExRFO
                    foreach (int factionEntity in exRFOsCreatingSelfRequestFilter.Value)
                    {
                        //���� ������� � ����������
                        ref CFaction faction = ref factionPool.Value.Get(factionEntity);
                        ref SRExRFOsCreating selfRequestComp = ref exRFOsCreatingSelfRequestPool.Value.Get(factionEntity);

                        //������ ����� �������� � ��������� �� ��������� ExRFO
                        int rFOEntity = world.Value.NewEntity();
                        ref CExplorationRegionFractionObject exRFO = ref exRFOPool.Value.Add(rFOEntity);

                        //��������� �������� ������ ExRFO
                        exRFO = new(
                            world.Value.PackEntity(rFOEntity),
                            faction.selfPE,
                            rC.selfPE);

                        //������ DRFO ��� �������� ������ ������� ��������������� � RC
                        DRegionFactionObject rFO = new(
                            exRFO.selfPE);

                        //������� ��� �� ��������� ������
                        tempRFO.Add(rFO);
                    }

                    //��������� ������ ������ �������
                    int oldArraySize = rC.rFOPEs.Length;

                    //��������� ������
                    Array.Resize(
                        ref rC.rFOPEs,
                        factionsCount);

                    //��� ������� DRFO �� ��������� �������
                    for (int a = 0; a < tempRFO.Count; a++)
                    {
                        //��������� DRFO � ������ �� �������
                        rC.rFOPEs[oldArraySize++] = tempRFO[a];
                    }
                }

                //��� ������� ����������� �������� ExRFO
                foreach (int factionEntity in exRFOsCreatingSelfRequestFilter.Value)
                {
                    exRFOsCreatingSelfRequestPool.Value.Del(factionEntity);
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

                //���� �������, ������� ���������� ���������� RC
                requestComp.factionPE.Unpack(world.Value, out int factionEntity);
                ref CFaction faction = ref factionPool.Value.Get(factionEntity);

                //���� RC
                requestComp.regionPE.Unpack(world.Value, out int regionEntity);
                ref CRegionCore rC = ref rCPool.Value.Get(regionEntity);

                //���� ����� ��������� ���������� ��� �������������
                if (requestComp.requestType == RCChangeOwnerType.Initialization)
                {
                    RCChangeOwnerInitialization();
                }


                //������ �������, ���������� � ����� ��������� RC
                RCChangeOwnerEvent(
                    rC.selfPE,
                    faction.selfPE, rC.ownerFactionPE);


                //��������� �������-��������� RC
                rC.ownerFactionPE = faction.selfPE;

                //������� PE ������� � ������ �������
                faction.ownedRCPEs.Add(rC.selfPE);

                //����
                //���� ExRFO ������� ������
                rC.rFOPEs[faction.selfIndex].rFOPE.Unpack(world.Value, out int rFOEntity);
                ref CExplorationRegionFractionObject exRFO = ref exRFOPool.Value.Get(rFOEntity);

                //��������� RFO ��������� ����������� � ��������� ������ �� ���� � ������ �������
                ref CExplorationObserver exObserver = ref explorationObserverPool.Value.Add(rFOEntity);
                faction.observerPEs.Add(exRFO.selfPE);
                //����

                rCChangeOwnerRequestPool.Value.Del(requestEntity);
            }
        }

        void RCChangeOwnerInitialization()
        {

        }

        readonly EcsPoolInject<ERCChangeOwner> rCChangeOwnerEventPool = default;
        void RCChangeOwnerEvent(
            EcsPackedEntity regionPE,
            EcsPackedEntity newOwnerFactionPE, EcsPackedEntity oldOwnerFactionPE = new())
        {
            //������ ����� �������� � ��������� �� ������� ����� ��������� RC
            int eventEntity = world.Value.NewEntity();
            ref ERCChangeOwner eventComp = ref rCChangeOwnerEventPool.Value.Add(eventEntity);

            //��������� ������ �������
            eventComp = new(
                regionPE,
                newOwnerFactionPE, oldOwnerFactionPE);
        }
    }
}