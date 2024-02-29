
using Leopotam.EcsLite;
using Leopotam.EcsLite.Threads;
using System.Collections.Generic;

using SO.Character;
using SO.Map.Hexasphere;

namespace SO.Map
{
    public struct TObserverExplorationCalc : IEcsThread<
        CCharacter,
        CExplorationObserver,
        CExplorationRegionFractionObject,
        CRegionHexasphere,
        CRegionCore>
    {
        public EcsWorld world;

        public RegionsData regionsData;

        int[] characterEntities;

        CCharacter[] characterPool;
        int[] characterIndices;

        CExplorationObserver[] exObserverPool;
        int[] exObserverIndices;

        CExplorationRegionFractionObject[] exRFOPool;
        int[] exRFOIndices;

        CRegionHexasphere[] rHSPool;
        int[] rHSIndices;

        CRegionCore[] rCPool;
        int[] rCIndices;

        public void Init(
            int[] entities,
            CCharacter[] pool1, int[] indices1,
            CExplorationObserver[] pool2, int[] indices2,
            CExplorationRegionFractionObject[] pool3, int[] indices3,
            CRegionHexasphere[] pool4, int[] indices4,
            CRegionCore[] pool5, int[] indices5)
        {
            characterEntities = entities;

            characterPool = pool1;
            characterIndices = indices1;

            exObserverPool = pool2;
            exObserverIndices = indices2;

            exRFOPool = pool3;
            exRFOIndices = indices3;

            rHSPool = pool4;
            rHSIndices = indices4;

            rCPool = pool5;
            rCIndices = indices5;
        }

        public void Execute(int threadId, int fromIndex, int beforeIndex)
        {
            //��� ������� ��������� � ������
            for (int a = fromIndex; a < beforeIndex; a++)
            {
                //���� ���������
                int characterEntity = characterEntities[a];
                ref CCharacter character = ref characterPool[characterIndices[characterEntity]];

                //��� ������� ����������� ���������
                for (int b = 0; b < character.observerPEs.Count; b++)
                {
                    //���� �����������
                    character.observerPEs[b].Unpack(world, out int observerEntity);
                    ref CExplorationObserver observer = ref exObserverPool[exObserverIndices[observerEntity]];

                    //���� ������, � ������� ��������� �����������
                    //����
                    ref CExplorationRegionFractionObject observerExRFO = ref exRFOPool[exRFOIndices[observerEntity]];

                    observerExRFO.parentRFOPE.Unpack(world, out int observerRegionEntity);
                    ref CRegionCore observerRC = ref rCPool[rHSIndices[observerRegionEntity]];

                    observerExRFO.explorationLevel = 99;
                    //����

                    //������� ��� ������� � �������� ������� �� �����������
                    List<int> regions = regionsData.GetRegionIndicesWithinStepsThreads(
                        world,
                        ref rCPool, ref rCIndices,
                        threadId,
                        ref observerRC,
                        5);


                    //��� ������� ���������� �������
                    for (int c = 0; c < regions.Count; c++)
                    {
                        //���� RC
                        regionsData.regionPEs[regions[c]].Unpack(world, out int neighbourRegionEntity);
                        ref CRegionCore neighbourRC = ref rCPool[rCIndices[neighbourRegionEntity]];

                        //���� ExRFO ���������
                        neighbourRC.rFOPEs[character.selfIndex].rFOPE.Unpack(world, out int rFOEntity);
                        ref CExplorationRegionFractionObject exRFO = ref exRFOPool[exRFOIndices[rFOEntity]];

                        //��������� �������� ������������
                        exRFO.explorationLevel = 99;
                    }
                }
            }
        }
    }
}