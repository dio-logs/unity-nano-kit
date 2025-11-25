using UnityEngine;
using System.Collections.Generic;
using IndustrialGeometry;
using System;
using System.Linq;
using System.Diagnostics;
using Domain.Geometry;
using Debug = UnityEngine.Debug; // Added for Stopwatch

namespace Tests.Runtime
{
    public class IntersectionSolverTester : MonoBehaviour
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
            Stopwatch stopwatch = new Stopwatch(); // Start timer
            stopwatch.Start();

            // Cleanup and setup for segment GameObjects
            if (segmentsParent != null)
            {
                Destroy(segmentsParent);
            }
            segmentsParent = new GameObject("GeneratedSegments_IntersectionSolver");
            segmentsParent.transform.SetParent(this.transform);

            GenerateSegments();
            FindAndVisualizeIntersections();

            stopwatch.Stop(); // Stop timer
            UnityEngine.Debug.Log($"IntersectionSolverTester execution time: {stopwatch.Elapsed.TotalSeconds:F4} seconds"); // Log elapsed time
        }

        private void GenerateSegments()
        {
            // 1. Initialize lists
            if (generatedSegments == null)
            {
                generatedSegments = new List<InternalLineSegment>();
            }
            generatedSegments.Clear();
            
            // 2. Add random segments to the list with 3 decimal precision
            for (int i = 0; i < numberOfSegments; i++)
            {
                float x1 = Mathf.Round(UnityEngine.Random.Range(-bounds, bounds) * 1000f) / 1000f;
                float y1 = Mathf.Round(UnityEngine.Random.Range(-bounds, bounds) * 1000f) / 1000f;
                float x2 = Mathf.Round(UnityEngine.Random.Range(-bounds, bounds) * 1000f) / 1000f;
                float y2 = Mathf.Round(UnityEngine.Random.Range(-bounds, bounds) * 1000f) / 1000f;

                Vector2 p1 = new Vector2(x1, y1);
                Vector2 p2 = new Vector2(x2, y2);
                generatedSegments.Add(new InternalLineSegment(p1, p2, generatedSegments.Count));
            }
            
            // AddClosedShapeSegments(generatedSegments);
        }

        private void DrawSplitSegments(List<Segment> segments)
        {
            
            foreach (var seg in segments)
            {
                GameObject segmentGO = new GameObject($"Segment");
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
                    lr.startColor = Color.blue; // Differentiate from BentleyOttmannTester
                    lr.endColor = Color.blue;
                }
                
                lr.positionCount = 2;
                lr.SetPosition(0, seg.P1.ToUnityVector2());
                lr.SetPosition(1, seg.P2.ToUnityVector2());
            }
        }
        
        
    
        private void AddClosedShapeSegments(ICollection<InternalLineSegment> segments)
        {
            // Replace the old shapes with a grid of squares.
            AddGridSegments(segments, 5, 5, 2.5f, new Vector2(-12, -12));
        }

        /// <summary>
        /// Generates a grid of segments.
        /// </summary>
        /// <param name="segments">The collection to add segments to.</param>
        /// <param name="width">Number of cells in the x-direction.</param>
        /// <param name="height">Number of cells in the y-direction.</param>
        /// <param name="cellSize">The size of each square cell.</param>
        /// <param name="origin">The bottom-left corner of the grid.</param>
        private void AddGridSegments(ICollection<InternalLineSegment> segments, int width, int height, float cellSize, Vector2 origin)
        {
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    // Define the four corners of the current cell
                    var bottomLeft = new Vector2(origin.x + j * cellSize, origin.y + i * cellSize);
                    var bottomRight = new Vector2(origin.x + (j + 1) * cellSize, origin.y + i * cellSize);
                    var topLeft = new Vector2(origin.x + j * cellSize, origin.y + (i + 1) * cellSize);
                    var topRight = new Vector2(origin.x + (j + 1) * cellSize, origin.y + (i + 1) * cellSize);

                    // Add the 4 segments for this cell, creating overlapping segments
                    segments.Add(new InternalLineSegment(bottomLeft, bottomRight, segments.Count)); // Bottom
                    segments.Add(new InternalLineSegment(bottomRight, topRight, segments.Count));    // Right
                    segments.Add(new InternalLineSegment(topRight, topLeft, segments.Count));        // Top
                    segments.Add(new InternalLineSegment(topLeft, bottomLeft, segments.Count));      // Left
                }
            }
        }

        private void AddPolygonSegments(ICollection<InternalLineSegment> segments, IReadOnlyList<Vector2> vertices)
        {
            for (int i = 0; i < vertices.Count; i++)
            {
                Vector2 p1 = vertices[i];
                Vector2 p2 = vertices[(i + 1) % vertices.Count];
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
                .Select(s => new Segment(ToIndustrialPoint(s.P1), ToIndustrialPoint(s.P2)))
                .ToList();
            
            var solver = new IntersectionSolver(); // Use the new IntersectionSolver
            List<IntersectionResult> intersections = solver.FindIntersections(solverSegments);

            foreach (var result in intersections)
            {
                Vector2 intersectionPoint = ToUnityVector2(result.Point);
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.position = intersectionPoint;
                sphere.transform.localScale = Vector3.one * 0.25f;

                var sphereRenderer = sphere.GetComponent<Renderer>();
                if (sphereRenderer != null)
                {
                    sphereRenderer.material.color = GetIntersectionColor(result.Type);
                }

                intersectionMarkers.Add(sphere);
            }
            
            //분할하고 그림
            var segments = SegmentSplitter.SplitSegmentsAtIntersections(solverSegments);
            DrawSplitSegments(segments);
            var loopSolver = new ClosedLoopSolver();
            var loops = loopSolver.FindMinimalClosedLoops(segments);

            Debug.Log("total loop : " + loops.Count);
            loops.ForEach(loop => loop.ForEach(p => Debug.Log(p.ToUnityVector2())));


        }

        private Color GetIntersectionColor(IntersectionType type)
        {
            switch (type)
            {
                case IntersectionType.Crossing:
                    return Color.red;
                case IntersectionType.Junction_T: // T_Junction changed to Junction_T
                    return Color.yellow;
                case IntersectionType.Corner_L:   // Corner changed to Corner_L
                    return Color.green;
                case IntersectionType.Overlap:
                    return Color.magenta;
                default:
                    return Color.white;
            }
        }

        private static IndustrialGeometry.Point ToIndustrialPoint(Vector2 unityVector)
        {
            return new IndustrialGeometry.Point(unityVector.x, unityVector.y);
        }

        private static Vector2 ToUnityVector2(IndustrialGeometry.Point industrialPoint)
        {
            return new Vector2((float)industrialPoint.X, (float)industrialPoint.Y);
        }
    }
}