
using UnityEngine;

using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using SO.Map.Generation;
using SO.Map.Region;

namespace SO.Map.StrategicArea
{
    public class SMapStrategicArea : IEcsRunSystem
    {
        //����
        readonly EcsWorldInject world = default;


        //�����
        readonly EcsFilterInject<Inc<CRegionCore>> rCFilter = default;
        readonly EcsPoolInject<CRegionCore> rCPool = default;
        readonly EcsFilterInject<Inc<CRegionCore, CTRegionStrategicArea>> regionSAFilter = default;
        readonly EcsPoolInject<CTRegionStrategicArea> rSAPool = default;

        readonly EcsFilterInject<Inc<CStrategicArea, CTStrategicAreaGeneration>> sAGFilter = default;
        readonly EcsPoolInject<CStrategicArea> sAPool = default;
        readonly EcsPoolInject<CTStrategicAreaGeneration> sAGPool = default;

        readonly EcsFilterInject<Inc<CTWithoutFreeNeighbours>> withoutFreeNeighboursFilter = default;
        readonly EcsFilterInject<Inc<CStrategicArea>, Exc<CTWithoutFreeNeighbours>> sAWithFreeNeighboursFilter = default;
        readonly EcsPoolInject<CTWithoutFreeNeighbours> withoutFreeNeighboursPool = default;


        //������� �����
        readonly EcsFilterInject<Inc<RMapGenerating>> mapGeneratingRequestFilter = default;
        readonly EcsPoolInject<RMapGenerating> mapGeneratingRequestPool = default;


        //������
        readonly EcsCustomInject<MapGenerationData> mapGenerationData = default;
        readonly EcsCustomInject<RegionsData> regionsData = default;

        public void Run(IEcsSystems systems)
        {
            //��� ������� ������� ��������� �����
            foreach (int requestEntity in mapGeneratingRequestFilter.Value)
            {
                //���� ������
                ref RMapGenerating requestComp = ref mapGeneratingRequestPool.Value.Get(requestEntity);

                //������ �������������� �������
                StrategicAreaCreate();
            }
        }

        void StrategicAreaCreate()
        {
            //��� ������� �������
            foreach(int regionEntity in rCFilter.Value)
            {
                //��������� ������� ��������� �������������� �������
                ref CTRegionStrategicArea regionSA = ref rSAPool.Value.Add(regionEntity);

                //��������� ������ ����������
                regionSA = new(world.Value.PackEntity(regionEntity));
            }

            //�������� ���������� �������������� �������
            StrategicAreaGenerate();

            //���������� ������� ��������
            StrategicAreaSetNeighbours();

            //��� ������ �������� � ����������� ���������� �������
            foreach (int entity in withoutFreeNeighboursFilter.Value)
            {
                //������� ��������� � ���������
                withoutFreeNeighboursPool.Value.Del(entity);
            }

            //��� ������� ������� �� �������������� ��������
            foreach (int regionEntity in regionSAFilter.Value)
            {
                //���� ������
                ref CRegionCore rC = ref rCPool.Value.Get(regionEntity);
                ref CTRegionStrategicArea rSA = ref rSAPool.Value.Get(regionEntity);

                //��������� ��������� ������ � ��������
                rC.SetParentStrategicArea(rSA.parentSAPE);

                //������� ��������� � ��������
                rSAPool.Value.Del(regionEntity);
            }

            //��� ������ ������� � ����������� ���������
            foreach(int sAEntity in sAGFilter.Value)
            {
                //���� �������
                ref CStrategicArea sA = ref sAPool.Value.Get(sAEntity);
                ref CTStrategicAreaGeneration sAG = ref sAGPool.Value.Get(sAEntity);

                //��������� ��������� ������ � ��������
                sA.regionPEs = sAG.tempRegionPEs.ToArray();
                sA.neighbourSAPEs = sAG.tempNeighbourSAPEs.ToArray();

                sAGPool.Value.Del(sAEntity);
            }
        }

