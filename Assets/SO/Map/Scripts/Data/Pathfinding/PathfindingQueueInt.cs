using System.Collections.Generic;

namespace SO.Map.Pathfinding
{
    public class PathfindingQueueInt
    {
        int[] regions;
        public int regionsCount;

        public IComparer<int> Comparer
        {
            get
            {
                return mComparer;
            }
        }
        IComparer<int> mComparer;

        public PathfindingQueueInt(
            IComparer<int> comparer,
            int capacity)
        {
            mComparer = comparer;

            regions = new int[capacity];
            regionsCount = 0;
        }

        void Swap(
            int i, int j)
        {
            int h = regions[i];

            regions[i] = regions[j];

            regions[j] = h;
        }

        int Compare(
            int i, int j)
        {
            return mComparer.Compare(regions[i], regions[j]);
        }

        public int Pop()
        {
            //���� ������ ������ � �������
            int result = regions[0];

            //������ ��������� ����������
            int p = 0, p1, p2, pn;

            //��������� ��������� ������ � ������� �� ����� �������
            int count = regionsCount - 1;
            regions[0] = regions[count];
            regionsCount--;

            //����������
            //������
            do
            {
                //���� � PN ������� ������
                pn = p;

                //���� � P1 ������ 2P + 1
                p1 = 2 * p + 1;

                //���� � P2 ������ 2P + 2
                p2 = p1 + 1;

                //���� ������� ������ P1 � P ������ P1
                if (count > p1 && Compare(p, p1) > 0)
                    //�� ���� � P ������ P1
                    p = p1;

                //���� ������� ������ P2 � P ������ P2
                if (count > p2 && Compare(p, p2) > 0)
                    //�� ���� � P ������ P2
                    p = p2;

                //���� P ����� ������������ �������
                if (p == pn)
                    //������� �� �����
                    break;

                //����� ������ ������� P � PN
                Swap(p, pn);
            }
            //���� ������� 
            while (true);

            return result;
        }

        public int Push(int item)
        {
            //������ ��������� ����������
            int p = regionsCount, p2;

            //������� ���������� ������ �� ��������� �����
            regions[regionsCount] = item;
            regionsCount++;

            //����������
            //������
            do
            {
                //���� ���������� �������� � ������� ���� ����� 0, �� ������ ��� ����� 1 � ���������� �� ���������
                if (p == 0)
                {
                    break;
                }

                //���� ������ (P - 1) / 2
                p2 = (p - 1) / 2;

                //���� ��������� ������� P ������ ���������� P2
                if (Compare(p, p2) < 0)
                {
                    //������ �� �������
                    Swap(p, p2);

                    //���� ������ P2
                    p = p2;
                }
                //����� ������� �� �����
                else
                {
                    break;
                }
            }
            //���� �������
            while (true);

            //���������� ��������� ������
            return p;
        }

        public void Clear()
        {
            regionsCount = 0;
        }
    }
}