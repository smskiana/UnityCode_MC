using System.Collections.Generic;
using UnityEngine;


public enum BlockType
{
    Air,
    Solid,
}

namespace WorldCreatation {
    
    public class Block
    {
   
        private readonly int[] faceTable = new int[6] {-1,-1,-1,-1,-1,-1};
        private readonly bool[] facebool = new bool[6] {false,false,false,false,false,false };
        private readonly int[] mapping = new int[6] { 0, 1, 2, 3, 4, 5 };  
        private Vector3Int postionInChuck;
        private int posInSubBlocks = -1;
        public int ID { get; set; }     
        public BlockType Type { get; set; }
        public Vector3Int PostionInChuck { get => postionInChuck; set { 
                postionInChuck = value;
               
                } }
        public int PosInSubBlocks { get => posInSubBlocks; set { 
                posInSubBlocks = value; 
                //Debug.Log("方块" + postionInChuck + "postionInBLock更新 : " + value.ToString());
            } }
        public void Reflesh()
        {
            Type = BlockType.Air;
            postionInChuck = Vector3Int.zero;
            for (int i = 0; i < 6; i++) 
            {
                facebool[i] = false;
            }
            PosInSubBlocks = -1;
            ID = 0;
        }
        public void Reflesh(int id,Vector3Int pos)
        {
            ID = id;
            if(!Store.InfoStorer.Instance.TryFindBlock(id,out var buf))
                Debug.LogError($"无法找到Block信息: id{id}");
            this.Type = buf.BlockType;
            this.postionInChuck = pos;
        }
        public Block(int ID,Vector3Int postionInChuck)
        {
            if (!Store.InfoStorer.Instance.TryFindBlock(ID, out var buf))
                Debug.LogError($"无法找到正确信息，iD{ID}");
            this.Type =buf.BlockType;
            this.PostionInChuck = postionInChuck;
            this.ID = ID;
        }
        public static int GetNeighborFace(int n) => n%2==0? n+1:n-1;
        public bool IsFaceDrawn(int face) => facebool[face];     
        public bool IsNeedDrawFace(int face)=> faceTable[face] == -1&&facebool[face];     
        public void SetFaceDrawn(int face,bool needDraw = true ,int firstTriangleIndex=-1)
        {
            facebool[face] = needDraw;
            if (firstTriangleIndex != -1)
            {
                faceTable[face]= firstTriangleIndex;
            }
        }
        public int GetFirstTriangle(int face)
        {
            int firsttri = faceTable[face];
            return firsttri;
        }

        /// <summary>
        /// 输入本地方块坐标系的 Up、Front 向量（任意归一化方向）
        /// 自动建立六个面的映射。
        /// </summary>
        public void UpdateAxes(Vector3Int localUp, Vector3Int localFront)
        {
            // 必须正交，否则回默认
            if (localUp.x * localFront.x +
             localUp.y * localFront.y +
             localUp.z * localFront.z != 0)
            {
                ResetMapping();
                return;
            }

            // cross 用整数不会出误差
            Vector3Int localRight = new(
                localUp.y * localFront.z - localUp.z * localFront.y,
                localUp.z * localFront.x - localUp.x * localFront.z,
                localUp.x * localFront.y - localUp.y * localFront.x
            );

            Vector3Int localDown = -localUp;
            Vector3Int localLeft = -localRight;
            Vector3Int localBack = -localFront;

            mapping[0] = VecToId(localUp);
            mapping[1] = VecToId(localDown);
            mapping[2] = VecToId(localLeft);
            mapping[3] = VecToId(localRight);
            mapping[4] = VecToId(localFront);
            mapping[5] = VecToId(localBack);

            static int VecToId(Vector3Int v)
            {
                for (int i = 0; i < 6; i++)
                    if (VoxelData.idToVec[i] == v)
                        return i;
                return -1; // 无效方向
            }
        } 

        private void ResetMapping()
        {
            for (int i = 0; i < 6; i++)
                mapping[i] = i;
        }
       
        public Vector2[] GetUvs(int face)
        {
            return VoxelData.SetUvPos(mapping[face]);
        }   
    } 
    public class BlockFactory
    {
        private BlockFactory() { }

        private static BlockFactory instance;
        private readonly Queue<Block> blocksPool = new();

        public static  BlockFactory GetInstance()
        {
            if(instance==null)
            {
                instance=new BlockFactory();
            }
            return instance;
        }
        public Block GetBlock(int ID, Vector3Int posInChuck)
        {
            Block newBlock;
            if (blocksPool.Count == 0)
            {
               newBlock = new Block(ID,posInChuck);
            }
            else
            {
                newBlock = blocksPool.Dequeue();
                newBlock.Reflesh(ID,posInChuck);
            }
            return newBlock;    
        }
        public void RecycleBlock(Block block)
        {
            Debug.Log("回收方块");
            block.Reflesh();
            blocksPool.Enqueue(block);
        }        
    }
}

