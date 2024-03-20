
using Leopotam.EcsLite;

namespace SO.Map.Economy
{
    /// <summary>
    /// ���������, �������� ������ � ��������� � ����������� �������
    /// </summary>
    public struct CRegionEconomic
    {
        public CRegionEconomic(
            EcsPackedEntity selfPE)
        {
            this.selfPE = selfPE;
        }

        public readonly EcsPackedEntity selfPE;
    }
}