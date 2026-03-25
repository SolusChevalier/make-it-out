using UnityEngine;

namespace MakeItOut.Runtime.GridSystem
{
    public class ChunkData
    {
        public Vector3Int ChunkCoord;
        public Vector3Int GridOrigin;
        public Vector3 WorldOrigin;
        public Mesh OpaqueMesh;
        public Mesh TransparentMesh;
        public bool IsMeshDirty;
        public bool IsActive;
    }
}
