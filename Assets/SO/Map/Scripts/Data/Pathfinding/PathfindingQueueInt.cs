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
            //Берём первый регион в очереди
            int result = regions[0];

            //Создаём временные переменные
            int p = 0, p1, p2, pn;

            //Переносим последний регион в очереди на место первого
            int count = regionsCount - 1;
            regions[0] = regions[count];
            regionsCount--;

            //Сортировка
            //Делаем
            do
            {
                //Берём в PN текущий регион
                pn = p;

                //Берём в P1 регион 2P + 1
                p1 = 2 * p + 1;

                //Берём в P2 регион 2P + 2
                p2 = p1 + 1;

                //Если счётчик больше P1 и P больше P1
                if (count > p1 && Compare(p, p1) > 0)
                    //То берём в P регион P1
                    p = p1;

                //Если счётчик больше P2 и P больше P2
                if (count > p2 && Compare(p, p2) > 0)
                    //То берём в P регион P2
                    p = p2;

                //Если P равен изначальному региону
                if (p == pn)
                    //Выходим из цикла
                    break;

                //Иначе меняем местами P и PN
                Swap(p, pn);
            }
            //Пока истинно 
            while (true);

            return result;
        }

        public int Push(int item)
        {
            //Создаём временный переменные
            int p = regionsCount, p2;

            //Заносим переданный регион на последнее место
            regions[regionsCount] = item;
            regionsCount++;

            //Сортировка
            //Делаем
            do
            {
                //Если количество регионов в очереди было равно 0, то теперь оно равно 1 и сортировка не требуется
                if (p == 0)
                {
                    break;
                }

                //Берём регион (P - 1) / 2
                p2 = (p - 1) / 2;

                //Если приоритет региона P меньше приоритета P2
                if (Compare(p, p2) < 0)
                {
                    //Меняем их местами
                    Swap(p, p2);

                    //Берём регион P2
                    p = p2;
                }
                //Иначе выходим из цикла
                else
                {
                    break;
                }
            }
            //Пока истинно
            while (true);

            //Возвращаем последний регион
            return p;
        }

        public void Clear()
        {
            regionsCount = 0;
        }
    }
}