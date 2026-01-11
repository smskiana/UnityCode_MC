using System;
using System.Collections.Generic;
using UnityEngine;

namespace WorldCreatation
{
    /// <summary>
    /// Chuck（区块类）
    /// 管理一个体素区块（Voxel Chunk），负责：
    /// - 存储方块数据
    /// - 生成网格
    /// - 动态增删方块
    /// - 更新渲染
    /// </summary>
    public class Chuck
    {
        public Vector2Int pos;
        #region 所有数据
        //==================《区块基本信息》====================
        public const int WIDTH = 32;             // 区块宽度
        public static int HEIGHT { get => World.maxYpos; }
        public const int LENGTH = 32;            // 区块长度
        public const int FACE_COUNT = 6; //面的数量
        public const int VERTS_PER_FACE = 4;   //单个面顶点索引长度
        public const int TRI_PER_FACE = 6;   //单个面三角形索引长度
        //==================《存储块信息》=======================
        private readonly bool[,,] space;
        private readonly Dictionary<Vector3Int, Block> blocks;             // 方块坐标 -> Block 实例
        private readonly Dictionary<int, int> SubmeshIndex;                // Block类型ID -> Submesh索引
        //==================《记录网格信息》=====================
        private readonly List<(List<int> triangles, List<Block> blocks)> subMeshInfo;    //每个Submesh的整合信息 (三角形索引，块索引)
        private readonly List<Material> materials;                                       //每个submesh的材质信息。
        private readonly List<Vector2> uvs;                                              // UV坐标列表
        private Mesh mesh;                                                  // 当前网格对象                                              
        private readonly List<Vector3> verts;                                            // 顶点列表
        //=================《Unity组件信息》=====================
        private readonly ChunkView chunkView;
        public readonly Vector3Int position;
        //=================《动态记录信息》======================
        private readonly Stack<(Block blo, int Sub, int face)> addblock;  //记录待重新绘制的方块面（Block, Submesh索引, 面方向）                                                      
        private readonly List<(Block pos, int face)> secRemoveTris; // 次级记录需要删除的三角面 ，必须存在方块才会移除   
        private readonly List<(Block pos, int face)> secAddblock;  // 次级记录待重新绘制的方块面 ，必须存在方块才会添加
        private readonly List<(Block blo, int Sub, int face)> _removeTris;
        private bool hasdraw =false;

        #endregion

        #region 工具方法
        /// <summary>
        /// 判断是否为空气方块（没有或类型为Air）
        /// </summary>
        public bool IsBlockAir(Vector3Int pos)
        {
            if (blocks.TryGetValue(pos, out var block))
                return block.Type == BlockType.Air;
            return true;
        }
        public Vector3 ToworldPos(Vector3Int pos)
        {
           return position+pos;
        }
        public Vector3Int ToChunkPos(Vector3 pos)
        {
            return Vector3Int.FloorToInt(pos-position);
        }
        /// <summary>     
        /// 判断方块坐标是否在当前区块范围内
        /// </summary>
        /// <param id="pos"></param>
        /// <returns></returns>
        public bool IsVoxelInChunk(Vector3Int pos)=> 
            !(pos.x < 0 || pos.x >= WIDTH || pos.y < 0 || pos.y >= HEIGHT || pos.z < 0 || pos.z >= LENGTH);
        public bool IsFreeSpace(Vector3Int pos) =>
            IsVoxelInChunk(pos) && (!space[pos.x, pos.y, pos.z]);
        public bool HasDraw()
        {
           return hasdraw;
        }
        public void SetActive(bool v)
        {
            chunkView.gameObject.SetActive(v);
        }
        #endregion

