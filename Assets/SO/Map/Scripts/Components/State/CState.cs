
using System.Collections.Generic;

using Leopotam.EcsLite;

using SO.Population.Events;

namespace SO.Map.State
{
    public struct CState
    {
        public CState(
            EcsPackedEntity selfPE,
            EcsPackedEntity parentMAPE,
            EcsPackedEntity ownerCountryPE)
        {
            this.selfPE = selfPE;

            this.parentMAPE = parentMAPE;

            this.ownerCountryPE = ownerCountryPE;

            provincePEs = new();

            pOPPEs = new();
            orderedPOPs = new();
        }

        public readonly EcsPackedEntity selfPE;

        public readonly EcsPackedEntity parentMAPE;

        #region CountryData
        public readonly EcsPackedEntity ownerCountryPE;
        #endregion

        #region ProvincesData
        public List<EcsPackedEntity> provincePEs;

        /// <summary>
        /// ����� ����� �������� ������ � ���������� ������� ��������� ��� ��������
        /// </summary>
        /// <param name="provincePE"></param>
        public void RemoveProvinceFromList(
            EcsPackedEntity provincePE)
        {
            //���������� ������ ��������� � ������
            int provinceIndex = -1;

            //��� ������ ��������� � �������� �������
            for(int a = provincePEs.Count - 1; a >= 0; a--)
            {
                //���� ��������� - ��� �������
                if (provincePEs[a].EqualsTo(provincePE))
                {
                    //��������� ������
                    provinceIndex = a;

                    break;
                }
            }

            //������� ��������� �� �������
            provincePEs.RemoveAt(provinceIndex);
        }
        #endregion

        #region PopulationData
        public List<EcsPackedEntity> pOPPEs;
        public List<DROrderedPopulation> orderedPOPs;
        #endregion
    }
}