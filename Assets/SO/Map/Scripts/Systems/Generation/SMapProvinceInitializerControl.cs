
using System.Collections.Generic;

using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Map.Events;
using SO.Map.Hexasphere;
using SO.Map.Province;

namespace SO.Map.Generation
{
    public class SMapProvinceInitializerControl : IEcsRunSystem
    {
        //����
        readonly EcsWorldInject world = default;


        //�����
        readonly EcsPoolInject<CProvinceHexasphere> pHSPool = default;


        //������� ���������
        readonly EcsFilterInject<Inc<RProvinceInitializer>> provinceInitializerRequestFilter = default;
        readonly EcsPoolInject<RProvinceInitializer> provinceInitializerRequestPool = default;
        readonly EcsFilterInject<Inc<RProvinceInitializer, RProvinceInitializerOwner>> provinceInitializerOwnerRequestFilter = default;
        readonly EcsPoolInject<RProvinceInitializerOwner> provinceInitializerOwnerRequestPool = default;


        //������
        readonly EcsCustomInject<ProvincesData> provincesData = default;

        public void Run(IEcsSystems systems)
        {
            //����
            //���������� ������� ��� ��������������� ��������, �� ��� �����������
            MapProvinceInitializersRandom();
            //����

            //��������� ���������� ���������������
            MapProvinceInitializersOwner();

            //��� ������� ������� ��������������
            foreach (int requestEntity in provinceInitializerRequestFilter.Value)
            {
                provinceInitializerRequestPool.Value.Del(requestEntity);
            }
        }

        /// <summary>
        /// ����
        /// </summary>
        void MapProvinceInitializersRandom()
        {
            //������ ��������� ������� ��� ���������
            //int1 - �������� ���������, int2 - �������� ��������������
            Dictionary<int, int> initializedProvinces = new();

            //��� ������� ������� ��������������
            foreach (int requestEntity in provinceInitializerRequestFilter.Value)
            {
                //���� ������
                ref RProvinceInitializer requestComp = ref provinceInitializerRequestPool.Value.Get(requestEntity);

                bool isValidProvince = false;

                //���� �� ������� ���������� ���������
                while (isValidProvince == false)
                {
                    //���� �������� ���������
                    provincesData.Value.GetProvinceRandom().Unpack(world.Value, out int provinceEntity);

                    //���� �������� ����������� � �������
                    if (initializedProvinces.ContainsKey(provinceEntity) == false)
                    {
                        //���� ���������
                        ref CProvinceHexasphere pHS = ref pHSPool.Value.Get(provinceEntity);

                        //��������� �������� ��������� � ��������������
                        requestComp.provincePE = pHS.selfPE;

                        //������� ��������� � ������������� � �������
                        initializedProvinces.Add(provinceEntity, requestEntity);

                        //��������, ��� ��������� ���� �������
                        isValidProvince = true;
                    }
                }
            }
        }

        void MapProvinceInitializersOwner()
        {
            //��� ������� �������������� � ����������� ���������
            foreach (int requestEntity in provinceInitializerOwnerRequestFilter.Value)
            {
                //���� �������
                ref RProvinceInitializer requestCoreComp = ref provinceInitializerRequestPool.Value.Get(requestEntity);
                ref RProvinceInitializerOwner requestOwnerComp = ref provinceInitializerOwnerRequestPool.Value.Get(requestEntity);

                //����������� ����� ��������� ���������
                //ProvinceChangeOwnerRequest(
                //    requestOwnerComp.ownerCountryPE,
                //    requestCoreComp.provincePE,
                //    PCChangeOwnerType.Initialization);

                provinceInitializerOwnerRequestPool.Value.Del(requestEntity);
            }
        }

        readonly EcsPoolInject<RProvinceChangeOwner> provinceChangeOwnerRequestPool = default;
        void ProvinceChangeOwnerRequest(
            EcsPackedEntity countryPE,
            EcsPackedEntity provincePE,
            ProvinceChangeOwnerType requestType)
        {
            //������ ����� �������� � ��������� �� ������ ����� ��������� ���������
            int requestEntity = world.Value.NewEntity();
            ref RProvinceChangeOwner requestComp = ref provinceChangeOwnerRequestPool.Value.Add(requestEntity);

            //��������� ������ �������
            requestComp = new(
                countryPE,
                provincePE,
                requestType);
        }
    }
}