        void StrategicAreaGenerate()
        {
            //���������� ���������� �������������� ��������
            int strategicAreaCount = regionsData.Value.regionPEs.Length / mapGenerationData.Value.strategicAreaAverageSize;

            //������ ������ ��� PE ��������
            regionsData.Value.strategicAreaPEs = new EcsPackedEntity[strategicAreaCount];

            //��� ������ ��������� �������������� �������
            for (int a = 0; a < regionsData.Value.strategicAreaPEs.Length; a++)
            {
                //������ ����� �������� � ��������� �� ���������� �������������� ������� � ��������� �������������� �������
                int sAEntity = world.Value.NewEntity();
                ref CStrategicArea sA = ref sAPool.Value.Add(sAEntity);
                ref CTStrategicAreaGeneration sAG = ref sAGPool.Value.Add(sAEntity);

                //���������� ���� �������
                Color sAColor = new Color32((byte)(Random.value * 255), (byte)(Random.value * 255), (byte)(Random.value * 255), 255);

                //��������� �������� ������ �������
                sA = new(
                    world.Value.PackEntity(sAEntity),
                    sAColor);

                //��������� ������ ���������� ���������
                sAG = new(0);

                //������� PE ������� � ������
                regionsData.Value.strategicAreaPEs[a] = sA.selfPE;

                //���� ��������� ������� ���������� �������
                regionsData.Value.GetRegionRandom().Unpack(world.Value, out int regionEntity);
                ref CTRegionStrategicArea rSA = ref rSAPool.Value.Get(regionEntity);

                //���� ���������� ������ ����������� �����-���� �������
                while (rSA.parentSAPE.Unpack(world.Value, out int regionParentSAEntity))
                {
                    //���� ��������� ������� ���������� �������
                    regionsData.Value.GetRegionRandom().Unpack(world.Value, out regionEntity);
                    rSA = ref rSAPool.Value.Get(regionEntity);
                }

                //���� ���������� ������
                ref CRegionCore rC = ref rCPool.Value.Get(regionEntity);

                //������� ������ �� ��������� ������ �������
                sAG.tempRegionPEs.Add(rSA.selfPE);

                //������� PE ������� � ������ �������
                rSA.parentSAPE = sA.selfPE;

                //����������� ���������� ������� �������� �������� ��� ���� ��������, �������� rC
                //��� ������� ������
                for (int b = 0; b < rC.neighbourRegionPEs.Length; b++)
                {
                    //���� �������� ������
                    rC.neighbourRegionPEs[b].Unpack(world.Value, out int neighbourRegionEntity);

                    //����������� ���������� ������� �������� �������� ��� ����
                    RegionStrategicAreaAddNeighbourWithRSA(
                        neighbourRegionEntity,
                        sA.selfPE);
                }
            }

            //������ ������� ������
            int errorCount = 0;

            //���� ���������� �������, ������� ��������� �������
            while (sAWithFreeNeighboursFilter.Value.GetEntitiesCount() > 0)
            {
                //����������, ������� �������� ���� ��������� � �������� � ���� �����
                int addedRegionsCount = 0;

                //��� ������ �������, ������� ��������� �������
                foreach (int sAEntity in sAWithFreeNeighboursFilter.Value)
                {
                    //���� �������
                    ref CStrategicArea currentSA = ref sAPool.Value.Get(sAEntity);
                    ref CTStrategicAreaGeneration currentSAG = ref sAGPool.Value.Get(sAEntity);

                    //���� �������� ���������� ������� � �������
                    currentSAG.tempRegionPEs[Random.Range(0, currentSAG.tempRegionPEs.Count)].Unpack(world.Value, out int currentFreeRegionEntity);

                    //������ ������� ������
                    int regionErrorCount = 0;
                    //� ���������� ������������ ���������� ������
                    int maxRegionErrorCount = currentSAG.tempRegionPEs.Count * currentSAG.tempRegionPEs.Count;

                    //���� ���������� ������ �� ����� ����� ��������� �������
                    while (withoutFreeNeighboursPool.Value.Has(currentFreeRegionEntity) == true)
                    {
                        //����������� ������� ������
                        regionErrorCount++;

                        //���� ������� ������ ������ ������������� ��������
                        if (regionErrorCount > maxRegionErrorCount)
                        {
                            break;
                        }

                        //���� �������� ���������� ������� � �������
                        currentSAG.tempRegionPEs[Random.Range(0, currentSAG.tempRegionPEs.Count)].Unpack(world.Value, out currentFreeRegionEntity);
                    }

                    //���� ������� ������ ������ ������������� ��������
                    if (regionErrorCount <= maxRegionErrorCount)
                    {
                        //���� ���������� ������
                        ref CRegionCore currentFreeRC = ref rCPool.Value.Get(currentFreeRegionEntity);
                        ref CTRegionStrategicArea currentFreeRSA = ref rSAPool.Value.Get(currentFreeRegionEntity);

                        //���� ��������� ������� ���������� ��������� �������
                        currentFreeRC.neighbourRegionPEs[Random.Range(0, currentFreeRC.neighbourRegionPEs.Length)]
                            .Unpack(world.Value, out int neighbourFreeRegionEntity);
                        ref CTRegionStrategicArea neighbourFreeRSA = ref rSAPool.Value.Get(neighbourFreeRegionEntity);

                        //������ ������� ������
                        int neighbourErrorCount = 0;
                        //� ���������� ������������ ���������� ������
                        int maxNeighbourErrorCount = currentFreeRC.neighbourRegionPEs.Length
                            * (currentFreeRC.neighbourRegionPEs.Length - currentFreeRSA.neighboursWithSACount);

                        //���� ���������� ������ ����������� �����-���� �������
                        while (neighbourFreeRSA.parentSAPE.Unpack(world.Value, out int neighboutParentSAEntity))
                        {
                            //����������� ������� ������
                            neighbourErrorCount++;

                            //���� ������� ������ ������ ������������� ��������
                            if (neighbourErrorCount > maxNeighbourErrorCount)
                            {
                                break;
                            }

                            //���� ��������� ������� ���������� ��������� �������
                            currentFreeRC.neighbourRegionPEs[Random.Range(0, currentFreeRC.neighbourRegionPEs.Length)]
                                .Unpack(world.Value, out neighbourFreeRegionEntity);
                            neighbourFreeRSA = ref rSAPool.Value.Get(neighbourFreeRegionEntity);
                        }

                        //���� ������� ������ ������ ������������� ��������
                        if (neighbourErrorCount <= maxNeighbourErrorCount)
                        {
                            //���� ���������� �������� ������
                            ref CRegionCore neighbourFreeRC = ref rCPool.Value.Get(neighbourFreeRegionEntity);

                            //������� ��� �� ��������� ������ �������
                            currentSAG.tempRegionPEs.Add(neighbourFreeRC.selfPE);

                            //������� PE ������� � ������ �������
                            neighbourFreeRSA.parentSAPE = currentSA.selfPE;

                            //����������� ���������� ������� �������� �������� ��� ���� ��������, �������� neighbourFreeRC
                            //��� ������� ������
                            for (int a = 0; a < neighbourFreeRC.neighbourRegionPEs.Length; a++)
                            {
                                //���� �������� ������
                                neighbourFreeRC.neighbourRegionPEs[a].Unpack(world.Value, out int neighbourNeighbourRegionEntity);

                                //����������� ���������� ������� �������� �������� ��� ����
                                RegionStrategicAreaAddNeighbourWithRSA(
                                    neighbourNeighbourRegionEntity,
                                    currentSA.selfPE);
                            }

                            addedRegionsCount++;

                            //���� ������ ����� ��������� ���������� �������, � ������� �� �����
                            if (withoutFreeNeighboursPool.Value.Has(neighbourFreeRegionEntity) == true
                                && withoutFreeNeighboursPool.Value.Has(sAEntity) == false)
                            {
                                //�������� �������� �������
                                StrategicAreaCheckRegionsWithoutFreeNeighbours(sAEntity);
                            }
                        }
                    }
                }

                //���� �� ���� �� ���� ��������� �� ������ �������
                if (addedRegionsCount == 0)
                {
                    //����������� ������� ������
                    errorCount++;
                }

                //���� ������� ������ ������ �������
                if (errorCount >= strategicAreaCount)
                {
                    //������� ������ � ��������� ����
                    Debug.LogWarning("���������� ���������� ���������� �������������� ���!");

                    break;
                }
            }
        }

