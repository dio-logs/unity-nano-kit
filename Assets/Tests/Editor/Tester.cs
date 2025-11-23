using System;
using System.Collections.Generic;
using BentleyOttmannCS;
using NUnit.Framework;
using UnityEngine;

namespace Tests.Editor
{
    public  class LineSweepIntersections
    {
        [Test]
        public void OttmannTest()
        {
            var segments = new List<Tuple<Point, Point>>
            {
                Tuple.Create(new Point(0,0), new Point(5,0)),
                Tuple.Create(new Point(5,0), new Point(5,5)),
                Tuple.Create(new Point(5,5), new Point(0,5)),
                Tuple.Create(new Point(0,5), new Point(0,0)),
                Tuple.Create(new Point(1,1), new Point(4,4))
            };

            var solver = new BentleyOttmannSolver(segments);
            var intersections = solver.ComputeIntersections();

            // 내부에서 유니크 처리 완료 -> 반환된 리스트 그대로 검증
            // Assert.AreEqual(4, intersections.Count, "교차점은 4개여야 함");
            
            Debug.Log((intersections.Count));
        }
        
        [Test]
        public void OttmannVerticalTest()
        {
            var segments = new List<Tuple<Point, Point>>
            {
                Tuple.Create(new Point(0,0), new Point(5,0)),
                Tuple.Create(new Point(0,0), new Point(6,0))
            };

            var solver = new BentleyOttmannSolver(segments);
            var intersections = solver.ComputeIntersections();

            // 내부에서 유니크 처리 완료 -> 반환된 리스트 그대로 검증
            // Assert.AreEqual(4, intersections.Count, "교차점은 4개여야 함");
            
            Debug.Log((intersections.Count));
        }

        [Test]
        public void ClosedLoopTest()
        {
            var segments = new List<Tuple<Point, Point>>
            {
                Tuple.Create(new Point(0,0), new Point(5,0)),
                Tuple.Create(new Point(5,0), new Point(5,5)),
                Tuple.Create(new Point(5,5), new Point(0,5)),
                Tuple.Create(new Point(0,5), new Point(0,0)),
                Tuple.Create(new Point(0,0), new Point(5,5)),
                Tuple.Create(new Point(-1,0), new Point(0,5)),
                Tuple.Create(new Point(0,0), new Point(-1,0))
            };

            var polyons = ClosedLoopFinder.FindClosedLoops((segments));
            
            Debug.Log(polyons.Count);
        }
        


    }

 
}