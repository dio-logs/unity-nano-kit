using System.Collections.Generic;
using System.Linq;
using IndustrialGeometry;

namespace Domain.Geometry
{
    public class SegmentSplitter
    {
        public static List<Segment> SplitSegmentsAtIntersections(IEnumerable<Segment> rawSegments)
        {
            var originalSegments = rawSegments.ToList();
            
            // 1. 교차점 찾기
            var solver = new IntersectionSolver();
            // 전체 vs 전체 검사 수행
            List<IntersectionResult> intersections = solver.FindIntersections(originalSegments);

            // 2. 선분별로 잘라야 할 점(Cut Points) 수집
            // Key: 대상 선분, Value: 그 선분 위에 있는 교차점들
            var cutPointsMap = new Dictionary<Segment, HashSet<Point>>();

            // 맵 초기화
            foreach (var seg in originalSegments)
            {
                cutPointsMap[seg] = new HashSet<Point>();
            }

            // 교차점 등록 (Segment1, Segment2 양쪽 모두에 해당 점을 추가)
            foreach (var result in intersections)
            {
                // None이 아닌 유효한 교차에 대해서만 처리
                if (result.Type != IntersectionType.None)
                {
                    // Segment1 위에 점 추가
                    cutPointsMap[result.Segment1].Add(result.Point);
                    
                    // Segment2 위에 점 추가
                    cutPointsMap[result.Segment2].Add(result.Point);
                }
            }

            // 3. 분할 수행 및 결과 리스트 생성
            var splitResultSegments = new List<Segment>();

            foreach (var seg in originalSegments)
            {
                var pointsOnLine = cutPointsMap[seg].ToList();

                // 아무런 교차점이 없으면 원본 유지
                if (pointsOnLine.Count == 0)
                {
                    splitResultSegments.Add(seg);
                    continue;
                }

                // 시작점과 끝점도 포함시켜서 정렬 준비
                // (HashSet을 거쳤어도 P1, P2와 겹칠 수 있으므로 중복 방지 로직 필요하나,
                //  아래 정렬 후 루프에서 중복 점 처리가 가능함)
                pointsOnLine.Add(seg.P1);
                pointsOnLine.Add(seg.P2);

                // 4. 점 정렬 (P1으로부터의 거리 기준)
                // 수직선, 수평선, 대각선 모두 대응하기 위해 거리(Squared Distance) 사용
                pointsOnLine.Sort((a, b) => 
                {
                    double distA = GetDistSq(seg.P1, a);
                    double distB = GetDistSq(seg.P1, b);
                    return distA.CompareTo(distB);
                });

                // 5. 정렬된 점들을 이어서 세그먼트 생성
                for (int i = 0; i < pointsOnLine.Count - 1; i++)
                {
                    Point current = pointsOnLine[i];
                    Point next = pointsOnLine[i + 1];

                    // 길이가 0인 세그먼트(중복 점)는 생성하지 않음
                    // Tolerance를 이용한 Equals 체크
                    if (!current.Equals(next))
                    {
                        // ID는 새로 발급하거나, 기존 ID를 유지하며 suffix를 붙이는 등 정책 결정 필요
                        // 여기서는 임시로 -1 또는 기존 ID 유지
                        splitResultSegments.Add(new Segment(current, next));
                    }
                }
            }

            return splitResultSegments;
        }

        // 거리 제곱 계산 (Sqrt 비용 절약)
        private static double GetDistSq(Point p1, Point p2)
        {
            double dx = p1.X - p2.X;
            double dy = p1.Y - p2.Y;
            return dx * dx + dy * dy;
        }
    }
}