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
            //Берём первую провинцию в очереди
            int result = provinces[0];

            //Создаём временные переменные
            int p = 0, p1, p2, pn;

            //Переносим последнюю провинцию в очереди на место первого
            int count = provincesCount - 1;
            provinces[0] = provinces[count];
            provincesCount--;

            //Сортировка
            //Делаем
            do
            {
                //Берём в PN текущую провинцию
                pn = p;

                //Берём в P1 провинцию 2P + 1
                p1 = 2 * p + 1;

                //Берём в P2 провинцию 2P + 2
                p2 = p1 + 1;

                //Если счётчик больше P1 и P больше P1
                if (count > p1 && Compare(p, p1) > 0)
                    //То берём в P провинцию P1
                    p = p1;

                //Если счётчик больше P2 и P больше P2
                if (count > p2 && Compare(p, p2) > 0)
                    //То берём в P провинцию P2
                    p = p2;

                //Если P равен изначальной провинции
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
            //Создаём временные переменные
            int p = provincesCount, p2;

            //Заносим переданную провинцию на последнее место
            provinces[provincesCount] = item;
            provincesCount++;

            //Сортировка
            //Делаем
            do
            {
                //Если количество провинций в очереди было равно 0, то теперь оно равно 1 и сортировка не требуется
                if (p == 0)
                {
                    break;
                }

                //Берём провинцию (P - 1) / 2
                p2 = (p - 1) / 2;

                //Если приоритет провинции P меньше приоритета P2
                if (Compare(p, p2) < 0)
                {
                    //Меняем их местами
                    Swap(p, p2);

                    //Берём провинцию P2
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

            //Возвращаем последнюю провинцию
            return p;
        }

        public void Clear()
        {
            provincesCount = 0;
        }
    }
}