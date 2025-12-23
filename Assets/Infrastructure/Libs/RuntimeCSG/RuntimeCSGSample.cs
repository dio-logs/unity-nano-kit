using UnityEngine;
using LibCSG;
using System.Collections.Generic;
using Infrastructure.Util;

namespace LibCSG
{
    /// <summary>
    /// A simple sample class to demonstrate Runtime CSG operation between two cubes.
    /// </summary>
    public class RuntimeCSGSample : MonoBehaviour
    {
        [SerializeField] private GameObject _cubeA;
        [SerializeField] private GameObject _cubeB;
        [SerializeField] private GameObject _result;
        public Operation operation = Operation.OPERATION_SUBTRACTION;

        void Start()
        {
            var wallBottomCorners = new List<Vector3>();
            wallBottomCorners.Add(new Vector3(0, 0, 0));
            wallBottomCorners.Add(new Vector3(1, 0, 0));
            wallBottomCorners.Add(new Vector3(1, 0, 1));
            wallBottomCorners.Add(new Vector3(0, 0, 1));
            var wallMesh = UnityMeshUtil.CreateHexahedron(wallBottomCorners.ToArray(), 4);
            
            // 1. Setup two cubes for CSG operation
            // GameObject cubeA = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _cubeA.name = "CubeA";
            _cubeA.transform.position = Vector3.zero;
            _cubeA.GetComponent<MeshFilter>().mesh = wallMesh;
            
            var doorBottomCorners = new List<Vector3>();
            doorBottomCorners.Add(new Vector3(0, 1, 0));
            doorBottomCorners.Add(new Vector3(0.5f, 1, 0));
            doorBottomCorners.Add(new Vector3(0.5f, 1, 0.5f));
            doorBottomCorners.Add(new Vector3(0, 1, 0.5f));
            var doorMesh = UnityMeshUtil.CreateHexahedron(doorBottomCorners.ToArray(), 1);

            // GameObject cubeB = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _cubeB.name = "CubeB";
            _cubeB.transform.position = Vector3.zero;
            _cubeB.GetComponent<MeshFilter>().mesh = doorMesh;

            // 2. Initialize Brushes from the cubes' meshes
            CSGBrush brushA = new CSGBrush(_cubeA);
            brushA.build_from_mesh(_cubeA.GetComponent<MeshFilter>().sharedMesh);
            // brushA.build_from_faces();

            CSGBrush brushB = new CSGBrush(_cubeB);
            brushB.build_from_mesh(_cubeB.GetComponent<MeshFilter>().sharedMesh);
            Debug.Log(brushB.getMesh().subMeshCount);

            // 3. Prepare the result brush
            CSGBrush resultBrush = new CSGBrush(_result);

            // 4. Perform the CSG operation
            CSGBrushOperation csgOperation = new CSGBrushOperation();
            csgOperation.merge_brushes(operation, brushA, brushB, ref resultBrush);

            // 5. Visualize the result
            GameObject resultObj = resultBrush.obj;
            resultObj.transform.position = new Vector3(2, 0, 0); // Move result to the side for visibility
            
            MeshFilter mf = resultObj.GetComponent<MeshFilter>();
            
            // Use a standard material to see the result
            // mr.material = new Material(Shader.Find("Standard"));
            var mesh = resultBrush.getMesh();
            mesh.RecalculateBounds();
            Debug.Log(mesh.subMeshCount);
            mf.mesh = resultBrush.getMesh();
            

            // 6. Optional: Disable original cubes to focus on the result
            // cubeA.SetActive(false);
            // cubeB.SetActive(false);
            
            Debug.Log($"CSG Operation {operation} completed. Resulting mesh has {mf.mesh.vertexCount} vertices.");
        }
    }
}
