using System.Collections.Generic;

namespace SO.Map.Pathfinding
{
    public class PathfindingQueueInt
    {
        int[] provinces;
        public int provincesCount;

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

            provinces = new int[capacity];
            provincesCount = 0;
        }

        void Swap(
            int i, int j)
        {
            int h = provinces[i];

            provinces[i] = provinces[j];

            provinces[j] = h;
        }

        int Compare(
            int i, int j)
        {
            return mComparer.Compare(provinces[i], provinces[j]);
        }

        public int Pop()
        {
            //���� ������ ��������� � �������
            int result = provinces[0];

            //������ ��������� ����������
            int p = 0, p1, p2, pn;

            //��������� ��������� ��������� � ������� �� ����� �������
            int count = provincesCount - 1;
            provinces[0] = provinces[count];
            provincesCount--;

            //����������
            //������
            do
            {
                //���� � PN ������� ���������
                pn = p;

                //���� � P1 ��������� 2P + 1
                p1 = 2 * p + 1;

                //���� � P2 ��������� 2P + 2
                p2 = p1 + 1;

                //���� ������� ������ P1 � P ������ P1
                if (count > p1 && Compare(p, p1) > 0)
                    //�� ���� � P ��������� P1
                    p = p1;

                //���� ������� ������ P2 � P ������ P2
                if (count > p2 && Compare(p, p2) > 0)
                    //�� ���� � P ��������� P2
                    p = p2;

                //���� P ����� ����������� ���������
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
            int p = provincesCount, p2;

            //������� ���������� ��������� �� ��������� �����
            provinces[provincesCount] = item;
            provincesCount++;

            //����������
            //������
            do
            {
                //���� ���������� ��������� � ������� ���� ����� 0, �� ������ ��� ����� 1 � ���������� �� ���������
                if (p == 0)
                {
                    break;
                }

                //���� ��������� (P - 1) / 2
                p2 = (p - 1) / 2;

                //���� ��������� ��������� P ������ ���������� P2
                if (Compare(p, p2) < 0)
                {
                    //������ �� �������
                    Swap(p, p2);

                    //���� ��������� P2
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

            //���������� ��������� ���������
            return p;
        }

        public void Clear()
        {
            provincesCount = 0;
        }
    }
}