        #region 初始化方法
        /// <summary>
        /// 初始化所有容器与组件
        /// </summary>
        ///       
        public Chuck(ChunkView view)
        {
            blocks = new Dictionary<Vector3Int, Block>();
            verts = new List<Vector3>();
            SubmeshIndex = new();
            materials = new List<Material>();
            subMeshInfo = new();
            uvs = new List<Vector2>();
            _removeTris = new();
            addblock = new();
            secRemoveTris = new();  
            secAddblock = new();
            position = Vector3Int.FloorToInt(view.transform.position);
            chunkView = view;
            space = new bool[WIDTH, HEIGHT, LENGTH];
        }
        /// <summary>
        /// 初始化整个区块的方块 TODO :需要移除
        /// </summary>
        public void InitBlock()
        {
           // TODO: 
            for (int x = 0; x < WIDTH; x++)
            {
                for (int z = 0; z < LENGTH; z++)
                {
                    int h = (int)World.Instance.plain.GetHeight(x+position.x,z+position.z,HEIGHT);
                    for (int y = 0; y < h; y++)
                    {
                        if (y == 0)
                            InitAdd(new(x, y, z), 3);
                        else if (y == h-1)
                            InitAdd(new(x, y, z), 1);
                        else
                            InitAdd(new(x, y, z), 2);
                    }              
                }
            }
        }

        /// <summary>
        /// 添加一个方块， 仅在初始化使用
        /// </summary>
        /// <param id="pos"> 区块的局部坐标 </param>
        /// <param id="id"> 方块id </param>
        private void InitAdd(Vector3Int pos, int id  = 0)
        {
            if (!IsFreeSpace(pos)) return;
            if (!blocks.ContainsKey(pos))
            {
                Block block = BlockFactory.GetInstance().GetBlock(id, pos);
                if (!SubmeshIndex.TryGetValue(id, out var p))
                {
                    p = SubmeshIndex.Count;
                    SubmeshIndex.Add(id, p);
                    if (Store.InfoStorer.Instance.TryFindBlock(id, out var s))
                        materials.Add(s.Material);
                    else
                        Debug.LogError($"无法找到Block信息: id{id}");
                    subMeshInfo.Add((new(), new()));
                } 
                // 检查每个方向是否需要绘制
                for (int face = 0; face < FACE_COUNT; face++)
                {
                    Vector3Int neighborPos = pos + VoxelData.voxelNormals[face];
                    bool DontneedDraw = World.Instance.CancelDrawFace(ToworldPos(neighborPos), Block.GetNeighborFace(face));
                    if (DontneedDraw)
                    {
                        block.SetFaceDrawn(face, false);
                    }
                    else
                    {
                        block.SetFaceDrawn(face, true);
                    }
                }      

                blocks.Add(pos, block);
                space[pos.x,pos.y,pos.z] = true;
                var b = subMeshInfo[p].blocks;
                block.PosInSubBlocks = b.Count;
                b.Add(block);

            }        
        }

        /// <summary>
        /// 根据所有方块生成网格,生成初始网格
        /// </summary>
        public void AddVoxelByBlocks()
        {
            foreach (var val in blocks.Values)
            {
                int pos = SubmeshIndex[val.ID];
                for (int t = 0; t < FACE_COUNT; t++)
                {
                    if (!val.IsNeedDrawFace(t))
                        continue;
                    DrawFace(val, t, pos);
                }
            }
        }

        public bool IsFreeBlock(Vector3 pos)
        {
            Vector3Int p = ToChunkPos(pos);
            if (IsVoxelInChunk(p))
            {
                return space[p.x, p.y, p.z];
            }
            else
            {
                return false;
            }
           
        }

        #endregion

