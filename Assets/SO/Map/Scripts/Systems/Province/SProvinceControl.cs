
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Map.State;
using SO.Map.Events;
using SO.Map.Events.Province;

namespace SO.Map.Province
{
    public class SProvinceControl : IEcsRunSystem
    {
        //����
        readonly EcsWorldInject world = default;


        //�����
        readonly EcsPoolInject<CState> statePool = default;

        readonly EcsPoolInject<CProvinceCore> pCPool = default;

        public void Run(IEcsSystems systems)
        {
            //����� ���������� ���������
            ProvincesChangeOwner();
        }

        readonly EcsFilterInject<Inc<RProvinceChangeOwner>> provinceChangeOwnerRequestFilter = default;
        readonly EcsPoolInject<RProvinceChangeOwner> provinceChangeOwnerRequestPool = default;
        void ProvincesChangeOwner()
        {
            //��� ������� ������� ����� ��������� ���������
            foreach (int requestEntity in provinceChangeOwnerRequestFilter.Value)
            {
                //���� ������
                ref RProvinceChangeOwner requestComp = ref provinceChangeOwnerRequestPool.Value.Get(requestEntity);

                //��� ������ ��������� � �������
                for (int a = 0; a < requestComp.provincePEs.Count; a++)
                {
                    //���� ���������
                    requestComp.provincePEs[a].Unpack(world.Value, out int provinceEntity);
                    ref CProvinceCore pC = ref pCPool.Value.Get(provinceEntity);

                    //������ ��������� ���������
                    ProvinceChangeOwner(
                        ref pC,
                        ref requestComp);
                }

                provinceChangeOwnerRequestPool.Value.Del(requestEntity);
            }
        }

        void ProvinceChangeOwner(
            ref CProvinceCore pC,
            ref RProvinceChangeOwner requestComp)
        {
            //���� ����� ��������� ���������� ��� �������������
            if (requestComp.requestType == ProvinceChangeOwnerType.Initialization)
            {
                ProvinceChangeOwnerInitialization();
            }
            //����� 
            else
            {
                //���� �������� �������
                pC.ParentStatePE.Unpack(world.Value, out int oldStateEntity);
                ref CState oldState = ref statePool.Value.Get(oldStateEntity);

                //������� ��������� �� ������ �������� �������
                oldState.RemoveProvinceFromList(pC.selfPE);
            }

            //��������� ����� �������, � ������� ��������� ���������
            pC.SetParentState(requestComp.newStatePE);


            //���� ������� �������
            requestComp.newStatePE.Unpack(world.Value, out int newStateEntity);
            ref CState newState = ref statePool.Value.Get(newStateEntity);

            //������� ��������� � ������ ������� �������
            newState.provincePEs.Add(pC.selfPE);


            //������ �������, ���������� � ����� ��������� ���������
            ProvinceChangeOwnerEvent(
                pC.selfPE,
                requestComp.newOwnerCountryPE, requestComp.oldOwnerCountryPE);
        }

        void ProvinceChangeOwnerInitialization()
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