        void StrategicAreaSetNeighbours()
        {
            //��� ������� �������
            foreach (int regionEntity in regionSAFilter.Value)
            {
                //���� ��������� ������� �������
                ref CTRegionStrategicArea rSA = ref rSAPool.Value.Get(regionEntity);

                //������� �� ������ �������� �������� �������, � ������� ������ �����������
                rSA.neighbourSAPEs.Remove(rSA.parentSAPE);
            }

            //��� ������ �������������� �������
            foreach (int sAEntity in sAGFilter.Value)
            {
                //���� �������
                ref CStrategicArea sA = ref sAPool.Value.Get(sAEntity);
                ref CTStrategicAreaGeneration sAG = ref sAGPool.Value.Get(sAEntity);

                //��� ������� ������� �������
                for(int a = 0; a < sAG.tempRegionPEs.Count; a++)
                {
                    //���� ��������� ������� �������
                    sAG.tempRegionPEs[a].Unpack(world.Value, out int regionEntity);
                    ref CTRegionStrategicArea rSA = ref rSAPool.Value.Get(regionEntity);

                    //��� ������ �������� ������� �������
                    for(int b = 0; b < rSA.neighbourSAPEs.Count; b++)
                    {
                        //���� ����� ������� ��� � ������ �������� �������� ������� �������
                        if (sAG.tempNeighbourSAPEs.Contains(rSA.neighbourSAPEs[b]) == false)
                        {
                            //������� � PE � ������
                            sAG.tempNeighbourSAPEs.Add(rSA.neighbourSAPEs[b]);
                        }
                    }
                }
            }
        }

