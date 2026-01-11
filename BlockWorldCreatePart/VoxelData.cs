using UnityEngine;
namespace WorldCreatation
{
    public static class VoxelData
    {
        public static readonly Vector3[] idToVec = new Vector3[6]
        {
            Vector3.up,      // 0
            Vector3.down,    // 1
            Vector3.left,    // 2
            Vector3.right,   // 3
            Vector3.forward, // 4
            Vector3.back     // 5
        };
        public static readonly Vector3Int[] voxelVerts = new Vector3Int[8]
        {
            Vector3Int.zero,                    //0
            Vector3Int.right,                   //1
            Vector3Int.up+Vector3Int.right,     //2
            Vector3Int.up,                      //3
            Vector3Int.forward,                 //4
            Vector3Int.forward+Vector3Int.right,//5
            Vector3Int.one,                     //6
            Vector3Int.up+Vector3Int.forward    //7
        };
        public static readonly int[,] voxelTris = new int[6, 4]
        {
            {6,2,7,3},//up
            {1,5,0,4},//dowm
            {4,7,0,3},//left
            {1,2,5,6},//right
            {5,6,4,7},//forward
            {0,3,1,2},//back
        };
        public static readonly int[] voxelTriIndex = new int[6]{ 0,1,2,2,1,3 };
        public static readonly Vector3Int[] voxelNormals = new Vector3Int[6]
        {
            Vector3Int.up,
            Vector3Int.down,
            Vector3Int.left,
            Vector3Int.right,
            Vector3Int.forward,
            Vector3Int.back,
        };
        public static Vector2[][] UvPos = new Vector2[6][]
        {
             new Vector2[]{ -Vector2Int.one, -Vector2Int.one, -Vector2Int.one, -Vector2Int.one },
             new Vector2[]{ -Vector2Int.one, -Vector2Int.one, -Vector2Int.one, -Vector2Int.one },
             new Vector2[]{ -Vector2Int.one, -Vector2Int.one, -Vector2Int.one, -Vector2Int.one },
             new Vector2[]{ -Vector2Int.one, -Vector2Int.one, -Vector2Int.one, -Vector2Int.one },
             new Vector2[]{ -Vector2Int.one, -Vector2Int.one, -Vector2Int.one, -Vector2Int.one },
             new Vector2[]{ -Vector2Int.one, -Vector2Int.one, -Vector2Int.one, -Vector2Int.one },
        };
        public static Vector2[] SetUvPos(int pos)
        {
            if (UvPos[pos][0]==-Vector2Int.one)
            {
                Vector2[] vector2s = new Vector2[4];
                int t = pos / 3;
                int i = pos % 3;
                float x = i * (1.0f / 3.0f);
                float y = t * 0.5f;
                vector2s[0] = new Vector2(x, y);
                vector2s[1] = new Vector2(x, y + 0.5f);
                x += 1.0f / 3.0f;
                if (x > 1) x = 1;
                vector2s[2] = new Vector2(x, y);
                vector2s[3] = new Vector2(x, y + 0.5f);
                UvPos[pos] = vector2s;
                return vector2s;
            }
            else
            {
                return UvPos[pos];
            }
          
        }
    }
}
