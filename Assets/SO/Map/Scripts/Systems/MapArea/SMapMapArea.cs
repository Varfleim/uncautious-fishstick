
using UnityEngine;

using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using SO.Map.Generation;
using SO.Map.Province;

namespace SO.Map.MapArea
{
    public class SMapMapArea : IEcsRunSystem
    {
        //����
        readonly EcsWorldInject world = default;


        //�����
        readonly EcsFilterInject<Inc<CProvinceCore>> pCFilter = default;
        readonly EcsPoolInject<CProvinceCore> pCPool = default;
        readonly EcsFilterInject<Inc<CProvinceCore, CTProvinceMapArea>> provinceMAFilter = default;
        readonly EcsPoolInject<CTProvinceMapArea> pMAPool = default;

        readonly EcsFilterInject<Inc<CMapArea, CTMapAreaGeneration>> mAGFilter = default;
        readonly EcsPoolInject<CMapArea> mAPool = default;
        readonly EcsPoolInject<CTMapAreaGeneration> mAGPool = default;

        readonly EcsFilterInject<Inc<CTWithoutFreeNeighbours>> withoutFreeNeighboursFilter = default;
        readonly EcsFilterInject<Inc<CMapArea>, Exc<CTWithoutFreeNeighbours>> mAWithFreeNeighboursFilter = default;
        readonly EcsPoolInject<CTWithoutFreeNeighbours> withoutFreeNeighboursPool = default;


        //������� �����
        readonly EcsFilterInject<Inc<RMapGenerating>> mapGeneratingRequestFilter = default;
        readonly EcsPoolInject<RMapGenerating> mapGeneratingRequestPool = default;


        //������
        readonly EcsCustomInject<MapGenerationData> mapGenerationData = default;
        readonly EcsCustomInject<ProvincesData> provincesData = default;

        public void Run(IEcsSystems systems)
        {
            //��� ������� ������� ��������� �����
            foreach (int requestEntity in mapGeneratingRequestFilter.Value)
            {
                //���� ������
                ref RMapGenerating requestComp = ref mapGeneratingRequestPool.Value.Get(requestEntity);

                //������ ������� �����
                MapAreaCreate();
            }
        }

        void MapAreaCreate()
        {
            //��� ������ ���������
            foreach (int provinceEntity in pCFilter.Value)
            {
                //��������� ��������� ��������� ������� �����
                ref CTProvinceMapArea provinceMA = ref pMAPool.Value.Add(provinceEntity);

                //��������� ������ ����������
                provinceMA = new(world.Value.PackEntity(provinceEntity));
            }

            //�������� ���������� ������� �����
            MapAreaGenerate();

            //���������� ������� ��������
            MapAreaSetNeighbours();

            //��� ������ �������� � ����������� ���������� �������
            foreach (int entity in withoutFreeNeighboursFilter.Value)
            {
                //������� ��������� � ���������
                withoutFreeNeighboursPool.Value.Del(entity);
            }

            //��� ������ ��������� � �������� �����
            foreach (int provinceEntity in provinceMAFilter.Value)
            {
                //���� ���������
                ref CProvinceCore pC = ref pCPool.Value.Get(provinceEntity);
                ref CTProvinceMapArea pMA = ref pMAPool.Value.Get(provinceEntity);

                //��������� ��������� ������ � ��������
                pC.SetParentMapArea(pMA.parentMAPE);

                //������� ��������� � ��������
                pMAPool.Value.Del(provinceEntity);
            }

            //��� ������ ������� � ����������� ���������
            foreach(int mAEntity in mAGFilter.Value)
            {
                //���� �������
                ref CMapArea mA = ref mAPool.Value.Get(mAEntity);
                ref CTMapAreaGeneration mAG = ref mAGPool.Value.Get(mAEntity);

                //��������� ��������� ������ � ��������
                mA.provincePEs = mAG.tempProvincePEs.ToArray();
                mA.neighbourMAPEs = mAG.tempNeighbourMAPEs.ToArray();

                mAGPool.Value.Del(mAEntity);
            }
        }

