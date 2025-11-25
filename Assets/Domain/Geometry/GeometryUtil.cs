using System;
using System.Collections.Generic;
using IndustrialGeometry;

namespace BentleyOttmannCS
{
    public static class GeometryUtil
    {
        public static bool IsPointInPolygon(Point p, List<Point> polygon)
        {
            int n = polygon.Count;
            bool inside = false;

            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                Point pi = polygon[i];
                Point pj = polygon[j];

                // 점과 폴리곤의 각 선분 사이 교차 여부 확인
                if (((pi.Y > p.Y) != (pj.Y > p.Y)) &&
                    (p.X < (pj.X - pi.X) * (p.Y - pi.Y) / (pj.Y - pi.Y + 1e-12) + pi.X))
                {
                    inside = !inside;
                }
            }

            return inside;
        }
        
        public static class PointOnSegment
        {
            /// <summary>
            /// 점 p가 선분 AB 위에 있는지 확인
            /// </summary>
            public static bool IsPointOnSegment(Point p, Point a, Point b)
            {
                // 선분이 점 하나짜리인 경우
                if (a.Equals(b))
                    return p.Equals(a);

                // 벡터 AB와 AP 계산
                double cross = (b.X - a.X) * (p.Y - a.Y) - (b.Y - a.Y) * (p.X - a.X);
                if (Math.Abs(cross) > 1e-9) // 교차가 0이면 일직선 상
                    return false;

                // 점이 선분 범위 내에 있는지 확인
                double dot = (p.X - a.X) * (b.X - a.X) + (p.Y - a.Y) * (b.Y - a.Y);
                if (dot < 0) return false;

                double lenSq = (b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y);
                if (dot > lenSq) return false;

                return true;
            }
        }
    }
}