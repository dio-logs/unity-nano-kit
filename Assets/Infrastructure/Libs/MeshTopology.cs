using System.Collections.Generic;
using System.Linq;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Polygonize;

namespace Adapter.Libs
{
    public class MeshTopology
    {
        public List<Geometry> Polygonize(List<Segment> segments)
        {
            var factory = new GeometryFactory();

            // 2. 선분 리스트 준비 (WKT 대신 코드로 직접 생성)
            var lines = new List<Geometry>();
            foreach (var segment in segments)
            {
                lines.Add(factory.CreateLineString(SegmentToCoordinates(segment)));
            }

            var polygonizer = new Polygonizer();
            polygonizer.Add(lines);
            return polygonizer.GetPolygons().ToList();
            
        }

        private Coordinate[] SegmentToCoordinates(Segment segment)
        {
            return new Coordinate[] 
            {
                new Coordinate(segment.FromPoint.X, segment.FromPoint.Y),
                new Coordinate(segment.ToPoint.X, segment.ToPoint.Y)
            };
        }
    }


    public interface IIdentifiable
    {
        public object Id { get; }
    }


    public class Segment : IIdentifiable
    {
        public object Id { get; }
        public Point FromPoint { get; }
        public Point ToPoint { get; }


        public Segment(object id, Point fromPoint, Point toPoint)
        {
            Id = id;
            FromPoint = fromPoint;
            ToPoint = toPoint;
        }

    }

    public struct Point
    {
        public double X { get; }
        public double Y { get; }

        public Point(double x, double y)
        {
            X = x;
            Y = y;
        }
    }
    

}