        void MapAreaGenerate()
        {
            //���������� ���������� �������� �����
            int mapAreaCount = provincesData.Value.provincePEs.Length / mapGenerationData.Value.mapAreaAverageSize;

            //������ ������ ��� PE ��������
            provincesData.Value.mapAreaPEs = new EcsPackedEntity[mapAreaCount];

            //��� ������ ��������� ������� �����
            for (int a = 0; a < provincesData.Value.mapAreaPEs.Length; a++)
            {
                //������ ����� �������� � ��������� �� ���������� ������� ����� � ��������� ������� �����
                int mAEntity = world.Value.NewEntity();
                ref CMapArea mA = ref mAPool.Value.Add(mAEntity);
                ref CTMapAreaGeneration mAG = ref mAGPool.Value.Add(mAEntity);

                //���������� ���� �������
                Color mAColor = new Color32((byte)(Random.value * 255), (byte)(Random.value * 255), (byte)(Random.value * 255), 255);

                //��������� �������� ������ �������
                mA = new(
                    world.Value.PackEntity(mAEntity),
                    mAColor);

                //��������� ������ ���������� ���������
                mAG = new(0);

                //������� PE ������� � ������
                provincesData.Value.mapAreaPEs[a] = mA.selfPE;

                //���� ��������� ������� ��������� ���������
                provincesData.Value.GetProvinceRandom().Unpack(world.Value, out int provinceEntity);
                ref CTProvinceMapArea pMA = ref pMAPool.Value.Get(provinceEntity);

                //���� ���������� ��������� ����������� �����-���� �������
                while (pMA.parentMAPE.Unpack(world.Value, out int provinceParentMAEntity))
                {
                    //���� ��������� ������� ��������� ���������
                    provincesData.Value.GetProvinceRandom().Unpack(world.Value, out provinceEntity);
                    pMA = ref pMAPool.Value.Get(provinceEntity);
                }

                //���� ���������� ���������
                ref CProvinceCore pC = ref pCPool.Value.Get(provinceEntity);

                //������� ��������� �� ��������� ������ �������
                mAG.tempProvincePEs.Add(pMA.selfPE);

                //������� PE ������� � ������ ���������
                pMA.parentMAPE = mA.selfPE;

                //����������� ���������� ������� �������� ��������� ��� ���� ���������, �������� PC
                //��� ������� ������
                for (int b = 0; b < pC.neighbourProvincePEs.Length; b++)
                {
                    //���� �������� ������
                    pC.neighbourProvincePEs[b].Unpack(world.Value, out int neighbourProvinceEntity);

                    //����������� ���������� ������� �������� ��������� ��� ����
                    ProvinceMapAreaAddNeighbourWithPMA(
                        neighbourProvinceEntity,
                        mA.selfPE);
                }
            }

            //������ ������� ������
            int errorCount = 0;

            //���� ���������� �������, ������� ��������� �������
            while (mAWithFreeNeighboursFilter.Value.GetEntitiesCount() > 0)
            {
                //����������, ������� ��������� ���� ��������� � �������� � ���� �����
                int addedProvincesCount = 0;

                //��� ������ �������, ������� ��������� �������
                foreach (int mAEntity in mAWithFreeNeighboursFilter.Value)
                {
                    //���� �������
                    ref CMapArea currentMA = ref mAPool.Value.Get(mAEntity);
                    ref CTMapAreaGeneration currentMAG = ref mAGPool.Value.Get(mAEntity);

                    //���� �������� ��������� ��������� � �������
                    currentMAG.tempProvincePEs[Random.Range(0, currentMAG.tempProvincePEs.Count)].Unpack(world.Value, out int currentFreeProvinceEntity);

                    //������ ������� ������
                    int provinceErrorCount = 0;
                    //� ���������� ������������ ���������� ������
                    int maxProvinceErrorCount = currentMAG.tempProvincePEs.Count * currentMAG.tempProvincePEs.Count;

                    //���� ���������� ��������� �� ����� ����� ��������� �������
                    while (withoutFreeNeighboursPool.Value.Has(currentFreeProvinceEntity) == true)
                    {
                        //����������� ������� ������
                        provinceErrorCount++;

                        //���� ������� ������ ������ ������������� ��������
                        if (provinceErrorCount > maxProvinceErrorCount)
                        {
                            break;
                        }

                        //���� �������� ��������� ��������� � �������
                        currentMAG.tempProvincePEs[Random.Range(0, currentMAG.tempProvincePEs.Count)].Unpack(world.Value, out currentFreeProvinceEntity);
                    }

                    //���� ������� ������ ������ ������������� ��������
                    if (provinceErrorCount <= maxProvinceErrorCount)
                    {
                        //���� ���������� ���������
                        ref CProvinceCore currentFreePC = ref pCPool.Value.Get(currentFreeProvinceEntity);
                        ref CTProvinceMapArea currentFreePMA = ref pMAPool.Value.Get(currentFreeProvinceEntity);

                        //���� ��������� ������� ��������� �������� ���������
                        currentFreePC.neighbourProvincePEs[Random.Range(0, currentFreePC.neighbourProvincePEs.Length)]
                            .Unpack(world.Value, out int neighbourFreeProvinceEntity);
                        ref CTProvinceMapArea neighbourFreePMA = ref pMAPool.Value.Get(neighbourFreeProvinceEntity);

                        //������ ������� ������
                        int neighbourErrorCount = 0;
                        //� ���������� ������������ ���������� ������
                        int maxNeighbourErrorCount = currentFreePC.neighbourProvincePEs.Length
                            * (currentFreePC.neighbourProvincePEs.Length - currentFreePMA.neighboursWithMACount);

                        //���� ���������� ��������� ����������� �����-���� �������
                        while (neighbourFreePMA.parentMAPE.Unpack(world.Value, out int neighboutParentMAEntity))
                        {
                            //����������� ������� ������
                            neighbourErrorCount++;

                            //���� ������� ������ ������ ������������� ��������
                            if (neighbourErrorCount > maxNeighbourErrorCount)
                            {
                                break;
                            }

                            //���� ��������� ������� ��������� �������� ���������
                            currentFreePC.neighbourProvincePEs[Random.Range(0, currentFreePC.neighbourProvincePEs.Length)]
                                .Unpack(world.Value, out neighbourFreeProvinceEntity);
                            neighbourFreePMA = ref pMAPool.Value.Get(neighbourFreeProvinceEntity);
                        }

                        //���� ������� ������ ������ ������������� ��������
                        if (neighbourErrorCount <= maxNeighbourErrorCount)
                        {
                            //���� ���������� �������� ���������
                            ref CProvinceCore neighbourFreePC = ref pCPool.Value.Get(neighbourFreeProvinceEntity);

                            //������� ��� �� ��������� ������ �������
                            currentMAG.tempProvincePEs.Add(neighbourFreePC.selfPE);

                            //������� PE ������� � ������ ���������
                            neighbourFreePMA.parentMAPE = currentMA.selfPE;

                            //����������� ���������� ������� �������� ��������� ��� ���� ���������, �������� neighbourFreePC
                            //��� ������� ������
                            for (int a = 0; a < neighbourFreePC.neighbourProvincePEs.Length; a++)
                            {
                                //���� �������� ������
                                neighbourFreePC.neighbourProvincePEs[a].Unpack(world.Value, out int neighbourNeighbourProvinceEntity);

                                //����������� ���������� ������� �������� ��������� ��� ����
                                ProvinceMapAreaAddNeighbourWithPMA(
                                    neighbourNeighbourProvinceEntity,
                                    currentMA.selfPE);
                            }

                            addedProvincesCount++;

                            //���� ��������� ����� ��������� ���������� �������, � ������� �� �����
                            if (withoutFreeNeighboursPool.Value.Has(neighbourFreeProvinceEntity) == true
                                && withoutFreeNeighboursPool.Value.Has(mAEntity) == false)
                            {
                                //�������� �������� �������
                                MapAreaCheckProvincesWithoutFreeNeighbours(mAEntity);
                            }
                        }
                    }
                }

                //���� �� ���� �� ���� ��������� �� ����� ���������
                if (addedProvincesCount == 0)
                {
                    //����������� ������� ������
                    errorCount++;
                }

                //���� ������� ������ ������ �������
                if (errorCount >= mapAreaCount)
                {
                    //������� ������ � ��������� ����
                    Debug.LogWarning("���������� ���������� ���������� �������� �����!");

                    break;
                }
            }
        }