        #region 网格修改方法
        /// <summary>
        /// 绘制单个方块面（添加顶点、三角形索引、UV）
        /// </summary>
        /// <param id="block"> 要添加面的所属方块 </param>
        /// <param id="facePos"> 面的位置 </param>
        /// <param id="subPos"> 所属顶点组的位置信息 </param>
        private void DrawFace(Block block, int facePos, int subPos = 0)
        {
            Vector3Int pos = block.PostionInChuck;
            int vertIndex;
            List<int> triangles = subMeshInfo[subPos].triangles;
            block.SetFaceDrawn(facePos, true, triangles.Count);
            int count = verts.Count;

            for (vertIndex = 0; vertIndex < VERTS_PER_FACE; ++vertIndex)
            {
                int triIndex = VoxelData.voxelTris[facePos, vertIndex];
                verts.Add(pos + VoxelData.voxelVerts[triIndex]);
                triangles.Add(count + VoxelData.voxelTriIndex[vertIndex]);
            }

            triangles.Add(count + VoxelData.voxelTriIndex[vertIndex++]);
            triangles.Add(count + VoxelData.voxelTriIndex[vertIndex]);
            uvs.AddRange(block.GetUvs(facePos));
        }
        /// <summary>
        /// 面信息替换
        /// </summary>
        /// <param id="sub"> 添加面的子网格索引 </param>
        /// <param id="block"> 添加面所属的块  </param>
        /// <param id="face"> 添加面的位置  </param>
        /// <param id="resub"> 替换面的子网格索引 </param>
        /// <param id="reblock"> 替换面所属的块  </param>
        /// <param id="reFace"> 替换面的位置  </param>
        private void RePlaceFace(int sub, Block block, int face,int resub,Block reblock, int reFace)
        {
            //准备数据
            Vector3Int addPos = block.PostionInChuck;
            int reTriPos = reblock.GetFirstTriangle(reFace);
            int reVertPos = subMeshInfo[resub].triangles[reTriPos];

            //提前生成面对应UV
            Vector2[] UVs = block.GetUvs(face);
          
            for (int vertIndex = 0; vertIndex < VERTS_PER_FACE; ++vertIndex)
            {
                int triIndex = VoxelData.voxelTris[face, vertIndex];
                verts[vertIndex + reVertPos] = addPos + VoxelData.voxelVerts[triIndex];
                uvs[vertIndex + reVertPos] = UVs[vertIndex];                
            }          
            //如果不在同一个子网格
            if (sub != resub)
            {
                //===========《添加逻辑》===========
                //TODO: 可以直接创建，而非建立缓冲
                //准备三角形索引
                int[] addtrislist = new int[TRI_PER_FACE];
                for (int i = 0; i < TRI_PER_FACE; ++i)
                {
                    addtrislist[i] = subMeshInfo[resub].triangles[reTriPos + i];
                }
                //在需要的子网格下续上这段索引
                block.SetFaceDrawn(face, true, subMeshInfo[sub].triangles.Count);
                //映射到block
                subMeshInfo[sub].triangles.AddRange(addtrislist);
                

                //=============《移除逻辑》============
                //需要保证移除队列按降序排列

                //找到末尾面第一个索引
                List<int> triangles = subMeshInfo[resub].triangles;
                int endTirPos = triangles.Count - TRI_PER_FACE;
                //将移除位置挪到末尾
                for(int i = 0;i<TRI_PER_FACE; ++i)
                {
                    triangles[reTriPos+i] = triangles[endTirPos+i];
                }
                //移除数据
                triangles.RemoveRange(endTirPos,TRI_PER_FACE);

                //如果子网格再无数据
                if (subMeshInfo[resub].blocks.Count == 0)
                {
                    ReMoveSubTris(resub);
                    return;
                }
                //找到这个末尾面对应block信息

                //如果 reTriPos == endTirPos 表示移除就是末尾不需要更新索引
                if (reTriPos == endTirPos) return;
                Block endBLock = null;
                int endFace = 0;
                foreach(var blo in subMeshInfo[resub].blocks)
                {
                    for (int i = 0;i<FACE_COUNT; ++i)
                    {
                        bool over = false;
                        if (blo.IsFaceDrawn(i)&&blo.GetFirstTriangle(i) == endTirPos)
                        {
                            endFace = i;
                            endBLock = blo;
                            //找到后退出
                            over = false;
                            break;
                        }
                        if (over) break;
                    }
                }                
                //更新数据
                endBLock?.SetFaceDrawn(endFace,true,reTriPos);
            }
            //否则，只需将block个关系映射过去
            else
            {
                block.SetFaceDrawn(face, true, reTriPos);
            }
        }
        /// <summary>
        /// 应用当前网格数据到Mesh组件
        /// </summary>
        public void UpdateMesh()
        {
            if (mesh == null)
            {
                mesh = new()
                {
                    vertices = verts.ToArray(),
                    uv = uvs.ToArray(),
                    subMeshCount = SubmeshIndex.Count
                };
                mesh.MarkDynamic();
                for (int i = 0; i < subMeshInfo.Count; i++)
                    mesh.SetTriangles(subMeshInfo[i].triangles.ToArray(), i);
                chunkView.SetMesh(mesh);
                hasdraw = true;
            }
            else
            {
                mesh.Clear();
                mesh.SetVertices(verts.ToArray());
                mesh.SetUVs(0, uvs);
                mesh.subMeshCount = subMeshInfo.Count;
                for (int i = 0; i < subMeshInfo.Count; i++)
                    mesh.SetTriangles(subMeshInfo[i].triangles.ToArray(), i);
            }
            mesh.RecalculateNormals(); // 更新法线
            chunkView.SetMesh(mesh);
            chunkView.SetMaterial(materials);
        }

