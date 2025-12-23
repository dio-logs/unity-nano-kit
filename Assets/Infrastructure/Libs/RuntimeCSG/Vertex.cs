using UnityEngine;

namespace Infrastructure.Libs.RuntimeCSG
{
    public struct Vertex
    {
        public Vector3 Position { get; }
        public Vector3 Normal { get; }
        public Vector3 UV { get; }
        public Vertex(Vector3 position, Vector3 normal, Vector2 uv)
        {
            Position = position;
            Normal = normal;
            UV = uv;
        }
    }
}