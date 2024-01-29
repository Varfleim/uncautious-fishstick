
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace SO.Map
{
    public class DHexasphereTriangle
    {
        public DHexasphereTriangle(
            DHexaspherePoint point1, DHexaspherePoint point2, DHexaspherePoint point3,
            bool register = true)
        {
            points = new DHexaspherePoint[]
            {
                point1,
                point2,
                point3
            };

            if (register)
            {
                point1.RegisterTriangle(this);
                point2.RegisterTriangle(this);
                point3.RegisterTriangle(this);
            }
        }

        public DHexaspherePoint[] points;
        public int orderedFlag;

        DHexaspherePoint centroid;
        bool isCentroid;

        public bool IsAdjacentTo(
            DHexasphereTriangle triangle2)
        {
            bool match = false;

            //��� ������ �����
            for (int a = 0; a < 3; a++)
            {
                //���� ����� ������� ������������
                DHexaspherePoint point1 = points[a];

                //��� ������ �����
                for (var b = 0; b < 3; b++)
                {
                    //���� ����� ������� ������������
                    DHexaspherePoint point2 = triangle2.points[b];

                    //���� ���������� ���������
                    if (point1.x == point2.x && point1.y == point2.y && point1.z == point2.z)
                    {
                        //���� ��� ���� ����������
                        if (match)
                        {
                            //�� ������������ �������
                            return true;
                        }

                        //���������, ��� ���� ����������
                        match = true;
                    }
                }
            }

            //������������ �� �������
            return false;
        }

        public DHexaspherePoint GetCentroid()
        {
            if (isCentroid == true)
            {
                return centroid;
            }

            isCentroid = true;

            float x = (points[0].x + points[1].x + points[2].x) / 3;
            float y = (points[0].y + points[1].y + points[2].y) / 3;
            float z = (points[0].z + points[1].z + points[2].z) / 3;

            centroid = new DHexaspherePoint(x, y, z);

            return centroid;
        }
    }
}