        private void DeleteFace(int vertPos, int triPos, int pos)
        {
            List<int> triangles = subMeshInfo[pos].triangles;
            int originalVertCount = verts.Count;
            int srcVertStart = originalVertCount - VERTS_PER_FACE;
            int srcTriStart = triangles.Count - TRI_PER_FACE;

            // 1) 把末尾的 VERTS_PER_FACE 顶点/uv 移到被删除位置
            for (int i = 0; i < VERTS_PER_FACE; i++)
            {
                verts[vertPos + i] = verts[srcVertStart + i];
                uvs[vertPos + i] = uvs[srcVertStart + i];
            }
            // 4) 删除末尾的顶点/uv/三角
            verts.RemoveRange(srcVertStart, VERTS_PER_FACE);
            uvs.RemoveRange(srcVertStart, VERTS_PER_FACE);
               
            bool hasfound = false;
            List<int> reSubTri=null;
            int repos = 0;
            // 找到子网格里引用了末尾顶点 (>= srcVertStart) 的三角索引
            // 这些索引要被指向新位置 vertPos + (old - srcVertStart)
            for (int s = 0; s < subMeshInfo.Count; s++)
            {
                var triList = subMeshInfo[s].triangles;
                for (int i = 0; i < triList.Count; i++)
                {
                  
                    int idx = triList[i];
                    //找那些确实引用了被移动的末尾顶点的第一个顶点
                    if (idx == srcVertStart)
                    {
                        reSubTri = triList;
                        repos = i;
                        hasfound = true;    
                        break;
                    }
                    // 注意：其它索引（位于 [vertPos, srcVertStart)）不需要调整，因为我们没有在中间删除顶点
                }
                    if(hasfound) break;
            }
            //调整那些确实引用了被移动的末尾顶点
            if (reSubTri != null)
            {
                for (int i = repos; i < repos + TRI_PER_FACE; i++)
                {
                    int idx = reSubTri[i];
                    reSubTri[i] = vertPos + (idx - srcVertStart);            
                }
            }

            // 1) 把末尾的 移到被删除位置
            for (int i = 0; i < TRI_PER_FACE; i++)
            {
                triangles[i+triPos] = triangles[i+srcTriStart];
            }

          
           
            //移除三角形索引
            triangles.RemoveRange(srcTriStart, TRI_PER_FACE);

            // 5) 如果该 submesh 变空，移除并调整 SubmeshIndex、materials
            if (subMeshInfo[pos].blocks.Count == 0 && subMeshInfo[pos].triangles.Count==0)
            {
                ReMoveSubTris(pos);
                return;
            }
            //如果 triPos == srcTriStart 表示移除就是末尾不需要更新索引
            if (triPos == srcTriStart) return;
            hasfound =false;
            // 6) 更新当前 submesh 中 blocks 的 face 三角起始位置
            foreach (var block in subMeshInfo[pos].blocks)
            {
                for (int f = 0; f < FACE_COUNT; f++) 
                {
                    if (block.IsFaceDrawn(f))
                    {
                        int firstTri = block.GetFirstTriangle(f);
                        if (firstTri >= srcTriStart)
                        {
                            // 将指向末尾三角的索引映射到新的 triPos 位置

                            block.SetFaceDrawn(f, true, triPos);
                            hasfound = true;
                        }
                    }
                    if (hasfound) break;
                }
            }


           return;
        }

        #endregion

