
using System;
using System.Collections.Generic;

using UnityEngine;

using Leopotam.EcsLite;

namespace SO.Map.Hexasphere
{
    public class DHexaspherePoint : IEqualityComparer<DHexaspherePoint>, IEquatable<DHexaspherePoint>
    {
        public DHexaspherePoint(
            float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public float x;
        public float y;
        public float z;

        public float Elevation
        {
            get
            {
                return elevation;
            }
            set
            {
                if (elevation != value)
                {
                    elevation = value;

                    isProjectedVector3 = false;
                }
            }
        }
        float elevation;

        public Vector3 ProjectedVector3
        {
            get
            {
                //���� ������ ���������, �� ���������� ���
                if (isProjectedVector3 == true)
                {
                    return projectedVector3;
                }
                //����� ������������ ��� � ����������
                else
                {
                    CalculateProjectedVertex();

                    return projectedVector3;
                }
            }
        }
        Vector3 projectedVector3;
        bool isProjectedVector3 = false;

        public DHexasphereTriangle[] triangles;
        public int triangleCount;
        public EcsPackedEntity regionPE;

        public static int flag = 0;

        int hashCode;

        void CalculateProjectedVertex()
        {
            double len = 2.0 * Math.Sqrt(x * (double)x + y * (double)y + z * (double)z);
            len /= 1.0 + elevation;

            double xx = x / len;
            double yy = y / len;
            double zz = z / len;

            projectedVector3 = new Vector3((float)xx, (float)yy, (float)zz);

            isProjectedVector3 = true;
        }

        public void RegisterTriangle(
            DHexasphereTriangle triangle)
        {
            //���� ������ ������������� ����, ������ ����� �� ����� �������������
            if (triangles == null)
            {
                triangles = new DHexasphereTriangle[6];
            }

            //������� ����������� � ������ �� ��������� ��������� �����
            triangles[triangleCount++] = triangle;
        }

        public int GetOrderedTriangles(DHexasphereTriangle[] tempTriangles)
        {
            //���� ���������� ������������� ����� ����
            if (triangleCount == 0)
            {
                return 0;
            }

            tempTriangles[0] = triangles[0];

            //������ ������� �������������
            int count = 1;

            flag++;

            //��� ������� ������������, ����� ����������
            for (int a = 0; a < triangleCount - 1; a++)
            {
                //��� ������� ������������
                for (int b = 1; b < triangleCount; b++)
                {
                    //���� ���� ������������ �� ����� �������� ����� �����
                    if (triangles[b].orderedFlag != flag
                        //���� ������� ��������� ����������� �� ����
                        && tempTriangles[a] != null
                        //���� ������������ �������
                        && triangles[b].IsAdjacentTo(tempTriangles[a]))
                    {
                        //������� ����������� � ������ ��������� � ����������� �������
                        tempTriangles[count++] = triangles[b];

                        //��������� ���� ������������
                        triangles[b].orderedFlag = flag;

                        //������� �� �����
                        break;
                    }
                }
            }

            //���������� ���������� �������������
            return count;
        }

        public override string ToString()
        {
            return (int)(x * 100f) / 100f + "," + (int)(y * 100f) / 100f + "," + (int)(z * 100f) / 100f;

        }

        public override bool Equals(object obj)
        {
            if (obj is DHexaspherePoint)
            {
                DHexaspherePoint other = (DHexaspherePoint)obj;
                return x == other.x && y == other.y && z == other.z;
            }
            return false;
        }

        public bool Equals(DHexaspherePoint p2)
        {
            return x == p2.x && y == p2.y && z == p2.z;
        }

        public bool Equals(DHexaspherePoint p1, DHexaspherePoint p2)
        {
            return p1.x == p2.x && p1.y == p2.y && p1.z == p2.z;
        }

        public override int GetHashCode()
        {
            if (hashCode == 0)
            {
                hashCode = x.GetHashCode() ^ y.GetHashCode() << 2 ^ z.GetHashCode() >> 2;
            }
            return hashCode;
        }

        public int GetHashCode(DHexaspherePoint p)
        {
            if (hashCode == 0)
            {
                hashCode = p.x.GetHashCode() ^ p.y.GetHashCode() << 2 ^ p.z.GetHashCode() >> 2;
            }
            return hashCode;
        }

        public static explicit operator Vector3(DHexaspherePoint point)
        {
            return new Vector3(point.x, point.y, point.z);
        }
    }
}