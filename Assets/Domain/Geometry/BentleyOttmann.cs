using System;
using System.Collections.Generic;
using System.Linq;

namespace IndustrialGeometry
{
    // 정밀도 제어용 상수 (CAD 도면 기준 0.001mm 정도의 허용오차 권장)
    public static class Tolerance
    {
        public const double Epsilon = 1e-6; 

        public static bool Equals(double a, double b) => Math.Abs(a - b) < Epsilon;
        public static bool LessOrEqual(double a, double b) => a < b + Epsilon;
        public static bool GreaterOrEqual(double a, double b) => a > b - Epsilon;
        public static int Compare(double a, double b)
        {
            if (Equals(a, b)) return 0;
            return a < b ? -1 : 1;
        }
    }

    public enum IntersectionType
    {
        Crossing,   // X자 교차 (벽체 통과 오류)
        T_Junction, // T자 접합 (정상 연결)
        Corner,     // L자 모서리 (정상 연결 - 끝점 공유)
        Overlap     // 중첩 (설계 오류)
    }

    public struct Point : IComparable<Point>, IEquatable<Point>
    {
        public double X { get; }
        public double Y { get; }

        public Point(double x, double y) { X = x; Y = y; }

        public int CompareTo(Point other)
        {
            int cmpX = Tolerance.Compare(X, other.X);
            if (cmpX != 0) return cmpX;
            return Tolerance.Compare(Y, other.Y);
        }

        public bool Equals(Point other) => CompareTo(other) == 0;
        public override bool Equals(object obj) => obj is Point p && Equals(p);
        public override int GetHashCode() => (Math.Round(X, 5), Math.Round(Y, 5)).GetHashCode();
        public override string ToString() => $"({X:F3}, {Y:F3})";
    }

    public class Segment
    {
        public int Id { get; }
        public Point Start { get; }
        public Point End { get; }

        // 스윕라인 비교를 위한 현재 Y값 계산
        public double GetYAt(double x)
        {
            if (Tolerance.Equals(Start.X, End.X)) return Start.Y; // 수직선
            
            // 선형 보간
            double t = (x - Start.X) / (End.X - Start.X);
            return Start.Y + t * (End.Y - Start.Y);
        }

        public Segment(Point p1, Point p2, int id)
        {
            Id = id;
            // 항상 X가 작은 쪽을 Start로 (X가 같으면 Y가 작은 쪽)
            if (p1.CompareTo(p2) > 0) { Start = p2; End = p1; }
            else { Start = p1; End = p2; }
        }
        public override string ToString() => $"Seg{Id}";
    }

    // 이벤트 타입 정의
    internal enum EventType { Left, Right, Intersection }

    internal class Event : IComparable<Event>
    {
        public Point Point { get; }
        public EventType Type { get; }
        
        // Left/Right 이벤트용
        public Segment SegA { get; } 
        
        // Intersection 이벤트용 (교차하는 두 선분)
        public Segment SegB { get; } 

        public Event(Point point, EventType type, Segment s1, Segment s2 = null)
        {
            Point = point; Type = type; SegA = s1; SegB = s2;
        }

        public int CompareTo(Event other)
        {
            int cmp = Point.CompareTo(other.Point);
            if (cmp != 0) return cmp;

            // 좌표가 같을 때 처리 순서: 
            // 1. Intersection (상태 변경 및 보고)
            // 2. Left (새 선분 추가)
            // 3. Right (다 쓴 선분 제거)
            // 이 순서가 Active List의 무결성을 유지하는데 유리함
            if (Type != other.Type)
            {
                int Order(EventType t) => t == EventType.Intersection ? 0 : (t == EventType.Left ? 1 : 2);
                return Order(Type).CompareTo(Order(other.Type));
            }
            return 0; 
        }
    }

    public class IndustrialBentleyOttmann
    {
        private List<Segment> _activeList; // BST 대신 List 사용 (구현 복잡도 감소 및 안정성 확보)
        private SortedSet<Event> _eventQueue;
        private double _currentSweepX;

