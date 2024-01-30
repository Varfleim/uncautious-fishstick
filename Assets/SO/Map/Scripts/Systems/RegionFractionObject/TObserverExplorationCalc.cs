
using Leopotam.EcsLite;
using Leopotam.EcsLite.Threads;
using System.Collections.Generic;

using SO.Faction;

namespace SO.Map.RFO
{
    public struct TObserverExplorationCalc : IEcsThread<
        CFaction,
        CExplorationObserver,
        CExplorationFRFO,
        CRegion,
        CRegionFO>
    {
        public EcsWorld world;

        public RegionsData regionsData;

        int[] factionEntities;

        CFaction[] factionPool;
        int[] factionIndices;

        CExplorationObserver[] exObserverPool;
        int[] exObserverIndices;

        CExplorationFRFO[] exFRFOPool;
        int[] exFRFOIndices;

        CRegion[] regionPool;
        int[] regionIndices;

        CRegionFO[] rFOPool;
        int[] rFOIndices;

        public void Init(
            int[] entities,
            CFaction[] pool1, int[] indices1,
            CExplorationObserver[] pool2, int[] indices2,
            CExplorationFRFO[] pool3, int[] indices3,
            CRegion[] pool4, int[] indices4,
            CRegionFO[] pool5, int[] indices5)
        {
            factionEntities = entities;

            factionPool = pool1;
            factionIndices = indices1;

            exObserverPool = pool2;
            exObserverIndices = indices2;

            exFRFOPool = pool3;
            exFRFOIndices = indices3;

            regionPool = pool4;
            regionIndices = indices4;

            rFOPool = pool5;
            rFOIndices = indices5;
        }

        public void Execute(int threadId, int fromIndex, int beforeIndex)
        {
            //��� ������ ������� � ������
            for(int a = fromIndex; a < beforeIndex; a++)
            {
                //���� �������
                int factionEntity = factionEntities[a];
                ref CFaction faction = ref factionPool[factionIndices[factionEntity]];

                //��� ������� ����������� �������
                for (int b = 0; b < faction.observerPEs.Count; b++)
                {
                    //���� �����������
                    faction.observerPEs[b].Unpack(world, out int observerEntity);
                    ref CExplorationObserver observer = ref exObserverPool[exObserverIndices[observerEntity]];

                    //���� ������, � ������� ��������� �����������
                    //����
                    ref CExplorationFRFO observerExFRFO = ref exFRFOPool[exFRFOIndices[observerEntity]];

                    observerExFRFO.parentRFOPE.Unpack(world, out int observerRegionEntity);
                    ref CRegion observerRegion = ref regionPool[regionIndices[observerRegionEntity]];

                    observerExFRFO.explorationLevel = 99;
                    //����

                    //������� ��� ������� � �������� ������� �� �����������
                    List<int> regions = regionsData.GetRegionIndicesWithinStepsThreads(
                        world,
                        ref regionPool, ref regionIndices,
                        threadId,
                        ref observerRegion,
                        5);


                    //��� ������� ���������� �������
                    for (int c = 0; c < regions.Count; c++)
                    {
                        //���� RFO
                        regionsData.regionPEs[regions[c]].Unpack(world, out int neighbourRegionEntity);
                        ref CRegionFO neighbourRFO = ref rFOPool[rFOIndices[neighbourRegionEntity]];

                        //���� ExFRFO �������
                        neighbourRFO.factionRFOs[faction.selfIndex].fRFOPE.Unpack(world, out int fRFOEntity);
                        ref CExplorationFRFO exFRFO = ref exFRFOPool[exFRFOIndices[fRFOEntity]];

                        //��������� �������� ������������
                        exFRFO.explorationLevel = 99;
                    }
                }
            }
        }
    }
}