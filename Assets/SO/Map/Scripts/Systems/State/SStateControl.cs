
using System.Collections.Generic;

using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Map.MapArea;
using SO.Map.Events.State;
using SO.Map.Events.Province;
using SO.Country;

namespace SO.Map.State
{
    public class SStateControl : IEcsRunSystem
    {
        //����
        readonly EcsWorldInject world = default;


        //�����
        readonly EcsPoolInject<CMapArea> mAPool = default;

        readonly EcsPoolInject<CState> statePool = default;

        //������
        readonly EcsPoolInject<CCountry> countryPool = default;

        public void Run(IEcsSystems systems)
        {
            //�������� �������� �������� �� ������
            StatesStartCreating();

            //����� ���������� ��������
            StatesChangeOwner();
        }

        readonly EcsFilterInject<Inc<RStateStartCreating>> stateStartCreatingRequestFilter = default;
        readonly EcsPoolInject<RStateStartCreating> stateStartCreatingRequestPool = default;
        void StatesStartCreating()
        {
            //��� ������� ������� ���������� �������� �������
            foreach (int requestEntity in stateStartCreatingRequestFilter.Value)
            {
                //���� ������
                ref RStateStartCreating requestComp = ref stateStartCreatingRequestPool.Value.Get(requestEntity);

                //���� ������������ ���� �����
                requestComp.parentMAPE.Unpack(world.Value, out int mAEntity);
                ref CMapArea mA = ref mAPool.Value.Get(mAEntity);

                //������ ����� �������
                StateCreating(
                    ref mA, 
                    out int stateEntity);

                //���� �������
                ref CState state = ref statePool.Value.Get(stateEntity);

                //��������� � ������� ��� ���������
                StateAllProvinceAdd(
                    ref state,
                    ref mA);

                //����
                //������ �������� ������ ��������� � �������
                for (int a = 0; a < 5; a++)
                {
                    //������ ����� ������ �������� ����
                    Population.Events.DROrderedPopulation orderedPOP = new(
                        state.selfPE,
                        0,
                        100);

                    //������� ��� � ������ ���������� �����
                    state.orderedPOPs.Add(orderedPOP);
                }
                //����

                stateStartCreatingRequestPool.Value.Del(requestEntity);
            }
        }

        readonly EcsFilterInject<Inc<RStateChangeOwner>> stateChangeOwnerRequestFilter = default;
        readonly EcsPoolInject<RStateChangeOwner> stateChangeOwnerRequestPool = default;
        void StatesChangeOwner()
        {
            //��� ������� ������� ����� ��������� �������
            foreach(int requestEntity in stateChangeOwnerRequestFilter.Value)
            {
                //���� ������
                ref RStateChangeOwner requestComp = ref stateChangeOwnerRequestPool.Value.Get(requestEntity);

                //���� �������
                requestComp.statePE.Unpack(world.Value, out int stateEntity);
                ref CState state = ref statePool.Value.Get(stateEntity);

                //������ ��������� �������
                StateChangeOwner(
                    ref state,
                    ref requestComp);

                stateChangeOwnerRequestPool.Value.Del(requestEntity);
            }
        }

        void StateCreating(
            ref CMapArea mA,
            out int stateEntity,
            EcsPackedEntity ownerCountryPE = new())
        {
            //������ ����� �������� � ��������� �� ��������� �������
            stateEntity = world.Value.NewEntity();
            ref CState state = ref statePool.Value.Add(stateEntity);

            //��������� �������� ������ �������
            state = new(
                world.Value.PackEntity(stateEntity),
                mA.selfPE,
                ownerCountryPE);

            //������ ����� ��������� ��� �������� ������ ������� � ���� �����
            DState stateData = new(
                state.selfPE,
                ownerCountryPE);

            //������� � � ������ � ������������ ����
            mA.states.Add(stateData);

            //���� PE ��������� �� �����
            if(ownerCountryPE.Unpack(world.Value, out int countryEntity))
            {
                //���� ������-���������
                ref CCountry country = ref countryPool.Value.Get(countryEntity);

                //������� ������� � ������ �������� ������
                country.ownedStatePEs.Add(state.selfPE);
            }

            //������ �������, ���������� � �������� ����� �������
            ObjectNewCreatedEvent(state.selfPE, ObjectNewCreatedType.State);
        }