        void StrategicAreaAddRegionWithoutFreeNeighbours(
        int sAEntity)
        {
            //���� �������������� �������
            ref CTStrategicAreaGeneration sAG = ref sAGPool.Value.Get(sAEntity);

            //���� ���������� �������� ��� ��������� ������� ������ ���������� ��������
            if (sAG.regionWithoutFreeNeighboursCount < sAG.tempRegionPEs.Count)
            {
                //����������� ���������� �������� ��� ��������� �������
                sAG.regionWithoutFreeNeighboursCount++;

                //���� ���������� �������� ��� ��������� ������� ����� ���������� ��������
                if (sAG.regionWithoutFreeNeighboursCount >= sAG.tempRegionPEs.Count)
                {
                    //��������� ������� ��������� ���������� �������
                    ref CTWithoutFreeNeighbours sAWFN = ref withoutFreeNeighboursPool.Value.Add(sAEntity);
                }
            }
        }

        void StrategicAreaCheckRegionsWithoutFreeNeighbours(
            int sAEntity)
        {
            //���� �������������� �������
            ref CTStrategicAreaGeneration sAG = ref sAGPool.Value.Get(sAEntity);

            //������������ ���������� ��������, �� ������� ��������� �������
            int rWFNCount = 0;

            //��� ������� ������� � �������
            for(int a = 0; a < sAG.tempRegionPEs.Count; a++)
            {
                //���� �������� �������
                sAG.tempRegionPEs[a].Unpack(world.Value, out int regionEntity);

                //���� ������ ����� ��������� ���������� �������
                if(withoutFreeNeighboursPool.Value.Has(regionEntity) == true)
                {
                    //����������� �������
                    rWFNCount++;
                }
            }

            //��������� ������� � ������ �������
            sAG.regionWithoutFreeNeighboursCount = rWFNCount;

            //���� ���������� �������� ��� ��������� ������� ����� ���������� ��������
            if(sAG.regionWithoutFreeNeighboursCount >= sAG.tempRegionPEs.Count)
            {
                //��������� ������� ��������� ���������� �������
                ref CTWithoutFreeNeighbours sAWFN = ref withoutFreeNeighboursPool.Value.Add(sAEntity);
            }
        }

        void RegionStrategicAreaAddNeighbourWithRSA(
            int regionEntity,
            EcsPackedEntity neighbourSAPE)
        {
            //���� ������
            ref CRegionCore rC = ref rCPool.Value.Get(regionEntity);
            ref CTRegionStrategicArea rSA = ref rSAPool.Value.Get(regionEntity);

            //���� ���������� �������, ������������� ��������, ������ ���������� �������
            if(rSA.neighboursWithSACount < rC.neighbourRegionPEs.Length)
            {
                //����������� ���������� �������, ������������� ��������
                rSA.neighboursWithSACount++;

                //������� ����� �������� ������� � ������ �������� �������� �������
                rSA.neighbourSAPEs.Add(neighbourSAPE);

                //���� ���������� �������, ������������� ��������, ����� ���������� �������
                if (rSA.neighboursWithSACount >= rC.neighbourRegionPEs.Length)
                {
                    //��������� ������� ��������� ���������� ��������� �������
                    ref CTWithoutFreeNeighbours rWFN = ref withoutFreeNeighboursPool.Value.Add(regionEntity); 

                    //���� ������ ����������� �������
                    if(rSA.parentSAPE.Unpack(world.Value, out int parentSAEntity))
                    {
                        //�� ����������� ���������� �������� ��� ��������� ������� ��� ���� �������
                        StrategicAreaAddRegionWithoutFreeNeighbours(parentSAEntity);
                    }
                }
            }
        }
    }
}