        #region 运行时修改方法
        //=====================《内部修改方法》================
        /// <summary>
        /// 标记要添加的方块
        /// </summary>
        public void AddBlo(int ID, Vector3Int pos)
        {
            if (!IsFreeSpace(pos)) return;
            if (!blocks.ContainsKey(pos))
            {
                Block block = BlockFactory.GetInstance().GetBlock(ID, pos);

                // 检查Submesh索引
                if (!SubmeshIndex.TryGetValue(ID, out int posInSubmesh))
                {
                    posInSubmesh = subMeshInfo.Count;
                    SubmeshIndex.Add(ID, posInSubmesh);
                    subMeshInfo.Add((
                        new(),
                        new()
                        ));
                    if(Store.InfoStorer.Instance.TryFindBlock(ID,out var info))
                        materials.Add(info.Material);
                    else
                        Debug.LogError($"无法找到Block信息: id{ID}");
                }
                // 依次处理每一个面
                for (int t = 0; t < FACE_COUNT; t++)
                {
                    AddFace(pos, block, posInSubmesh, t);
                }

                // 将新方块加入区块
                blocks.Add(pos, block);
                space[pos.x,pos.y,pos.z] = true;
                List<Block> subBlock = subMeshInfo[posInSubmesh].blocks;
                block.PosInSubBlocks = subBlock.Count;
                subBlock.Add(block);

            }
        }
        /// <summary>
        /// 标记要移除的方块
        /// </summary>
        public void RemoveBlo(Vector3Int pos)
        {
            if (blocks.TryGetValue(pos, out var block))
            {
               
                int posInSubmesh = SubmeshIndex[block.ID];
                for (int i = 0; i < FACE_COUNT; i++)
                {
                    RemveFace(pos, block, posInSubmesh, i);
                }

                // 删除方块数据
                blocks.Remove(pos);
                space[pos.x, pos.y, pos.z] = false;
                List<Block> _blocks = subMeshInfo[posInSubmesh].blocks;
                _blocks.RemoveAt(block.PosInSubBlocks);

                // 更新subBlock索引
                foreach (Block _block in _blocks)
                {
                    if (_block.PosInSubBlocks >= block.PosInSubBlocks)
                    {
                        _block.PosInSubBlocks--;
                    }
                       
                }
                BlockFactory.GetInstance().RecycleBlock(block); // 回收Block实例
            }

        }
        /// <summary>
        /// 应用方块的增删效果：增量更新网格
        /// </summary>
        public void UpdateMeshInfo()
        {
            // 步骤顺序：先删除 -> 再添加 -> 再更新绘制 -> 删除多余顶点
            SetSeAddFace();
            SetSeRemoveFace();          
            _removeTris.Sort((x,y)=>y.blo.GetFirstTriangle(y.face).CompareTo(x.blo.GetFirstTriangle(x.face)));
            int position  = AddFaces();
            DeleteFaces(position);
            _removeTris.Clear();
            addblock.Clear();  
            // ---------- 3️⃣ 重新绘制暴露面 ----------
            int AddFaces()
            {
                int pos = 0;
                while (addblock.Count > 0)
                {
                    var (block,sub,face) = addblock.Pop();
                    if (pos<_removeTris.Count)
                    {
                        var (reBlock, resSub, Reface) = _removeTris[pos];
                        RePlaceFace(sub,block,face,resSub,reBlock,Reface);   
                        pos++;
                    }
                    else
                    {
                        DrawFace(block, face, sub);
                    }
                }
                return pos;
            }
            // ---------- 4️⃣ 删除旧面并更新索引 ----------
            void DeleteFaces(int p)
            {
                for (int t = p; t < _removeTris.Count; t++)
                {
                    Block block = _removeTris[t].blo;
                    int face = _removeTris[t].face;
                    int sub = _removeTris[t].Sub;
                    int triPos = block.GetFirstTriangle(face);

                    if (triPos >= subMeshInfo[sub].triangles.Count || triPos < 0)
                    {
                        continue;
                    }
                    int vertPos = subMeshInfo[sub].triangles[triPos];
                    DeleteFace(vertPos, triPos, sub);
                }
            }
        }

