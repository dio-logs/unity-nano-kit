using System;
using System.Collections.Generic;
using System.Linq;

namespace BentleyOttmannCS
{
    public struct Point : IEquatable<Point>
    {
        public double X { get; }
        public double Y { get; }

        public Point(double x, double y)
        {
            X = Math.Round(x, 3);
            Y = Math.Round(y, 3);
        }

        public bool Equals(Point other)
        {
            return Math.Abs(X - other.X) < 1e-9 && Math.Abs(Y - other.Y) < 1e-9;
        }

        public override bool Equals(object obj) => obj is Point p && Equals(p);
        public override int GetHashCode() => (X, Y).GetHashCode();
        public override string ToString() => $"({X:F3}, {Y:F3})";
    }

    public class Segment
    {
        public Point P1 { get; }
        public Point P2 { get; }
        public int Id { get; }

        public Segment(Point p1, Point p2, int id)
        {
            P1 = p1;
            P2 = p2;
            Id = id;
        }

        public double Yat(double x)
        {
            if (Math.Abs(P2.X - P1.X) < 1e-9)
                return P1.Y;
            double t = (x - P1.X) / (P2.X - P1.X);
            return P1.Y + t * (P2.Y - P1.Y);
        }
    }

    internal enum EventType { Left, Right, Intersection }

    internal class Event : IComparable<Event>
    {
        public Point Pt { get; }
        public EventType Type { get; }
        public Segment Seg1 { get; }
        public Segment Seg2 { get; }

        public Event(Point pt, EventType type, Segment s1, Segment s2 = null)
        {
            Pt = pt; Type = type; Seg1 = s1; Seg2 = s2;
        }

        public int CompareTo(Event other)
        {
            int cx = Pt.X.CompareTo(other.Pt.X);
            if (cx != 0) return cx;
            int cy = Pt.Y.CompareTo(other.Pt.Y);
            if (cy != 0) return cy;
            if (Type != other.Type)
                return Type == EventType.Left ? -1 :
                       other.Type == EventType.Left ? 1 :
                       (Type == EventType.Intersection ? -1 : 1);
            return Seg1.Id.CompareTo(other.Seg1.Id);
        }
    }

    public class BentleyOttmannSolver
    {
        private readonly List<Segment> segments;

        public BentleyOttmannSolver(IEnumerable<Tuple<Point, Point>> segs)
        {
            segments = segs.Select((t, i) => new Segment(t.Item1, t.Item2, i)).ToList();
        }

        public List<(Point point, int id1, int id2)> ComputeIntersections()
        {
            var events = new SortedSet<Event>();
            foreach (var s in segments)
            {
                events.Add(new Event(s.P1, EventType.Left, s));
                events.Add(new Event(s.P2, EventType.Right, s));
            }

            var active = new SortedSet<Segment>(Comparer<Segment>.Create((a, b) =>
            {
                double ya = a.Yat(CurrentX), yb = b.Yat(CurrentX);
                int cmp = ya.CompareTo(yb);
                if (cmp != 0) return cmp;
                return a.Id.CompareTo(b.Id);
            }));

            var intersectionsSet = new HashSet<Point>();
            var result = new List<(Point, int, int)>();

            while (events.Any())
            {
                var ev = events.Min;
                events.Remove(ev);
                CurrentX = ev.Pt.X;

                if (ev.Type == EventType.Left)
                {
                    foreach (var s in active)
                    {
                        if (TryIntersect(ev.Seg1, s, out Point ip))
                        {
                            if (intersectionsSet.Add(ip))  // 내부에서 유니크 처리
                                result.Add((ip, ev.Seg1.Id, s.Id));
                            events.Add(new Event(ip, EventType.Intersection, ev.Seg1, s));
                        }
                    }
                    active.Add(ev.Seg1);
                }
                else if (ev.Type == EventType.Right)
                {
                    active.Remove(ev.Seg1);
                }
                else
                {
                    active.Remove(ev.Seg1);
                    active.Remove(ev.Seg2);
                    active.Add(ev.Seg1);
                    active.Add(ev.Seg2);
                }
            }

            return result;
        }


        private double CurrentX;

        private bool TryIntersect(Segment s1, Segment s2, out Point ip)
        {
            ip = new Point();
            double x1 = s1.P1.X, y1 = s1.P1.Y;
            double x2 = s1.P2.X, y2 = s1.P2.Y;
            double x3 = s2.P1.X, y3 = s2.P1.Y;
            double x4 = s2.P2.X, y4 = s2.P2.Y;

            double denom = (x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4);
            if (Math.Abs(denom) < 1e-9)
            {
                // 평행/공유끝점 체크
                if (s1.P1.Equals(s2.P1) || s1.P1.Equals(s2.P2))
                    { ip = s1.P1; return true; }
                if (s1.P2.Equals(s2.P1) || s1.P2.Equals(s2.P2))
                    { ip = s1.P2; return true; }
                return false;
            }

            double t = ((x1 - x3)*(y3 - y4) - (y1 - y3)*(x3 - x4)) / denom;
            double u = ((x1 - x3)*(y1 - y2) - (y1 - y3)*(x1 - x2)) / denom;

            if (t >= 0 && t <= 1 && u >= 0 && u <= 1)
            {
                double ix = x1 + t * (x2 - x1);
                double iy = y1 + t * (y2 - y1);
                ip = new Point(ix, iy);
                return true;
            }

            return false;
        }
    }
}
