using System;
using System.Collections.Generic;

using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Faction;
using SO.Map.RFO.Events;

namespace SO.Map.RFO
{
    public class SRFOControl : IEcsRunSystem
    {
        //����
        readonly EcsWorldInject world = default;


        //�����
        readonly EcsFilterInject<Inc<CRegion>> rFOFilter = default;
        readonly EcsPoolInject<CRegionFO> rFOPool = default;

        readonly EcsPoolInject<CExplorationFRFO> exFRFOPool = default;

        //�������
        readonly EcsPoolInject<CFaction> factionPool = default;

        readonly EcsPoolInject<CExplorationObserver> explorationObserverPool = default;

        //������
        readonly EcsCustomInject<RuntimeData> runtimeData = default;

        public void Run(IEcsSystems systems)
        {
            //�������� ExFRFO
            ExFRFOCreating();

            //����� ���������� RFO
            RFOChangeOwners();
        }

        readonly EcsFilterInject<Inc<CFaction, SRExFRFOsCreating>> exFRFOsCreatingSelfRequestFilter = default;
        readonly EcsPoolInject<SRExFRFOsCreating> exFRFOsCreatingSelfRequestPool = default;
        void ExFRFOCreating()
        {
            //���� ������ ������������ �� ����
            if(exFRFOsCreatingSelfRequestFilter.Value.GetEntitiesCount() > 0)
            {
                //������ ��������� ������ DFRFO
                List<DFactionRFO> tempFRFO = new();

                //���������� ����� ���������� �������
                int factionsCount = runtimeData.Value.factionsCount;

                //��� ������� RFO
                foreach(int regionEntity in rFOFilter.Value)
                {
                    //���� RFO
                    ref CRegionFO rFO = ref rFOPool.Value.Get(regionEntity);

                    //������� ��������� ������
                    tempFRFO.Clear();

                    //��� ������ ������� � ������������ �������� ExFRFO
                    foreach (int factionEntity in exFRFOsCreatingSelfRequestFilter.Value)
                    {
                        //���� ������� � ����������
                        ref CFaction faction = ref factionPool.Value.Get(factionEntity);
                        ref SRExFRFOsCreating selfRequestComp = ref exFRFOsCreatingSelfRequestPool.Value.Get(factionEntity);

                        //������ ����� �������� � ��������� �� ��������� ExFRFO
                        int fRFOEntity = world.Value.NewEntity();
                        ref CExplorationFRFO exFRFO = ref exFRFOPool.Value.Add(fRFOEntity);

                        //��������� �������� ������ ExFRFO
                        exFRFO = new(
                            world.Value.PackEntity(fRFOEntity),
                            faction.selfPE,
                            rFO.selfPE);

                        //������ DFRFO ��� �������� ������ ������� ��������������� � RFO
                        DFactionRFO factionRFO = new(
                            exFRFO.selfPE);

                        //������� ��� �� ��������� ������
                        tempFRFO.Add(factionRFO);
                    }

                    //��������� ������ ������ �������
                    int oldArraySize = rFO.factionRFOs.Length;

                    //��������� ������
                    Array.Resize(
                        ref rFO.factionRFOs,
                        factionsCount);

                    //��� ������� DFRFO �� ��������� �������
                    for(int a = 0; a < tempFRFO.Count; a++)
                    {
                        //��������� DFRFO � ������ �� �������
                        rFO.factionRFOs[oldArraySize++] = tempFRFO[a];
                    }
                }

                //��� ������� ����������� �������� ExFRFO
                foreach(int factionEntity in exFRFOsCreatingSelfRequestFilter.Value)
                {
                    exFRFOsCreatingSelfRequestPool.Value.Del(factionEntity);
                }
            }
        }

        readonly EcsFilterInject<Inc<RRFOChangeOwner>> rFOChangeOwnerRequestFilter = default;
        readonly EcsPoolInject<RRFOChangeOwner> rFOChangeOwnerRequestPool = default;
        void RFOChangeOwners()
        {
            //��� ������� ������� ����� ��������� RFO
            foreach (int requestEntity in rFOChangeOwnerRequestFilter.Value)
            {
                //���� ������
                ref RRFOChangeOwner requestComp = ref rFOChangeOwnerRequestPool.Value.Get(requestEntity);

                //���� �������, ������� ���������� ���������� RFO
                requestComp.factionPE.Unpack(world.Value, out int factionEntity);
                ref CFaction faction = ref factionPool.Value.Get(factionEntity);

                //���� RFO
                requestComp.regionPE.Unpack(world.Value, out int regionEntity);
                ref CRegionFO rFO = ref rFOPool.Value.Get(regionEntity);

                //���� ����� ��������� ���������� ��� �������������
                if (requestComp.actionType == RegionChangeOwnerType.Initialization)
                {
                    RFOChangeOwnerInitialization();
                }

                //��������� �������-��������� RFO
                rFO.ownerFactionPE = faction.selfPE;

                //������� PE RFO � ������ �������
                faction.ownedRFOPEs.Add(rFO.selfPE);

                //����
                //���� ExFRFO ������� ������
                rFO.factionRFOs[faction.selfIndex].fRFOPE.Unpack(world.Value, out int fRFOEntity);
                ref CExplorationFRFO exFRFO = ref exFRFOPool.Value.Get(fRFOEntity);

                //��������� FRFO ��������� ����������� � ��������� ������ �� ���� � ������ �������
                ref CExplorationObserver exObserver = ref explorationObserverPool.Value.Add(fRFOEntity);
                faction.observerPEs.Add(exFRFO.selfPE);
                //����

                rFOChangeOwnerRequestPool.Value.Del(requestEntity);
            }
        }

        void RFOChangeOwnerInitialization()
        {

        }
    }
}