        public bool DotNeedUpdateMeshInfo()=> (secRemoveTris.Count == 0&&secAddblock.Count==0&&_removeTris.Count == 0 && addblock.Count == 0);
        /// <summary>
        ///动态添加面方法
        /// </summary>
        /// <param id="pos"></param>
        /// <param id="block"></param>
        /// <param id="posInSubmesh"></param>
        /// <param id="face"></param>
        private void AddFace(Vector3Int pos, Block block, int posInSubmesh, int face)
        {
            Vector3Int neighborPos = pos + VoxelData.voxelNormals[face];
            if (IsBlockAir(neighborPos))
            {           
                if (IsVoxelInChunk(neighborPos) || neighborPos.y != pos.y)
                {
                    block.SetFaceDrawn(face, true);
                    addblock.Push((block,posInSubmesh,face));
                }
                else
                {
                    bool DontneedDraw = World.Instance.CancelDrawFace(ToworldPos(neighborPos), Block.GetNeighborFace(face));
                    if (DontneedDraw)
                    {
                        block.SetFaceDrawn(face, false);
                    }
                    else
                    {
                        block.SetFaceDrawn(face, true);
                        addblock.Push((block,posInSubmesh, face));
                    }
                }
            }
            else
            {
                block.SetFaceDrawn(face, false);
                if (blocks.TryGetValue(neighborPos, out Block val))
                {
                    int posface = Block.GetNeighborFace(face);
                    _removeTris.Add((val, SubmeshIndex[val.ID],posface));
                    val.SetFaceDrawn(posface, false);
                }
                    
            }
        }
        /// <summary>
        /// 应用次级添加面数据
        /// </summary>
        private void SetSeAddFace()
        {
            foreach (var (block,face) in secAddblock)
            {
                if (block.IsFaceDrawn(face)) break;
                int sub = SubmeshIndex[block.ID];
                addblock.Push((block, sub, face));
                block.SetFaceDrawn(face, true);
            }
            secAddblock.Clear();
        }
        private void SetSeRemoveFace()
        {
            foreach (var (block, face) in secRemoveTris)
            {
                if (!block.IsFaceDrawn(face)) break;
                int sub = SubmeshIndex[block.ID];
                int tirFace = block.GetFirstTriangle(face);
                if (tirFace >= subMeshInfo[sub].triangles.Count || tirFace < 0)
                {
                    continue;
                }
                _removeTris.Add((block, sub, face));
                block.SetFaceDrawn(face, false);
            }
            secRemoveTris.Clear();
        }
        private void RemveFace(Vector3Int pos, Block block, int posInSubmesh, int i)
        {
            Vector3Int neighborPos = pos + VoxelData.voxelNormals[i];
            if (!block.IsFaceDrawn(i))
            {
                int face = Block.GetNeighborFace(i);
                // 邻居暴露的面需要重新绘制
                if (blocks.TryGetValue(neighborPos, out Block val))
                {
                    int neiborPos = SubmeshIndex[val.ID];                 
                    addblock.Push((val, neiborPos, face));
                }
                else
                {
                    World.Instance.RequireDrawFace(ToworldPos(neighborPos), face);
                }
            }
            else
            {
                _removeTris.Add((block,posInSubmesh,i));   
                block.SetFaceDrawn(i, false);
            }
        }
        private void ReMoveSubTris(int pos)
        {
            subMeshInfo.RemoveAt(pos);
            materials.RemoveAt(pos);
            int keysToChange = 0;
            List<int> keys = new();
            foreach (var (key, value) in SubmeshIndex)
            {
                if (value > pos) keys.Add(key);
                if (value == pos) keysToChange = key;
            }
            foreach (var key in keys)
            {
                SubmeshIndex[key] -= 1;
            }
            SubmeshIndex.Remove(keysToChange);
            return;
        }
        //===================《外部联动方法》======================
        public bool DeleteFaceByWorldPos(Vector3 pos, int face) 
        {
            Vector3Int vector = ToChunkPos(pos);
            if (blocks.TryGetValue(vector, out var block))
            {
                if (block.IsFaceDrawn(face))
                    secRemoveTris.Add((block,face));
                return true;
            }
            return false;

        }
        public bool AddFaceByWorldPos(Vector3 pos, int face)
        {
            Vector3Int vector = ToChunkPos(pos);
            if (blocks.TryGetValue(vector, out var block))
            {       
                if (block.IsFaceDrawn(face)) return false;
                secAddblock.Add((block,face));
                return true;
            }
            return false;
        }

        #endregion

    }
}
