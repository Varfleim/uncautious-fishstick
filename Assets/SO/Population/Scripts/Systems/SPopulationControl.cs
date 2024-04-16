
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Map.Economy;
using SO.Map.Province;
using SO.Population.Events;

namespace SO.Population
{
    public class SPopulationControl : IEcsRunSystem
    {
        //����
        readonly EcsWorldInject world = default;


        //�����
        readonly EcsFilterInject<Inc<CProvinceEconomy>> provinceFilter = default;
        readonly EcsPoolInject<CProvinceEconomy> pEPool = default;

        //���������
        readonly EcsPoolInject<CPopulation> pOPPool = default;

        public void Run(IEcsSystems systems)
        {
            //�������� ����� ���������
            PopulationCreating();
        }

        void PopulationCreating()
        {
            //��� ������ ���������
            foreach (int currentProvinceEntity in provinceFilter.Value)
            {
                //���� ���������
                ref CProvinceEconomy currentPE = ref pEPool.Value.Get(currentProvinceEntity);

                //���� � ��������� ���� ���������� ������ ���������
                for (int a = 0; a < currentPE.orderedPOPs.Count; a++)
                {
                    //���� ���������� ���
                    DROrderedPopulation orderedPOP = currentPE.orderedPOPs[a];

                    //���������� ���������, � ������� �������� ���
                    int targetProvinceEntity;

                    //���� � ���� ��� ������� ��������� 
                    if (orderedPOP.targetProvincePE.Unpack(world.Value, out targetProvinceEntity) == false)
                    {
                        //��� �������� � ������� ���������

                        //��������� � �������� ������� �������
                        targetProvinceEntity = currentProvinceEntity;
                    }
                    //����� ��� �������� � ������� ���������

                    //���� ������� ���������
                    ref CProvinceEconomy targetPE = ref pEPool.Value.Get(targetProvinceEntity);

                    //���� � ��������� ��� ���� ���������� ���
                    if (FindIdenticalPopulation(
                        ref targetPE,
                        ref orderedPOP,
                        out int oldPOPEntity))
                    {
                        //���� ���
                        ref CPopulation oldPOP = ref pOPPool.Value.Get(oldPOPEntity);

                        //��������� � ���� ���������
                        PopulationAddToOld(
                            ref orderedPOP,
                            ref oldPOP);
                    }
                    //�����
                    else
                    {
                        //������ ����� ��� � ���������
                        PopulationCreateNew(
                            ref orderedPOP,
                            ref targetPE);
                    }
                }
                currentPE.orderedPOPs.Clear();
            }
        }

        bool FindIdenticalPopulation(
            ref CProvinceEconomy pE,
            ref DROrderedPopulation orderedPOP,
            out int oldPOPEntity)
        {
            //����� �������������� ������
            oldPOPEntity = -1;

            //��� ������ ������ ��������� � ���������
            for (int a = 0; a < pE.pOPs.Count; a++)
            {
                //���� ���
                pE.pOPs[a].Unpack(world.Value, out oldPOPEntity);
                ref CPopulation oldPOP = ref pOPPool.Value.Get(oldPOPEntity);

                //���� ������������ ��� ��������� ������������
                if(PopulationFunctions.IsPopulationIdentical(
                    ref orderedPOP, 
                    ref oldPOP))
                {
                    //����������, ��� ��� ���������� ���
                    return true;
                }
            }

            //����������, ��� ����������� ���� ���
            return false;
        }

        void PopulationCreateNew(
            ref DROrderedPopulation orderedPOP,
            ref CProvinceEconomy pE)
        {
            //������ ����� �������� � ��������� �� ��������� ����
            int pOPEntity = world.Value.NewEntity();
            ref CPopulation pOP = ref pOPPool.Value.Add(pOPEntity);

            //��������� �������� ������ ����
            pOP = new(
                world.Value.PackEntity(pOPEntity),
                orderedPOP.socialClassIndex);

            //��������� ���� ��� ���������
            pOP.UpdatePopulation(ref orderedPOP);

            //������� PE ���� � ������ ���������
            pE.pOPs.Add(pOP.selfPE);

            //������ �������, ���������� � �������� ������ ����
            ObjectNewCreatedEvent(pOP.selfPE, ObjectNewCreatedType.Population);
        }

        void PopulationAddToOld(
            ref DROrderedPopulation orderedPOP,
            ref CPopulation oldPOP)
        {
            //��������� ���� ����� ���������
            oldPOP.UpdatePopulation(ref orderedPOP);
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