        void StateChangeOwner(
            ref CState oldState,
            ref RStateChangeOwner requestComp)
        {
            //���� ����� ��������� ���������� ��� �������������
            if(requestComp.requestType == StateChangeOwnerType.Initialization)
            {
                StateChangeOwnerInitialization();
            }

            //���� ������������ ���� �����
            oldState.parentMAPE.Unpack(world.Value, out int mAEntity);
            ref CMapArea mA = ref mAPool.Value.Get(mAEntity);

            //����������, ���������� �� ��� � ���� �������, ������������� ������� ������
            bool isNewStateExist = false;

            //���������� ��� �������� ������� �������
            int newStateEntity = -1;

            //��� ������ ������� � ����
            for(int a = 0; a < mA.states.Count; a++)
            {
                //���� ������� ����������� ������� ������
                if (mA.states[a].ownerCountryPE.EqualsTo(requestComp.newOwnerCountryPE))
                {
                    //���� �������� ������� �������
                    mA.states[a].statePE.Unpack(world.Value, out newStateEntity);

                    //���������, ��� ���� ����������
                    isNewStateExist = true;

                    break;
                }
            }

            //���� ������� ������� �� ����������
            if(isNewStateExist == false)
            {
                //������ ����� �������
                StateCreating(
                    ref mA,
                    out newStateEntity,
                    requestComp.newOwnerCountryPE);
            }

            //���� ������� �������
            ref CState newState = ref statePool.Value.Get(newStateEntity);


            //����������� ����� ��������� ���� ���������, �� ������� ������� �������� �������
            StateAllProvincesChangeOwner(
                ref oldState, ref newState);

            //��������� ����������� �������� ���������� �������� ���������,
            //������� ����� ��������� ������ ���������� �� ������ ���������,
            //����� ���� �������� ���������
        }

        void StateChangeOwnerInitialization()
        {

        }

        void StateAllProvincesChangeOwner(
            ref CState oldState, ref CState newState)
        {
            //������ ������ ��������� ��� ��������
            List<EcsPackedEntity> provincePEs = new();

            //��� ������ ��������� � �������� ������� � �������� �������
            for(int a = oldState.provincePEs.Count - 1; a >= 0; a--)
            {
                //������� ��������� � ������ �����������
                provincePEs.Add(oldState.provincePEs[a]);
            }

            //����������� ����� ��������� ���������
            ProvinceChangeOwnerRequest(
                newState.ownerCountryPE, newState.selfPE,
                oldState.ownerCountryPE,
                provincePEs,
                ProvinceChangeOwnerType.Test);
        }

        /// <summary>
        /// ���������� ������ ������ ��� ������ ����, ����� ������ ��������� �������� �������
        /// </summary>
        /// <param name="state"></param>
        /// <param name="parentMAPE"></param>
        void StateAllProvinceAdd(
            ref CState state,
            ref CMapArea mA)
        {
            //������ ������ ���������
            List<EcsPackedEntity> provincePEs = new();

            //��� ������ ��������� � ����
            for(int a = 0; a < mA.provincePEs.Length; a++)
            {
                //������� ��������� � ������ �����������
                provincePEs.Add(mA.provincePEs[a]);
            }

            //����������� ����� ��������� ���������
            ProvinceChangeOwnerRequest(
                state.selfPE,
                provincePEs,
                ProvinceChangeOwnerType.Initialization);
        }



        readonly EcsPoolInject<RProvinceChangeOwner> provinceChangeOwnerRequestPool = default;
        void ProvinceChangeOwnerRequest(
            EcsPackedEntity newOwnerCountryPE, EcsPackedEntity statePE,
            EcsPackedEntity oldOwnerCountryPE,
            List<EcsPackedEntity> provincePEs,
            ProvinceChangeOwnerType requestType)
        {
            //������ ����� �������� � ��������� �� ������ ����� ��������� ���������
            int requestEntity = world.Value.NewEntity();
            ref RProvinceChangeOwner requestComp = ref provinceChangeOwnerRequestPool.Value.Add(requestEntity);

            //��������� ������ �������
            requestComp = new(
                newOwnerCountryPE, statePE,
                oldOwnerCountryPE,
                provincePEs,
                requestType);
        }

        void ProvinceChangeOwnerRequest(
            EcsPackedEntity statePE,
            List<EcsPackedEntity> provincePEs,
            ProvinceChangeOwnerType requestType)
        {
            ProvinceChangeOwnerRequest(
                new(), statePE,
                new(),
                provincePEs,
                requestType);
        }

        readonly EcsPoolInject<EObjectNewCreated> objectNewCreatedEventPool = default;
        void ObjectNewCreatedEvent(
            EcsPackedEntity objectPE, ObjectNewCreatedType objectType)
        {
            //������ ����� �������� � ��������� �� ������� �������� ������ �������
            int eventEntity = world.Value.NewEntity();
            ref EObjectNewCreated eventComp = ref objectNewCreatedEventPool.Value.Add(eventEntity);

            //��������� ������ �������
            eventComp = new(objectPE, objectType);
        }
    }
}