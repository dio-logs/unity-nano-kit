using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace IndustrialGeometry
{
    // CAD 정밀도 제어 (mm 단위 도면 기준)
    public static class Tolerance
    {
        public const double Epsilon = 1e-5; // 0.00001
        public static bool Equals(double a, double b) => Math.Abs(a - b) < Epsilon;
        public static bool LessOrEqual(double a, double b) => a < b + Epsilon;
        public static bool GreaterOrEqual(double a, double b) => a > b - Epsilon;
    }

    public enum IntersectionType
    {
        None,
        Crossing,    // X자 교차 (벽체 통과, 에러)
        Corner_L,    // L자 모서리 (정상 연결)
        Junction_T,  // T자 접합 (정상 연결)
        Overlap      // 선분 중첩 (설계 오류)
    }

    public struct Point
    {
        public double X { get; }
        public double Y { get; }
        public Point(double x, double y) { X = x; Y = y; }
        public override string ToString() => $"({X:F3}, {Y:F3})";
        public bool Equals(Point other) => Tolerance.Equals(X, other.X) && Tolerance.Equals(Y, other.Y);

        public Vector2 ToUnityVector2()
        {
            return new Vector2((float)X, (float)Y);
        }
        
        
    }
    

    public class Segment
    {
        public Point P1 { get; }
        public Point P2 { get; }
        
        // AABB (Axis Aligned Bounding Box) 캐싱 - 성능 최적화 핵심
        public double MinX { get; }
        public double MaxX { get; }
        public double MinY { get; }
        public double MaxY { get; }

        public Segment(Point p1, Point p2)
        {
            P1 = p1; P2 = p2;
            MinX = Math.Min(p1.X, p2.X); MaxX = Math.Max(p1.X, p2.X);
            MinY = Math.Min(p1.Y, p2.Y); MaxY = Math.Max(p1.Y, p2.Y);
        }
    }

    public struct IntersectionResult
    {
        public Point Point { get; }
        public Segment Segment1 { get; }
        public Segment Segment2 { get; }
        public IntersectionType Type { get; }

        public IntersectionResult(
            Point point
            , Segment segment1
            , Segment segment2
            , IntersectionType type)
        {
            Point = point;
            Segment1 = segment1;
            Segment2 = segment2;
            Type = type;
        }
    }

    public class IntersectionSolver
    {
        public List<IntersectionResult> FindIntersections(IEnumerable<Segment> segments, Segment target)
        {
            var results = new List<IntersectionResult>();

            foreach (var other in segments)
            {
                // 자기 자신과의 비교는 건너뜀 (참조 비교)
                if (ReferenceEquals(target, other)) continue;

                CheckAndAdd(target, other, results);
            }

            return results;
        }
        
        public List<IntersectionResult> FindIntersections(IEnumerable<Segment> rawLines)
        {
            var segments = rawLines.ToList();
            var results = new List<IntersectionResult>();

            for (int i = 0; i < segments.Count; i++)
            {
                for (int j = i + 1; j < segments.Count; j++)
                {
                    CheckAndAdd(segments[i], segments[j], results);
                }
            }
            return results;
        }
        
        public List<IntersectionResult> FindIntersections(IEnumerable<Tuple<Point, Point>> rawLines)
        {
            var segments = rawLines.Select((t, i) => new Segment(t.Item1, t.Item2)).ToList();
            return FindIntersections(segments);
        }
        
        
        
        private void CheckAndAdd(Segment s1, Segment s2, List<IntersectionResult> results)
        {
            // 1단계: Bounding Box 검사 (빠른 기각)
            if (s1.MaxX < s2.MinX - Tolerance.Epsilon || s1.MinX > s2.MaxX + Tolerance.Epsilon ||
                s1.MaxY < s2.MinY - Tolerance.Epsilon || s1.MinY > s2.MaxY + Tolerance.Epsilon)
            {
                return;
            }

            // 2단계: 정밀 기하 교차 검사
            var result = CalculateIntersection(s1, s2);
            if (result.type != IntersectionType.None)
            {
                results.Add(new IntersectionResult(result.pt, s1, s2, result.type));
            }
        }

        private (Point pt, IntersectionType type) CalculateIntersection(Segment s1, Segment s2)
        {
            double x1 = s1.P1.X, y1 = s1.P1.Y, x2 = s1.P2.X, y2 = s1.P2.Y;
            double x3 = s2.P1.X, y3 = s2.P1.Y, x4 = s2.P2.X, y4 = s2.P2.Y;

            // 외적(Cross Product)을 이용한 분모 계산
            double denom = (y4 - y3) * (x2 - x1) - (x4 - x3) * (y2 - y1);

            // 1. 평행한 경우 (분모가 0)
            if (Math.Abs(denom) < Tolerance.Epsilon)
            {
                // 중첩(Overlap) 검사는 여기서 수행 (인테리어 벽체 겹침 등)
                // 필요하다면 구현 추가 가능, 여기서는 생략하고 None 리턴
                return (new Point(), IntersectionType.None);
            }

            double ua = ((x4 - x3) * (y1 - y3) - (y4 - y3) * (x1 - x3)) / denom;
            double ub = ((x2 - x1) * (y1 - y3) - (y2 - y1) * (x1 - x3)) / denom;

            // 2. 선분 내부 포함 여부 확인 (0 <= u <= 1)
            // Tolerance를 사용하여 끝점 접촉(모서리)을 확실하게 포함
            if (Tolerance.GreaterOrEqual(ua, 0) && Tolerance.LessOrEqual(ua, 1) &&
                Tolerance.GreaterOrEqual(ub, 0) && Tolerance.LessOrEqual(ub, 1))
            {
                double ix = x1 + ua * (x2 - x1);
                double iy = y1 + ua * (y2 - y1);
                Point ip = new Point(ix, iy);

                // 3. 교차 타입 분류
                bool isEnd1 = ip.Equals(s1.P1) || ip.Equals(s1.P2);
                bool isEnd2 = ip.Equals(s2.P1) || ip.Equals(s2.P2);

                if (isEnd1 && isEnd2) return (ip, IntersectionType.Corner_L); // 끝점끼리 만남 (모서리)
                if (isEnd1 || isEnd2) return (ip, IntersectionType.Junction_T); // 하나만 끝점 (T자)
                
                return (ip, IntersectionType.Crossing); // 순수 교차 (X자)
            }

            return (new Point(), IntersectionType.None);
        }
    }
}