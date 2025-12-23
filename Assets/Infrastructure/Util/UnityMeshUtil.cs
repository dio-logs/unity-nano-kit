using System.Collections.Generic;
using UnityEngine;

namespace Infrastructure.Util
{
    public static class UnityMeshUtil
    {
        public enum FaceType { Bottom = 0, Top = 1, Front = 2, Back = 3, Left = 4, Right = 5 }

    /// <summary>
    /// 바닥 정점 4개와 높이를 이용해 육면체 메쉬를 생성합니다.
    /// </summary>
    /// <param name="bottomCorners">바닥의 4개 정점 (순서: 시계 방향 CW 권장)</param>
    /// <param name="height">육면체의 높이</param>
    /// <returns>생성된 Mesh 객체</returns>
    public static Mesh CreateHexahedron(Vector3[] bottomCorners, float height)
    {
        // 1. Input Validation
        if (bottomCorners == null || bottomCorners.Length != 4)
        {
            Debug.LogError("[HexahedronMeshUtils] Bottom corners must contain exactly 4 vertices.");
            return null;
        }

        Mesh mesh = new Mesh();
        mesh.name = "ProceduralHexahedron";

        // 2. Data Preparation
        // 정점 24개 (6면 * 4정점). 정점을 공유하지 않아야 Hard Edge(각진 모서리)와 개별 UV 처리가 가능함.
        List<Vector3> vertices = new List<Vector3>(24);
        List<Vector3> normals = new List<Vector3>(24);
        List<Vector2> uvs = new List<Vector2>(24);
        
        // 서브메쉬가 6개이므로, 삼각형 인덱스 배열도 6개 리스트로 관리
        List<int>[] submeshTriangles = new List<int>[6];
        for (int i = 0; i < 6; i++) submeshTriangles[i] = new List<int>(6);

        // 바닥 정점 (p0 ~ p3)
        Vector3 p0 = bottomCorners[0];
        Vector3 p1 = bottomCorners[1];
        Vector3 p2 = bottomCorners[2];
        Vector3 p3 = bottomCorners[3];

        // 천장 정점 (p4 ~ p7) - Up 벡터 방향으로 높이만큼 이동
        Vector3 up = Vector3.up * height;
        Vector3 p4 = p0 + up;
        Vector3 p5 = p1 + up;
        Vector3 p6 = p2 + up;
        Vector3 p7 = p3 + up;

        // 3. Build Faces (Vertex Splitting 방식)
        // 각 면마다 별도의 정점을 생성하여 리스트에 추가하고, 해당 서브메쉬의 인덱스를 갱신
        
        // Bottom (p0 -> p3 -> p2 -> p1) : Unity는 시계방향(CW)이 앞면. 바닥은 아래서 보므로 순서 뒤집기 고려 필요하나, 
        // 여기서는 바닥을 위에서 투과해서 보는게 아니라면 p0, p3, p2, p1 순서(Counter-Clockwise relative to top)가 바닥의 Normal을 아래로 향하게 함.
        AddFace(vertices, normals, uvs, submeshTriangles[(int)FaceType.Bottom], 
            p0, p3, p2, p1, Vector3.down);

        // Top (p4 -> p5 -> p6 -> p7)
        AddFace(vertices, normals, uvs, submeshTriangles[(int)FaceType.Top], 
            p4, p5, p6, p7, Vector3.up);

        // Front (p0 -> p1 -> p5 -> p4)
        AddFace(vertices, normals, uvs, submeshTriangles[(int)FaceType.Front], 
            p0, p1, p5, p4, Vector3.forward); // Normal은 대략적 방향, 실제로는 면에 수직으로 계산됨

        // Back (p2 -> p3 -> p7 -> p6)
        AddFace(vertices, normals, uvs, submeshTriangles[(int)FaceType.Back], 
            p2, p3, p7, p6, Vector3.back);

        // Left (p3 -> p0 -> p4 -> p7)
        AddFace(vertices, normals, uvs, submeshTriangles[(int)FaceType.Left], 
            p3, p0, p4, p7, Vector3.left);

        // Right (p1 -> p2 -> p6 -> p5)
        AddFace(vertices, normals, uvs, submeshTriangles[(int)FaceType.Right], 
            p1, p2, p6, p5, Vector3.right);

        // 4. Assign to Mesh
        // 2019.3+ 부터 SetVertices(List) 사용 권장 (GC 할당 감소)
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);

        mesh.subMeshCount = 6;
        for (int i = 0; i < 6; i++)
        {
            mesh.SetTriangles(submeshTriangles[i], i);
        }

        // 5. Optimization & Bounds
        mesh.RecalculateBounds(); // 바운딩 박스 재계산 (Culling에 필수)
        mesh.RecalculateTangents(); // 노멀맵 사용 시 필수
        mesh.Optimize(); // 정점 캐시 최적화 (GPU 성능 향상)

        return mesh;
    }

    /// <summary>
    /// 단일 면을 구성하는 헬퍼 메서드
    /// </summary>
    private static void AddFace(
        List<Vector3> verts, List<Vector3> norms, List<Vector2> uvs, List<int> tris,
        Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, Vector3 faceNormal)
    {
        int startIndex = verts.Count;

        // 정점 추가
        verts.Add(v0);
        verts.Add(v1);
        verts.Add(v2);
        verts.Add(v3);

        // UV 추가 (기본 0~1 매핑)
        uvs.Add(new Vector2(0, 0));
        uvs.Add(new Vector2(1, 0));
        uvs.Add(new Vector2(1, 1));
        uvs.Add(new Vector2(0, 1));

        // 노멀 추가 (하드 엣지를 위해 명시적 할당하거나, 4개 정점이 평면이라면 Cross Product로 계산 가능)
        // 여기서는 전달받은 faceNormal을 사용하거나, 실제 기하학적 노멀을 계산할 수 있음.
        // 정확도를 위해 기하학적 노멀 재계산:
        Vector3 computedNormal = Vector3.Cross(v1 - v0, v2 - v0).normalized;
        norms.Add(computedNormal);
        norms.Add(computedNormal);
        norms.Add(computedNormal);
        norms.Add(computedNormal);

        // 삼각형 인덱스 (Quad -> 2 Triangles)
        // Triangle 1: 0-2-1 (순서 중요: 시계방향이 전면) -> 여기서는 0, 1, 2 순서가 되어야 CW
        tris.Add(startIndex + 0);
        tris.Add(startIndex + 2);
        tris.Add(startIndex + 1);

        // Triangle 2: 0-3-2
        tris.Add(startIndex + 0);
        tris.Add(startIndex + 3);
        tris.Add(startIndex + 2);
    }
    }
}