using System;
using System.Collections.Generic;
using System.Linq;
using IndustrialGeometry;

namespace Domain.Geometry
{
   public class RobustPointComparer : IEqualityComparer<Point>
    {
        private const double Precision = 10000.0; // 0.0001 수준에서 스냅

        public bool Equals(Point p1, Point p2)
        {
            return Math.Abs(p1.X - p2.X) < Tolerance.Epsilon && 
                   Math.Abs(p1.Y - p2.Y) < Tolerance.Epsilon;
        }

        public int GetHashCode(Point p)
        {
            // 정수 스냅핑
            long x = (long)Math.Round(p.X * Precision);
            long y = (long)Math.Round(p.Y * Precision);
            return (x, y).GetHashCode();
        }
    }

    public class LoopResult
    {
        public List<Point> Vertices { get; }
        public double Area { get; }
        public bool IsClockwise { get; }

        public LoopResult(List<Point> vertices)
        {
            Vertices = vertices;
            Area = CalculateSignedArea(vertices);
            IsClockwise = Area < 0; // 일반 수학 좌표계 기준 (스크린 좌표계면 반대)
        }

        // 신발끈 공식
        private double CalculateSignedArea(List<Point> polygon)
        {
            double area = 0;
            for (int i = 0; i < polygon.Count; i++)
            {
                var p1 = polygon[i];
                var p2 = polygon[(i + 1) % polygon.Count];
                area += (p1.X * p2.Y - p2.X * p1.Y);
            }
            return area * 0.5;
        }
    }

    public class ClosedLoopSolver
    {
        private class DirectedEdge
        {
            public Point From { get; }
            public Point To { get; }
            public double Angle { get; }
            public bool Visited { get; set; } = false;

            public DirectedEdge(Point from, Point to)
            {
                From = from;
                To = to;
                // Atan2는 -PI ~ PI 반환
                Angle = Math.Atan2(to.Y - from.Y, to.X - from.X);
            }
        }

