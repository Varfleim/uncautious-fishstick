
using System.Collections.Generic;

using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Map.Events;
using SO.Map.Hexasphere;
using SO.Map.Region;

namespace SO.Map.Generation
{
    public class SMapRegionInitializerControl : IEcsRunSystem
    {
        //����
        readonly EcsWorldInject world = default;


        //�����
        readonly EcsPoolInject<CRegionHexasphere> rHSPool = default;


        //������� ���������
        readonly EcsFilterInject<Inc<RRegionInitializer>> regionInitializerRequestFilter = default;
        readonly EcsPoolInject<RRegionInitializer> regionInitializerRequestPool = default;
        readonly EcsFilterInject<Inc<RRegionInitializer, RRegionInitializerOwner>> regionInitializerOwnerRequestFilter = default;
        readonly EcsPoolInject<RRegionInitializerOwner> regionInitializerOwnerRequestPool = default;


        //������
        readonly EcsCustomInject<RegionsData> regionsData = default;

        public void Run(IEcsSystems systems)
        {
            //����
            //���������� ������� ��� ��������������� ��������, �� ��� �����������
            MapRegionInitializersRandom();
            //����

            //��������� ���������� ���������������
            MapRegionInitializersOwner();

            //��� ������� ������� ��������������
            foreach (int requestEntity in regionInitializerRequestFilter.Value)
            {
                regionInitializerRequestPool.Value.Del(requestEntity);
            }
        }

        /// <summary>
        /// ����
        /// </summary>
        void MapRegionInitializersRandom()
        {
            //������ ��������� ������� ��� ��������
            //int1 - �������� �������, int2 - �������� ��������������
            Dictionary<int, int> initializedRegions = new();

            //��� ������� ������� ��������������
            foreach (int requestEntity in regionInitializerRequestFilter.Value)
            {
                //���� ������
                ref RRegionInitializer requestComp = ref regionInitializerRequestPool.Value.Get(requestEntity);

                bool isValidRegion = false;

                //���� �� ������ ���������� ������
                while (isValidRegion == false)
                {
                    //���� �������� ���������� �������
                    regionsData.Value.GetRegionRandom().Unpack(world.Value, out int regionEntity);

                    //���� �������� ������� ����������� � �������
                    if (initializedRegions.ContainsKey(regionEntity) == false)
                    {
                        //���� ������
                        ref CRegionHexasphere rHS = ref rHSPool.Value.Get(regionEntity);

                        //��������� �������� ������� � ��������������
                        requestComp.regionPE = rHS.selfPE;

                        //������� ������ � ������������� � �������
                        initializedRegions.Add(regionEntity, requestEntity);

                        //��������, ��� ������ ��� ������
                        isValidRegion = true;
                    }
                }
            }
        }

        void MapRegionInitializersOwner()
        {
            //��� ������� �������������� � ����������� ���������
            foreach (int requestEntity in regionInitializerOwnerRequestFilter.Value)
            {
                //���� �������
                ref RRegionInitializer requestCoreComp = ref regionInitializerRequestPool.Value.Get(requestEntity);
                ref RRegionInitializerOwner requestOwnerComp = ref regionInitializerOwnerRequestPool.Value.Get(requestEntity);

                //����������� ����� ��������� �������
                //RegionChangeOwnerRequest(
                //    requestOwnerComp.ownerCharacterPE,
                //    requestCoreComp.regionPE,
                //    RCChangeOwnerType.Initialization);

                regionInitializerOwnerRequestPool.Value.Del(requestEntity);
            }
        }

        readonly EcsPoolInject<RRegionChangeOwner> regionChangeOwnerRequestPool = default;
        void RegionChangeOwnerRequest(
            EcsPackedEntity characterPE,
            EcsPackedEntity regionPE,
            RegionChangeOwnerType requestType)
        {
            //������ ����� �������� � ��������� �� ������ ����� ��������� �������
            int requestEntity = world.Value.NewEntity();
            ref RRegionChangeOwner requestComp = ref regionChangeOwnerRequestPool.Value.Add(requestEntity);

            //��������� ������ �������
            requestComp = new(
                characterPE,
                regionPE,
                requestType);
        }
    }
}