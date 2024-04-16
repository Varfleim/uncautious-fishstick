using System;
using System.Collections.Generic;

using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Country;
using SO.Map.Events;
using SO.Map.Economy;

namespace SO.Map.Province
{
    public class SProvinceControl : IEcsRunSystem
    {
        //����
        readonly EcsWorldInject world = default;


        //�����
        readonly EcsPoolInject<CProvinceCore> pCPool = default;
        readonly EcsPoolInject<CProvinceEconomy> pEPool = default;

        //������
        readonly EcsPoolInject<CCountry> countryPool = default;

        public void Run(IEcsSystems systems)
        {
            //����� ���������� ���������
            ProvinceChangeOwners();
        }

        readonly EcsFilterInject<Inc<RProvinceChangeOwner>> provinceChangeOwnerRequestFilter = default;
        readonly EcsPoolInject<RProvinceChangeOwner> provinceChangeOwnerRequestPool = default;
        void ProvinceChangeOwners()
        {
            //��� ������� ������� ����� ��������� ���������
            foreach (int requestEntity in provinceChangeOwnerRequestFilter.Value)
            {
                //���� ������
                ref RProvinceChangeOwner requestComp = ref provinceChangeOwnerRequestPool.Value.Get(requestEntity);

                //���� ������, ������� ���������� ���������� ���������
                requestComp.countryPE.Unpack(world.Value, out int countryEntity);
                ref CCountry country = ref countryPool.Value.Get(countryEntity);

                //���� ���������
                requestComp.provincePE.Unpack(world.Value, out int provinceEntity);
                ref CProvinceCore pC = ref pCPool.Value.Get(provinceEntity);

                //���� ����� ��������� ���������� ��� �������������
                if (requestComp.requestType == ProvinceChangeOwnerType.Initialization)
                {
                    PCChangeOwnerInitialization();
                }


                //������ �������, ���������� � ����� ��������� ���������
                ProvinceChangeOwnerEvent(
                    pC.selfPE,
                    country.selfPE, pC.ownerCountryPE);


                //��������� ������-��������� ���������
                pC.ownerCountryPE = country.selfPE;

                //����
                //������� PE ��������� � ������ ������
                country.ownedPCPEs.Add(pC.selfPE);

                //���� ������������� ��������� ���������
                ref CProvinceEconomy pE = ref pEPool.Value.Get(provinceEntity);

                //������ �������� ������ ���������
                for(int a = 0; a < 5; a++)
                {
                    //������ ����� ������ �������� ����
                    Population.Events.DROrderedPopulation orderedPOP = new(
                        pE.selfPE,
                        0,
                        100);

                    //������� ��� � ������ ���������� �����
                    pE.orderedPOPs.Add(orderedPOP);
                }
                //����

                provinceChangeOwnerRequestPool.Value.Del(requestEntity);
            }
        }

        void PCChangeOwnerInitialization()
        {

        }

        readonly EcsPoolInject<EProvinceChangeOwner> provinceChangeOwnerEventPool = default;
        void ProvinceChangeOwnerEvent(
            EcsPackedEntity provincePE,
            EcsPackedEntity newOwnerCountryPE, EcsPackedEntity oldOwnerCountryPE = new())
        {
            //������ ����� �������� � ��������� �� ������� ����� ��������� PC
            int eventEntity = world.Value.NewEntity();
            ref EProvinceChangeOwner eventComp = ref provinceChangeOwnerEventPool.Value.Add(eventEntity);

            //��������� ������ �������
            eventComp = new(
                provincePE,
                newOwnerCountryPE, oldOwnerCountryPE);
        }
    }
}