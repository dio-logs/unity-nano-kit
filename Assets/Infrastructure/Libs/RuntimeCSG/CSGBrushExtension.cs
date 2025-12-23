using System.Collections.Generic;
using LibCSG;
using UnityEngine;

namespace Infrastructure.Libs.RuntimeCSG
{
    public static class CSGBrushExtension
    {
        // public static  void BuildBrushPreservingSubmeshes(this CSGBrush brush, Mesh unityMesh, Material[] originalMaterials)
        // {
        //     List<CSGBrush.Face> csgFaces = new List<CSGBrush.Face>();
        //     Vector3[] vertices = unityMesh.vertices;
        //     Vector3[] normals = unityMesh.normals;
        //     Vector2[] uvs = unityMesh.uv;
        //
        //     // 1. 서브메쉬 단위로 순회 (이게 핵심)
        //     for (int subMeshIndex = 0; subMeshIndex < unityMesh.subMeshCount; subMeshIndex++)
        //     {
        //         // 해당 서브메쉬의 삼각형 인덱스들을 가져옴
        //         int[] indices = unityMesh.GetTriangles(subMeshIndex);
        //         
        //         // 이 서브메쉬에 할당된 재질 (식별자 역할)
        //         Material subMeshMat = (originalMaterials != null && subMeshIndex < originalMaterials.Length) 
        //                               ? originalMaterials[subMeshIndex] 
        //                               : new Material(Shader.Find("Standard")); // Fallback
        //
        //         // 2. 삼각형 단위로 CSG Face 생성
        //         for (int i = 0; i < indices.Length; i += 3)
        //         {
        //             // 삼각형의 정점 3개 추출
        //             Vector3 v1 = vertices[indices[i]];
        //             Vector3 v2 = vertices[indices[i + 1]];
        //             Vector3 v3 = vertices[indices[i + 2]];
        //
        //             // UV 정보 추출 (필요 시)
        //             Vector2 uv1 = (uvs.Length > 0) ? uvs[indices[i]] : Vector2.zero;
        //             Vector2 uv2 = (uvs.Length > 0) ? uvs[indices[i+1]] : Vector2.zero;
        //             Vector2 uv3 = (uvs.Length > 0) ? uvs[indices[i+2]] : Vector2.zero;
        //
        //             // CSG Face 생성 (LibCSG-Runtime의 Vertex/Face 생성자에 맞게 조정 필요)
        //             // 보통 Face 생성자는 정점 리스트나 배열을 받습니다.
        //             List<Vertex> faceVerts = new List<ㅍerVertex>();
        //             faceVerts.Add(new Vertex(v1, normals[indices[i]], uv1));
        //             faceVerts.Add(new Vertex(v2, normals[indices[i+1]], uv2));
        //             faceVerts.Add(new Vertex(v3, normals[indices[i+2]], uv3));
        //
        //             CSGBrush.Face newFace = new CSGBrush.Face(faceVerts);
        //             
        //             // ★ 중요: 여기서 재질을 박아넣습니다 ★
        //             newFace.material = subMeshMat; 
        //
        //             csgFaces.Add(newFace);
        //         }
        //     }
        //
        //     // 3. 수동으로 만든 Face 리스트를 브러시에 주입하고 빌드
        //     brush.faces = csgFaces;
        //     brush.build_from_faces(csgFaces); // AABB, BSP Tree 등을 재계산하는 함수
        // }
    }
}