        void MapAreaSetNeighbours()
        {
            //��� ������ ���������
            foreach (int provinceEntity in provinceMAFilter.Value)
            {
                //���� ��������� ������� ���������
                ref CTProvinceMapArea pMA = ref pMAPool.Value.Get(provinceEntity);

                //������� �� ������ �������� �������� �������, � ������� ��������� �����������
                pMA.neighbourMAPEs.Remove(pMA.parentMAPE);
            }

            //��� ������ ������� �����
            foreach (int mAEntity in mAGFilter.Value)
            {
                //���� �������
                ref CMapArea mA = ref mAPool.Value.Get(mAEntity);
                ref CTMapAreaGeneration mAG = ref mAGPool.Value.Get(mAEntity);

                //��� ������ ��������� �������
                for (int a = 0; a < mAG.tempProvincePEs.Count; a++)
                {
                    //���� ��������� ������� ���������
                    mAG.tempProvincePEs[a].Unpack(world.Value, out int provinceEntity);
                    ref CTProvinceMapArea pMA = ref pMAPool.Value.Get(provinceEntity);

                    //��� ������ �������� ��������� �������
                    for (int b = 0; b < pMA.neighbourMAPEs.Count; b++)
                    {
                        //���� ����� ������� ��� � ������ �������� �������� ������� �������
                        if (mAG.tempNeighbourMAPEs.Contains(pMA.neighbourMAPEs[b]) == false)
                        {
                            //������� � PE � ������
                            mAG.tempNeighbourMAPEs.Add(pMA.neighbourMAPEs[b]);
                        }
                    }
                }
            }
        }