        public List<List<Point>> FindMinimalClosedLoops(IEnumerable<Segment> segments)
        {
            // 1. 버텍스 용접 (그래프 끊김 방지)
            var weldedSegments = WeldVertices(segments);

            // 2. 그래프 생성
            var graph = new Dictionary<Point, List<DirectedEdge>>(new RobustPointComparer());
            var allEdges = new List<DirectedEdge>();

            foreach (var seg in weldedSegments)
            {
                // 점(길이0) 제외
                if (Tolerance.Equals(seg.P1.X, seg.P2.X) && Tolerance.Equals(seg.P1.Y, seg.P2.Y)) continue;

                var e1 = new DirectedEdge(seg.P1, seg.P2);
                var e2 = new DirectedEdge(seg.P2, seg.P1);

                AddToGraph(graph, e1);
                AddToGraph(graph, e2);
                allEdges.Add(e1);
                allEdges.Add(e2);
            }

            // 3. 각도 정렬
            foreach (var list in graph.Values)
            {
                list.Sort((a, b) => a.Angle.CompareTo(b.Angle));
            }

            var rawLoops = new List<LoopResult>();

            // 4. 루프 탐색 (Left-Turn Algorithm)
            foreach (var startEdge in allEdges)
            {
                if (startEdge.Visited) continue;

                var path = new List<DirectedEdge>();
                var curr = startEdge;
                bool isLoopClosed = false;

                // 무한루프 방지
                int safety = 0;
                int maxIter = allEdges.Count * 2;

                while (!curr.Visited && safety++ < maxIter)
                {
                    curr.Visited = true;
                    path.Add(curr);

                    var next = GetBestTurnEdge(graph, curr);

                    // 끊긴 길
                    if (next == null) break;

                    // 루프 완성
                    if (next == startEdge)
                    {
                        isLoopClosed = true;
                        break;
                    }

                    // 다른 루프에 합류 (현재 루프는 무효)
                    if (next.Visited) break;

                    curr = next;
                }

                if (isLoopClosed && path.Count >= 3)
                {
                    var points = path.Select(e => e.From).ToList();
                    var loopResult = new LoopResult(points);
                    
                    // 아주 작은 노이즈 루프(0.0001 이하)는 버림
                    if (Math.Abs(loopResult.Area) > 0.001)
                    {
                        rawLoops.Add(loopResult);
                    }
                }
            }

            // 5. [중요] 외곽선(Outer Loop) 필터링
            // 방(Inner Room)과 외곽선(Outer Boundary)은 면적 부호가 반대입니다.
            // 그리고 외곽선은 모든 방의 합보다 크거나 비슷하므로 '면적이 가장 큰 것'이 외곽선일 확률이 높습니다.

            if (rawLoops.Count == 0) return new List<List<Point>>();

            // 부호별 그룹화 (양수 그룹 vs 음수 그룹)
            var positiveLoops = rawLoops.Where(l => l.Area > 0).ToList();
            var negativeLoops = rawLoops.Where(l => l.Area < 0).ToList();

            // 대다수(Majority)가 속한 그룹을 '방(Room)'으로 판단
            // (일반적인 도면은 방이 여러 개이고 외곽선은 1개임)
            List<LoopResult> innerRooms;
            
            if (positiveLoops.Count > negativeLoops.Count)
            {
                innerRooms = positiveLoops; // 양수가 방이다
            }
            else if (negativeLoops.Count > positiveLoops.Count)
            {
                innerRooms = negativeLoops; // 음수가 방이다
            }
            else
            {
                // 개수가 같다면(방1개 vs 외곽선1개), 절대 면적이 작은 것이 방이다.
                var maxPos = positiveLoops.Any() ? positiveLoops.Max(l => Math.Abs(l.Area)) : 0;
                var maxNeg = negativeLoops.Any() ? negativeLoops.Max(l => Math.Abs(l.Area)) : 0;
                innerRooms = maxPos < maxNeg ? positiveLoops : negativeLoops;
            }

            // 최종 결과: 방의 좌표 리스트만 반환
            return innerRooms.Select(l => l.Vertices).ToList();
        }

        private void AddToGraph(Dictionary<Point, List<DirectedEdge>> graph, DirectedEdge edge)
        {
            if (!graph.TryGetValue(edge.From, out var list))
            {
                list = new List<DirectedEdge>();
                graph[edge.From] = list;
            }
            list.Add(edge);
        }

        private List<Segment> WeldVertices(IEnumerable<Segment> originalSegments)
        {
            var uniquePoints = new Dictionary<Point, Point>(new RobustPointComparer());
            var result = new List<Segment>();

            Point GetOrAdd(Point p)
            {
                if (uniquePoints.TryGetValue(p, out var existing)) return existing;
                uniquePoints[p] = p;
                return p;
            }

            foreach (var seg in originalSegments)
            {
                result.Add(new Segment(GetOrAdd(seg.P1), GetOrAdd(seg.P2)));
            }
            return result;
        }

        private DirectedEdge GetBestTurnEdge(Dictionary<Point, List<DirectedEdge>> graph, DirectedEdge incoming)
        {
            if (!graph.TryGetValue(incoming.To, out var candidates) || candidates.Count == 0) return null;

            // 현재 엣지의 역방향 각도
            double backAngle = incoming.Angle + Math.PI;
            if (backAngle > Math.PI) backAngle -= 2 * Math.PI;

            // backAngle보다 크면서 가장 가까운 각도를 찾음 (Left-Most)
            // 리스트는 이미 정렬되어 있음
            foreach (var edge in candidates)
            {
                if (edge.Angle > backAngle + Tolerance.Epsilon) return edge;
            }

            // 한 바퀴 돌아서 제일 작은 각도 선택
            return candidates[0];
        }
    }
}