        public IndustrialBentleyOttmann()
        {
            _activeList = new List<Segment>();
            _eventQueue = new SortedSet<Event>();
        }

        public List<(Point pt, int id1, int id2, IntersectionType type)> FindIntersections(IEnumerable<Tuple<Point, Point>> lines)
        {
            // 1. 초기화
            _activeList.Clear();
            _eventQueue.Clear();
            
            var segments = lines.Select((t, i) => new Segment(t.Item1, t.Item2, i)).ToList();
            var results = new List<(Point, int, int, IntersectionType)>();
            var processedPairs = new HashSet<string>(); // 중복 보고 방지용 키 (예: "1-5", "5-1")

            // 2. 초기 이벤트 등록 (Left, Right)
            foreach (var seg in segments)
            {
                // 길이가 0인 선분(점)은 무시
                if (seg.Start.Equals(seg.End)) continue;

                _eventQueue.Add(new Event(seg.Start, EventType.Left, seg));
                _eventQueue.Add(new Event(seg.End, EventType.Right, seg));
            }

            // 3. 스윕 라인 진행
            while (_eventQueue.Count > 0)
            {
                var ev = _eventQueue.Min;
                _eventQueue.Remove(ev);
                
                _currentSweepX = ev.Point.X;

                if (ev.Type == EventType.Left)
                {
                    HandleLeft(ev);
                }
                else if (ev.Type == EventType.Right)
                {
                    HandleRight(ev);
                }
                else if (ev.Type == EventType.Intersection)
                {
                    // 중복 이벤트 필터링
                    string key = GetPairKey(ev.SegA.Id, ev.SegB.Id);
                    if (processedPairs.Add(key))
                    {
                        // 교차 타입 판별
                        var type = ClassifyIntersection(ev.SegA, ev.SegB, ev.Point);
                        results.Add((ev.Point, ev.SegA.Id, ev.SegB.Id, type));
                        
                        HandleIntersection(ev);
                    }
                }
            }

            return results;
        }

        private void HandleLeft(Event ev)
        {
            var seg = ev.SegA;
            
            // 삽입 위치 탐색 (Y값 기준)
            int index = GetInsertIndex(seg);
            _activeList.Insert(index, seg);

            // 위/아래 이웃과 교차 검사
            if (index > 0) 
                CheckIntersection(_activeList[index - 1], seg);
            if (index < _activeList.Count - 1) 
                CheckIntersection(seg, _activeList[index + 1]);
        }

        private void HandleRight(Event ev)
        {
            var seg = ev.SegA;
            int index = _activeList.FindIndex(s => s.Id == seg.Id);
            
            if (index != -1)
            {
                Segment upper = index < _activeList.Count - 1 ? _activeList[index + 1] : null;
                Segment lower = index > 0 ? _activeList[index - 1] : null;

                _activeList.RemoveAt(index);

                // 삭제로 인해 새로 붙게 된 이웃끼리 검사
                if (upper != null && lower != null)
                    CheckIntersection(lower, upper);
            }
        }

        private void HandleIntersection(Event ev)
        {
            Segment s1 = ev.SegA;
            Segment s2 = ev.SegB;

            // 리스트 내 위치 찾기
            int i1 = _activeList.FindIndex(s => s.Id == s1.Id);
            int i2 = _activeList.FindIndex(s => s.Id == s2.Id);

            // 이미 제거된 선분이면 스킵 (끝점에서 만나는 경우 등)
            if (i1 == -1 || i2 == -1) return;

            // Active List 상에서 물리적 위치 스왑
            // (교차점 이후에는 Y 순서가 뒤바뀌므로)
            // 주의: 리스트가 정렬된 상태라고 가정하에 인접해 있어야 정상이지만,
            // 다중 교차 시 인접하지 않을 수도 있음. 단순 스왑만 진행.
            
            var temp = _activeList[i1];
            _activeList[i1] = _activeList[i2];
            _activeList[i2] = temp;

            // 스왑 후 새로운 이웃들과 검사
            // 바뀐 위치 기준 위/아래 검사
            // (복잡성을 줄이기 위해 s1, s2 각각의 현재 위/아래를 다시 찾아서 검사)
            
            CheckNeighbor(s1);
            CheckNeighbor(s2);
        }

