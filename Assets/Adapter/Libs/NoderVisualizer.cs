using UnityEngine;
using NetTopologySuite.Geometries;
using NetTopologySuite.Noding;
using NetTopologySuite.Noding.Snapround;
using System.Collections.Generic;
using System.Linq;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.Operation.Polygonize;
using Random = System.Random;

namespace Adapter.Libs
{
    public class NoderVisualizer : MonoBehaviour
    {
        void Start()
        {
            RunNoderWithCustomSegments();
        }
        private void RunTestMTS()
        {
            var factory = new GeometryFactory();
            var random = new Random();
            var lines = new List<LineString>();

            // 2. 임의의 선분 30개 생성
            // 좌표 범위는 0 ~ 100으로 가정
            for (int i = 0; i < 30; i++)
            {
                var x1 = random.NextDouble() * 100;
                var y1 = random.NextDouble() * 100;
                var x2 = random.NextDouble() * 100;
                var y2 = random.NextDouble() * 100;

                var p1 = new Coordinate(x1, y1);
                var p2 = new Coordinate(x2, y2);

                // 선분(LineString) 생성 후 리스트에 추가
                var line = factory.CreateLineString(new[] { p1, p2 });
                lines.Add(line);
            }

            Debug.Log($"생성된 원본 선분 개수: {lines.Count}");

            // 3. 선분 분할 (Noding) 수행
            // MultiLineString으로 묶은 뒤 UnaryUnion을 수행하면
            // 교차점이 모두 계산되어 자잘한 선분들로 나뉩니다.
            var multiLineString = factory.CreateMultiLineString(lines.ToArray());
            
            // UnaryUnion은 "Noded Union"을 수행하여 교차점에서 선을 자릅니다.
            var nodedGeometry = multiLineString.Union();

            // 4. 결과 확인
            // 결과는 MultiLineString 형태일 것이므로, 이를 구성하는 개별 LineString 개수를 확인합니다.
            int segmentCount = nodedGeometry.NumGeometries;

            Debug.Log("------------------------------------------------");
            Debug.Log($"분할(Noding) 처리 후 선분 개수: {segmentCount}");
            Debug.Log("------------------------------------------------");

            // (선택 사항) 분할된 각 선분의 좌표 출력
            for (int i = 0; i < nodedGeometry.NumGeometries; i++)
            {
                var segment = nodedGeometry.GetGeometryN(i);
                // 너무 길어질 수 있으므로 처음 5개만 출력하거나 전체를 출력하려면 주석 해제
                // Console.WriteLine($"Segment {i + 1}: {segment}");
            }
            
            Debug.Log("작업 완료. 교차하는 지점마다 선분이 분리되었습니다.");
        }


        static List<Segment> ProcessGeometryWithPreservation(List<Segment> originalItems, GeometryFactory factory)
        {
            // 1. 빠른 검색을 위해 원본 아이템을 STRtree(공간 인덱스)에 넣습니다.
            var tree = new STRtree<Segment>();
            foreach (var item in originalItems)
            {
                // 아이템의 경계상자(Envelope)를 키로 저장
                tree.Insert(item.Geometry.EnvelopeInternal, item);
            }
            // 인덱스 빌드 (검색 전 필수)
            tree.Build();

            // 2. Noding (분할) 수행
            // 원본 지오메트리들만 추출
            var geometries = originalItems.Select(x => x.Geometry).ToArray();
            var multiLine = factory.CreateMultiLineString(geometries);
            var nodedGeometry = multiLine.Union(); // 분할된 결과

            // 3. Polygonizer (면 생성 - 선택사항)
            var polygonizer = new Polygonizer();
            polygonizer.Add(nodedGeometry);
            var polygons = polygonizer.GetPolygons();

            // 4. 결과 리스트 생성
            var resultList = new List<CanvasItem>();

            // (A) 폴리곤 처리 (폴리곤은 구조적으로 무조건 새로운 객체로 취급 -> 새 ID)
            foreach (var poly in polygons)
            {
                resultList.Add(new CanvasItem((Geometry)poly, "Polygon"));
            }

            // (B) 선분 처리 (Noding된 결과를 원본과 비교)
            // nodedGeometry는 MultiLineString이거나 LineString일 수 있음
            for (int i = 0; i < nodedGeometry.NumGeometries; i++)
            {
                var splitSegment = nodedGeometry.GetGeometryN(i);

                // ** 핵심: 이 조각이 원본 리스트에 그대로 있는지 확인 **
                var match = FindExactMatch(tree, splitSegment);

                if (match != null)
                {
                    // [유지] 원본과 완벽히 같다면, 원본 ID를 계승합니다.
                    // 기하는 새로 생성된 것(splitSegment)을 써도 되고 원본을 써도 되지만,
                    // 미세한 오차 방지를 위해 원본(match.Geometry)을 쓰는 게 더 안전할 수 있습니다.
                    // 여기선 좌표 정규화를 위해 splitSegment를 쓰되 ID만 가져옵니다.
                    resultList.Add(new CanvasItem(splitSegment, "Line", match.Id));
                }
                else
                {
                    // [변경됨] 원본에 없는 모양이라면(쪼개졌다면), 새 ID 발급
                    resultList.Add(new CanvasItem(splitSegment, "Line", null)); // null -> 새 GUID 생성
                }
            }

            return resultList;
        }

        // STRtree를 이용해 정확히 일치하는 원본 찾기
        static CanvasItem FindExactMatch(STRtree<CanvasItem> tree, Geometry searchGeom)
        {
            // 1. 경계 상자(Envelope)가 겹치는 후보군을 먼저 검색 (매우 빠름)
            var candidates = tree.Query(searchGeom.EnvelopeInternal);

            // 2. 후보군 중에서 '정밀 비교' 수행
            foreach (var candidate in candidates)
            {
                // EqualsExact: 좌표값까지 허용오차 내에서 정확히 일치하는지 확인
                // 일반 .Equals()는 위상만 같으면(방향 달라도) true일 수 있음. 필요에 따라 선택.
                
                // 0.00001 같은 작은 오차(Tolerance)를 줄 수도 있습니다.
                // 여기서는 기본적인 EqualsTopologically 사용 (방향 달라도 같은 선분으로 인정)
                if (searchGeom.Equals(candidate.Geometry)) 
                {
                    return candidate;
                }
            }

            return null; // 일치하는 것 없음
        }

     
    }
}
