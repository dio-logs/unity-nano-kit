using UnityEngine;
using System.Collections.Generic;
using IndustrialGeometry; // Using the latest algorithm's namespace
using System;
using System.Linq;

namespace Tests.Runtime
{
    public class BentelyOttmanTester : MonoBehaviour
    {
        public int numberOfSegments = 50;
        public float bounds = 10f;
        public Material lineMaterial;

        private GameObject segmentsParent; 
        private List<InternalLineSegment> generatedSegments;
        private List<GameObject> intersectionMarkers;

        private struct InternalLineSegment
        {
            public Vector2 P1;
            public Vector2 P2;
            public int Id;

            public InternalLineSegment(Vector2 p1, Vector2 p2, int id)
            {
                P1 = p1;
                P2 = p2;
                Id = id;
            }
        }

        void Start()
        {
            // Cleanup and setup for segment GameObjects
            if (segmentsParent != null)
            {
                Destroy(segmentsParent);
            }
            segmentsParent = new GameObject("GeneratedSegments");
            segmentsParent.transform.SetParent(this.transform);

            GenerateAndDrawSegments();
            FindAndVisualizeIntersections();
        }

        private void GenerateAndDrawSegments()
        {
            // 1. Initialize lists
            if (generatedSegments == null)
            {
                generatedSegments = new List<InternalLineSegment>();
            }
            generatedSegments.Clear();
            
            // 2. Add random segments to the list
            for (int i = 0; i < numberOfSegments; i++)
            {
                Vector2 p1 = new Vector2(UnityEngine.Random.Range(-bounds, bounds), UnityEngine.Random.Range(-bounds, bounds));
                Vector2 p2 = new Vector2(UnityEngine.Random.Range(-bounds, bounds), UnityEngine.Random.Range(-bounds, bounds));
                generatedSegments.Add(new InternalLineSegment(p1, p2, generatedSegments.Count));
            }

            // 3. Add closed shape segments to the list
            AddClosedShapeSegments(generatedSegments);

            // 4. Draw all segments from the list
            foreach (var seg in generatedSegments)
            {
                GameObject segmentGO = new GameObject($"Segment_{seg.Id}");
                segmentGO.transform.SetParent(segmentsParent.transform);
                LineRenderer lr = segmentGO.AddComponent<LineRenderer>();

                lr.widthMultiplier = 0.1f;
                if (lineMaterial != null)
                {
                    lr.material = lineMaterial;
                }
                else
                {
                    lr.material = new Material(Shader.Find("Sprites/Default"));
                    lr.startColor = Color.cyan;
                    lr.endColor = Color.cyan;
                }
                
                lr.positionCount = 2;
                lr.SetPosition(0, seg.P1);
                lr.SetPosition(1, seg.P2);
            }
        }

        /// <summary>
        /// Adds a set of hardcoded closed polygon segments to the list for testing.
        /// </summary>
        private void AddClosedShapeSegments(ICollection<InternalLineSegment> segments)
        {
            // Define a square
            var square = new[]
            {
                new Vector2(2, 2), new Vector2(8, 2), new Vector2(8, 8), new Vector2(2, 8)
            };
            AddPolygonSegments(segments, square);

            // Define a triangle that overlaps with the square
            var triangle = new[]
            {
                new Vector2(6, 5), new Vector2(12, 5), new Vector2(9, 10)
            };
            AddPolygonSegments(segments, triangle);
        }

        /// <summary>
        /// Helper method to convert a loop of vertices into a list of segments.
        /// </summary>
        private void AddPolygonSegments(ICollection<InternalLineSegment> segments, IReadOnlyList<Vector2> vertices)
        {
            for (int i = 0; i < vertices.Count; i++)
            {
                Vector2 p1 = vertices[i];
                Vector2 p2 = vertices[(i + 1) % vertices.Count]; // Wrap around for the last segment
                segments.Add(new InternalLineSegment(p1, p2, segments.Count));
            }
        }
        
        private void FindAndVisualizeIntersections()
        {
            if (intersectionMarkers == null)
            {
                intersectionMarkers = new List<GameObject>();
            }
            intersectionMarkers.ForEach(Destroy);
            intersectionMarkers.Clear();

            var solverSegments = generatedSegments
                .Select(s => new Tuple<Point, Point>(ToIndustrialPoint(s.P1), ToIndustrialPoint(s.P2)))
                .ToList();
            
            var solver = new IndustrialBentleyOttmann();
            List<(Point pt, int id1, int id2, IntersectionType type)> intersections = solver.FindIntersections(solverSegments);

            foreach (var result in intersections)
            {
                Vector2 intersectionPoint = ToUnityVector2(result.pt);
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.position = intersectionPoint;
                sphere.transform.localScale = Vector3.one * 0.25f;

                var sphereRenderer = sphere.GetComponent<Renderer>();
                if (sphereRenderer != null)
                {
                    sphereRenderer.material.color = GetIntersectionColor(result.type);
                }

                intersectionMarkers.Add(sphere);
            }
        }

        private Color GetIntersectionColor(IntersectionType type)
        {
            switch (type)
            {
                case IntersectionType.Crossing:
                    return Color.red;
                case IntersectionType.T_Junction:
                    return Color.yellow;
                case IntersectionType.Corner:
                    return Color.green;
                case IntersectionType.Overlap:
                    return Color.magenta;
                default:
                    return Color.white;
            }
        }

        private static Point ToIndustrialPoint(Vector2 unityVector)
        {
            return new Point(unityVector.x, unityVector.y);
        }

        private static Vector2 ToUnityVector2(Point industrialPoint)
        {
            return new Vector2((float)industrialPoint.X, (float)industrialPoint.Y);
        }
    }
}