        private void CheckNeighbor(Segment seg)
        {
            int idx = _activeList.FindIndex(s => s.Id == seg.Id);
            if (idx == -1) return;

            if (idx > 0) CheckIntersection(_activeList[idx - 1], seg);
            if (idx < _activeList.Count - 1) CheckIntersection(seg, _activeList[idx + 1]);
        }

        private void CheckIntersection(Segment s1, Segment s2)
        {
            // 기하학적 교차 계산
            if (TryGetIntersectionPoint(s1, s2, out Point p))
            {
                // 현재 스윕 라인보다 오른쪽에 있거나, 
                // 현재 스윕 라인 위에 있지만 Y가 아직 처리되지 않은 경우(아래쪽)만 큐에 추가해야 함.
                // 하지만 단순하게는: "이미 처리된 과거(X < current)"가 아니면 추가.
                
                if (p.CompareTo(new Point(_currentSweepX, double.MinValue)) >= 0)
                {
                    _eventQueue.Add(new Event(p, EventType.Intersection, s1, s2));
                }
            }
        }

        // 선형 삽입 위치 찾기 (Binary Search는 정밀도 문제로 List에서 위험할 수 있어 순차 탐색 fallback 고려 가능하지만 여기선 이진탐색 유지)
        private int GetInsertIndex(Segment seg)
        {
            int left = 0;
            int right = _activeList.Count - 1;
            double targetY = seg.GetYAt(_currentSweepX);

            while (left <= right)
            {
                int mid = left + (right - left) / 2;
                double midY = _activeList[mid].GetYAt(_currentSweepX);

                if (Tolerance.Compare(midY, targetY) < 0) // mid < target
                    left = mid + 1;
                else
                    right = mid - 1;
            }
            return left;
        }

        private bool TryGetIntersectionPoint(Segment A, Segment B, out Point p)
        {
            p = new Point();
            double x1 = A.Start.X, y1 = A.Start.Y, x2 = A.End.X, y2 = A.End.Y;
            double x3 = B.Start.X, y3 = B.Start.Y, x4 = B.End.X, y4 = B.End.Y;

            double denom = (y4 - y3) * (x2 - x1) - (x4 - x3) * (y2 - y1);

            // 평행 또는 일치
            if (Math.Abs(denom) < Tolerance.Epsilon) return false;

            double ua = ((x4 - x3) * (y1 - y3) - (y4 - y3) * (x1 - x3)) / denom;
            double ub = ((x2 - x1) * (y1 - y3) - (y2 - y1) * (x1 - x3)) / denom;

            // [수정됨] 등호(LessOrEqual, GreaterOrEqual)를 사용하여 끝점 교차 포함
            if (Tolerance.GreaterOrEqual(ua, 0) && Tolerance.LessOrEqual(ua, 1) &&
                Tolerance.GreaterOrEqual(ub, 0) && Tolerance.LessOrEqual(ub, 1))
            {
                double ix = x1 + ua * (x2 - x1);
                double iy = y1 + ua * (y2 - y1);
                p = new Point(ix, iy);
                return true;
            }

            return false;
        }

        private IntersectionType ClassifyIntersection(Segment s1, Segment s2, Point p)
        {
            // 점 p가 각 선분의 끝점과 일치하는지 확인
            bool isEnd1 = p.Equals(s1.Start) || p.Equals(s1.End);
            bool isEnd2 = p.Equals(s2.Start) || p.Equals(s2.End);

            if (isEnd1 && isEnd2) return IntersectionType.Corner;     // ㄱ, ㄴ 자 모서리
            if (isEnd1 || isEnd2) return IntersectionType.T_Junction; // ㅜ, ㅏ 자 접합
            return IntersectionType.Crossing;                         // + 자 교차
        }

        private string GetPairKey(int id1, int id2)
        {
            return id1 < id2 ? $"{id1}-{id2}" : $"{id2}-{id1}";
        }
    }
}