        void MapAreaAddProvinceWithoutFreeNeighbours(
        int mAEntity)
        {
            //���� ������� �����
            ref CTMapAreaGeneration mAG = ref mAGPool.Value.Get(mAEntity);

            //���� ���������� ��������� ��� ��������� ������� ������ ���������� ���������
            if (mAG.provinceWithoutFreeNeighboursCount < mAG.tempProvincePEs.Count)
            {
                //����������� ���������� ��������� ��� ��������� �������
                mAG.provinceWithoutFreeNeighboursCount++;

                //���� ���������� ��������� ��� ��������� ������� ����� ���������� ���������
                if (mAG.provinceWithoutFreeNeighboursCount >= mAG.tempProvincePEs.Count)
                {
                    //��������� ������� ��������� ���������� �������
                    ref CTWithoutFreeNeighbours mAWFN = ref withoutFreeNeighboursPool.Value.Add(mAEntity);
                }
            }
        }

        void MapAreaCheckProvincesWithoutFreeNeighbours(
            int mAEntity)
        {
            //���� ������� �����
            ref CTMapAreaGeneration mAG = ref mAGPool.Value.Get(mAEntity);

            //������������ ���������� ���������, �� ������� ��������� �������
            int rWFNCount = 0;

            //��� ������ ��������� � �������
            for (int a = 0; a < mAG.tempProvincePEs.Count; a++)
            {
                //���� �������� ���������
                mAG.tempProvincePEs[a].Unpack(world.Value, out int provinceEntity);

                //���� ��������� ����� ��������� ���������� �������
                if (withoutFreeNeighboursPool.Value.Has(provinceEntity) == true)
                {
                    //����������� �������
                    rWFNCount++;
                }
            }

            //��������� ������� � ������ �������
            mAG.provinceWithoutFreeNeighboursCount = rWFNCount;

            //���� ���������� ��������� ��� ��������� ������� ����� ���������� ���������
            if (mAG.provinceWithoutFreeNeighboursCount >= mAG.tempProvincePEs.Count)
            {
                //��������� ������� ��������� ���������� �������
                ref CTWithoutFreeNeighbours mAWFN = ref withoutFreeNeighboursPool.Value.Add(mAEntity);
            }
        }

        void ProvinceMapAreaAddNeighbourWithPMA(
            int provinceEntity,
            EcsPackedEntity neighbourMAPE)
        {
            //���� ���������
            ref CProvinceCore pC = ref pCPool.Value.Get(provinceEntity);
            ref CTProvinceMapArea pMA = ref pMAPool.Value.Get(provinceEntity);

            //���� ���������� �������, ������������� ��������, ������ ���������� �������
            if(pMA.neighboursWithMACount < pC.neighbourProvincePEs.Length)
            {
                //����������� ���������� �������, ������������� ��������
                pMA.neighboursWithMACount++;

                //������� ����� �������� ������� � ������ �������� �������� ���������
                pMA.neighbourMAPEs.Add(neighbourMAPE);

                //���� ���������� �������, ������������� ��������, ����� ���������� �������
                if (pMA.neighboursWithMACount >= pC.neighbourProvincePEs.Length)
                {
                    //��������� ��������� ��������� ���������� ��������� �������
                    ref CTWithoutFreeNeighbours rWFN = ref withoutFreeNeighboursPool.Value.Add(provinceEntity);

                    //���� ��������� ����������� �������
                    if (pMA.parentMAPE.Unpack(world.Value, out int parentMAEntity))
                    {
                        //�� ����������� ���������� ��������� ��� ��������� ������� ��� ���� �������
                        MapAreaAddProvinceWithoutFreeNeighbours(parentMAEntity);
                    }
                }
            }
        }
    }
}