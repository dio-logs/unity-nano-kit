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
        // 내부 데이터 구조
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
                Angle = Math.Atan2(to.Y - from.Y, to.X - from.X);
            }
        }

        private class InternalLoop
        {
            public List<Segment> Segments { get; }
            public double Area { get; }

            public InternalLoop(List<DirectedEdge> edges)
            {
                Segments = edges.Select(e => new Segment(e.From, e.To)).ToList();
                Area = CalculateSignedArea(edges.Select(e => e.From).ToList());
            }

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

        // ------------------------------------------------------------------
        // [Public API 1] 모든 루프 찾기 (기존)
        // ------------------------------------------------------------------
        public List<List<Segment>> FindMinimalClosedLoopsAsSegments(IEnumerable<Segment> segments)
        {
            // 1. 그래프 빌드
            var graphContext = BuildGraph(segments);
            var rawLoops = new List<InternalLoop>();

            // 2. 모든 엣지 순회
            foreach (var startEdge in graphContext.AllEdges)
            {
                if (startEdge.Visited) continue;
                var loop = TraceLoop(graphContext.Graph, startEdge);
                if (loop != null) rawLoops.Add(loop);
            }

            // 3. 외곽선 자동 필터링 후 반환
            return FilterOuterLoops(rawLoops).Select(l => l.Segments).ToList();
        }

        // ------------------------------------------------------------------
        // [Public API 2] 타겟 세그먼트 기준 루프 찾기 (신규 요청)
        // ------------------------------------------------------------------
        public List<List<Segment>> FindClosedLoopsAroundSegment(IEnumerable<Segment> allSegments, Segment target)
        {
            // 1. 그래프 빌드
            var graphContext = BuildGraph(allSegments);
            var foundLoops = new List<InternalLoop>();

            // 2. 타겟 세그먼트와 일치하는 그래프상의 엣지(Edge) 찾기
            // 타겟 세그먼트는 양방향(A->B, B->A) 두 개의 엣지에 대응될 수 있음
            var targetEdges = FindMatchingEdges(graphContext.AllEdges, target);

            foreach (var startEdge in targetEdges)
            {
                // 이미 방문했더라도, 타겟 기준 검색이므로 강제로 다시 추적 가능해야 함.
                // 하지만 TraceLoop 내부에서 Visited를 체크하므로, 
                // 특정 타겟 탐색을 위해 임시로 Visited를 초기화하거나, 
                // 여기서는 새로 빌드된 그래프이므로 Visited가 모두 false 상태임.
                
                var loop = TraceLoop(graphContext.Graph, startEdge);
                if (loop != null)
                {
                    foundLoops.Add(loop);
                }
            }

            // 3. 필터링 (선택 사항)
            // 보통 벽 하나는 [방1]과 [방2] 사이에 있거나, [방1]과 [외부] 사이에 있음.
            // 모든 유효한 루프를 반환하되, '외부(Outer)'라고 판단되는 아주 큰 루프는 제외할 수도 있음.
            // 여기서는 사용자가 판단할 수 있도록 유효한 기하학적 루프는 모두 반환함.
            // 단, 노이즈(면적 0)는 TraceLoop에서 이미 제외됨.
            
            return foundLoops.Select(l => l.Segments).ToList();
        }

        // ------------------------------------------------------------------
        // 내부 로직 (재사용성을 위해 분리)
        // ------------------------------------------------------------------

        private class GraphContext
        {
            public Dictionary<Point, List<DirectedEdge>> Graph { get; set; }
            public List<DirectedEdge> AllEdges { get; set; }
        }

        private GraphContext BuildGraph(IEnumerable<Segment> segments)
        {
            var weldedSegments = WeldVertices(segments);
            var graph = new Dictionary<Point, List<DirectedEdge>>(new RobustPointComparer());
            var allEdges = new List<DirectedEdge>();

            foreach (var seg in weldedSegments)
            {
                if (Tolerance.Equals(seg.P1.X, seg.P2.X) && Tolerance.Equals(seg.P1.Y, seg.P2.Y)) continue;

                var e1 = new DirectedEdge(seg.P1, seg.P2);
                var e2 = new DirectedEdge(seg.P2, seg.P1);

                AddToGraph(graph, e1);
                AddToGraph(graph, e2);
                allEdges.Add(e1);
                allEdges.Add(e2);
            }

            // 각도 정렬
            foreach (var list in graph.Values)
                list.Sort((a, b) => a.Angle.CompareTo(b.Angle));

            return new GraphContext { Graph = graph, AllEdges = allEdges };
        }

        private InternalLoop TraceLoop(Dictionary<Point, List<DirectedEdge>> graph, DirectedEdge startEdge)
        {
            var path = new List<DirectedEdge>();
            var curr = startEdge;
            bool isClosed = false;
            int safety = 0;
            int maxIter = 5000; // 충분히 큰 수

            while (!curr.Visited && safety++ < maxIter)
            {
                curr.Visited = true;
                path.Add(curr);

                var next = GetBestTurnEdge(graph, curr);
                if (next == null) break;

                if (next == startEdge)
                {
                    isClosed = true;
                    break;
                }
                
                // 주의: 타겟 검색 시에는 다른 경로로 합류해도 루프로 인정하지 않음 (단순화)
                if (next.Visited) break; 
                curr = next;
            }

            if (isClosed && path.Count >= 3)
            {
                var loop = new InternalLoop(path);
                if (Math.Abs(loop.Area) > 0.001) return loop;
            }
            return null;
        }

        private List<DirectedEdge> FindMatchingEdges(List<DirectedEdge> allEdges, Segment target)
        {
            var matches = new List<DirectedEdge>();
            var comparer = new RobustPointComparer();

            // Welding으로 인해 좌표값이 미세하게 다를 수 있으므로 RobustComparer 사용
            // Target의 P1->P2 방향과 P2->P1 방향 모두 찾음
            foreach (var edge in allEdges)
            {
                bool matchForward = comparer.Equals(edge.From, target.P1) && comparer.Equals(edge.To, target.P2);
                bool matchBackward = comparer.Equals(edge.From, target.P2) && comparer.Equals(edge.To, target.P1);

                if (matchForward || matchBackward)
                {
                    matches.Add(edge);
                }
            }
            return matches;
        }

        private List<InternalLoop> FilterOuterLoops(List<InternalLoop> rawLoops)
        {
            if (rawLoops.Count == 0) return rawLoops;

            var positive = rawLoops.Where(l => l.Area > 0).ToList();
            var negative = rawLoops.Where(l => l.Area < 0).ToList();

            // 다수결 원칙으로 방(Room) 그룹 판별
            if (positive.Count > negative.Count) return positive;
            if (negative.Count > positive.Count) return negative;

            double maxPos = positive.Any() ? positive.Max(l => Math.Abs(l.Area)) : 0;
            double maxNeg = negative.Any() ? negative.Max(l => Math.Abs(l.Area)) : 0;
            return maxPos < maxNeg ? positive : negative;
        }

        // --- Helpers ---
        private void AddToGraph(Dictionary<Point, List<DirectedEdge>> graph, DirectedEdge edge)
        {
            if (!graph.TryGetValue(edge.From, out var list))
            {
                list = new List<DirectedEdge>();
                graph[edge.From] = list;
            }
            list.Add(edge);
        }

        private DirectedEdge GetBestTurnEdge(Dictionary<Point, List<DirectedEdge>> graph, DirectedEdge incoming)
        {
            if (!graph.TryGetValue(incoming.To, out var candidates) || candidates.Count == 0) return null;
            
            double backAngle = incoming.Angle + Math.PI;
            if (backAngle > Math.PI) backAngle -= 2 * Math.PI;

            foreach (var edge in candidates)
            {
                if (edge.Angle > backAngle + Tolerance.Epsilon) return edge;
            }
            return candidates[0];
        }

        private List<Segment> WeldVertices(IEnumerable<Segment> segments)
        {
            var uniquePoints = new Dictionary<Point, Point>(new RobustPointComparer());
            var result = new List<Segment>();

            Point GetOrAdd(Point p)
            {
                if (uniquePoints.TryGetValue(p, out var existing)) return existing;
                uniquePoints[p] = p;
                return p;
            }

            foreach (var seg in segments)
            {
                // ID 복사 등이 필요하면 여기서 처리
                result.Add(new Segment(GetOrAdd(seg.P1), GetOrAdd(seg.P2)));
            }
            return result;
        }
    }
}