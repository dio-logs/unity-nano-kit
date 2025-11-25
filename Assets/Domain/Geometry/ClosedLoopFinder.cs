using System;
using System.Collections.Generic;
using System.Linq;
using IndustrialGeometry;

namespace BentleyOttmannCS
{
 public class ClosedLoopFinder
    {
        // 입력: 세그먼트 리스트
        // 출력: 폐곡선마다 세그먼트 리스트
        public static List<List<Tuple<Point, Point>>> FindClosedLoops(List<Tuple<Point, Point>> segments)
        {
            // 1. 그래프 생성 (Point -> 연결된 Point 리스트)
            var adjacency = new Dictionary<Point, List<Point>>();
            foreach (var seg in segments)
            {
                if (!adjacency.ContainsKey(seg.Item1)) adjacency[seg.Item1] = new List<Point>();
                if (!adjacency.ContainsKey(seg.Item2)) adjacency[seg.Item2] = new List<Point>();
                adjacency[seg.Item1].Add(seg.Item2);
                adjacency[seg.Item2].Add(seg.Item1);
            }

            var visitedEdges = new HashSet<Tuple<Point, Point>>(new EdgeComparer());
            var loops = new List<List<Tuple<Point, Point>>>();

            // 모든 세그먼트에서 DFS 시작
            foreach (var seg in segments)
            {
                if (visitedEdges.Contains(seg)) continue;

                var loop = TraceLoop(seg.Item1, seg.Item2, adjacency, visitedEdges);
                if (loop != null && loop.Count >= 3) // 최소 3개 세그먼트
                    loops.Add(loop);
            }

            return loops;
        }

        private static List<Tuple<Point, Point>> TraceLoop(
            Point start, Point next,
            Dictionary<Point, List<Point>> adjacency,
            HashSet<Tuple<Point, Point>> visitedEdges)
        {
            var stack = new Stack<(Point current, Point prev, List<Tuple<Point, Point>> path)>();
            stack.Push((next, start, new List<Tuple<Point, Point>> { Tuple.Create(start, next) }));

            while (stack.Count > 0)
            {
                var (current, prev, path) = stack.Pop();
                var edge = Tuple.Create(prev, current);

                if (visitedEdges.Contains(edge)) continue;
                visitedEdges.Add(edge);

                foreach (var neighbor in adjacency[current])
                {
                    if (neighbor.Equals(start) && path.Count >= 2)
                    {
                        var closedPath = new List<Tuple<Point, Point>>(path)
                        {
                            Tuple.Create(current, neighbor)
                        };
                        return closedPath;
                    }

                    var newEdge = Tuple.Create(current, neighbor);
                    if (!visitedEdges.Contains(newEdge))
                    {
                        var newPath = new List<Tuple<Point, Point>>(path)
                        {
                            newEdge
                        };
                        stack.Push((neighbor, current, newPath));
                    }
                }
            }

            return null;
        }

        private class EdgeComparer : IEqualityComparer<Tuple<Point, Point>>
        {
            public bool Equals(Tuple<Point, Point> x, Tuple<Point, Point> y)
            {
                return (x.Item1.Equals(y.Item1) && x.Item2.Equals(y.Item2)) ||
                       (x.Item1.Equals(y.Item2) && x.Item2.Equals(y.Item1));
            }

            public int GetHashCode(Tuple<Point, Point> obj)
            {
                int h1 = obj.Item1.GetHashCode();
                int h2 = obj.Item2.GetHashCode();
                return h1 ^ h2;
            }
        }
    }
}