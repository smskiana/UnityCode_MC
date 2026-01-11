using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Store;
using UnityEngine;
using Random = UnityEngine.Random;

namespace WorldCreatation
{
    /// <summary>
    /// 世界管理类
    /// 负责生成、管理和更新世界中的方块区域（Chunk）
    /// </summary>
    public class World : MonoBehaviour
    {
        #region 数据
        private Dictionary<Vector2Int, Chuck> chucks;
        private readonly HashSet<Vector2Int> lockedChucks = new ();
        private ChunkView chuckView;
        #region=============================<静态常量数据>=================================
        /// <summary>世界在X轴方向的总长度</summary>
        public static readonly int WorldSizeX = WorldChunkSizeX * Chuck.WIDTH;
        /// <summary>世界在Z轴方向的总长度</summary>
        public static readonly int WorldSiseZ = WorldChunkSizeZ * Chuck.LENGTH;
        /// <summary>世界在X轴方向的Chunk数量</summary>
        public const int WorldChunkSizeX = 32;
        /// <summary>世界在Z轴方向的Chunk数量</summary>
        public const int WorldChunkSizeZ = 32;
        /// <summary>X轴最小Chunk索引</summary>
        private static readonly int minXChuckPos = Tool.FloorDiv(-WorldChunkSizeX, 2);
        /// <summary>X轴最大Chunk索引</summary>
        private static readonly int maxXChuckPos = Tool.FloorDiv(WorldChunkSizeX, 2);
        /// <summary>Z轴最小Chunk索引</summary>
        private static readonly int minZChuckPos = Tool.FloorDiv(-WorldChunkSizeZ, 2);
        /// <summary>Z轴最大Chunk索引</summary>
        private static readonly int maxZChuckPos = Tool.FloorDiv(WorldChunkSizeZ, 2);
        /// <summary>X轴世界坐标最小值</summary>
        private static readonly int minXPos = minXChuckPos * Chuck.WIDTH;
        /// <summary>X轴世界坐标最大值</summary>
        private static readonly int maxXPos = maxXChuckPos * Chuck.WIDTH;
        /// <summary>Z轴世界坐标最小值</summary>
        private static readonly int minZPos = minZChuckPos * Chuck.LENGTH;
        /// <summary>Z轴世界坐标最大值</summary>
        private static readonly int maxZPos = maxZChuckPos * Chuck.LENGTH;
        /// <summary>Y轴最小坐标</summary>
        public const int minYpos = 0;
        /// <summary>Y轴最大坐标</summary>
        public const int maxYpos = 64;
        #endregion
        //==========================《玩家信息》===================================
 
        [SerializeField] private int viewDistace = 1;
        [SerializeField] private int extraLoadRange = 2;   
        public Transform playerPos;
        public PlainTerrainGenerator plain;
        public (int x, int z) lastUpdatePos;
        public List<(int x, int z)> oldChunkValue;
        public static World Instance { get; private set; }
        public event Action IsOver;
        [SerializeField] private float detectTime = 1f;
        [SerializeField] private int seed=0;
        private readonly string path = "Prefab/Chunk";
        private bool needDetect = false;
        private float detectTimer =  0f;
        #endregion

        #region Unity生命周期方法
        private void Awake()
        {
            Instance = this;
            chucks = new();
            if(playerPos==null) playerPos = Camera.main.transform;
            chuckView = Resources.Load<GameObject>(path).GetComponent<ChunkView>();
            seed = Random.Range(0, 10000);
            plain = new PlainTerrainGenerator(0.01f, seed);
           
        }
        private void Start()
        {
            InitMap(); // 初始化地图
        }
        private void Update()
        {
            detectTimer += Time.deltaTime;
            if (detectTimer > detectTime)
            {
                if (needDetect)
                {
                    UpdateChucks(); // 根据玩家移动更新Chunk显示
                }
                detectTimer = 0f;                    
            }
           
        }
        private void LateUpdate()
        {
            // 遍历所有Chunk，更新网格信息
            foreach (var chu in chucks.Values)
            {
                if(chu.DotNeedUpdateMeshInfo()) continue;
                if (!lockedChucks.Contains(chu.pos))
                {
                    lockedChucks.Add(chu.pos);
                    chu.UpdateMeshInfo();
                    chu.UpdateMesh();
                    lockedChucks.Remove(chu.pos);
                }                 
            }
        }
        #endregion

        #region Chunk管理方法
        /// <summary>
        /// 初始化地图，生成玩家视野内的Chunk
        /// </summary>
        private void InitMap()
        {
            lastUpdatePos = GetChuckLoc(playerPos.position);
            oldChunkValue = GetNeedDrawChunk(lastUpdatePos, viewDistace);
            var InitValue = GetNeedDrawChunk(lastUpdatePos, viewDistace+extraLoadRange);
            foreach ((int x, int z) in InitValue)
            {
                GameObject obj = Instantiate(chuckView.gameObject);
                ChunkView chucv = obj.GetComponent<ChunkView>();
                obj.transform.position = GetChuckPos(x, z);

                obj.name = $"{chuckView.name}:\t[{obj.transform.position}] ({x},{z})]";  
               
                obj.layer = gameObject.layer;
                Chuck chuc = new(chucv)
                {
                    pos = new(x, z)
                };
                chucks.Add(chuc.pos, chuc);
                obj.transform.parent = transform;
                if (Math.Abs(x - lastUpdatePos.x) > viewDistace ||Math.Abs(z - lastUpdatePos.z) > viewDistace)
                {
                    chucv.gameObject.SetActive(false);
                }
            }

            List<Chuck> buf = chucks.Values.ToList<Chuck>();
            if(InfoStorer.Instance != null)
                StartCoroutine(InitWorldCoroutine(buf));
        }

        /// <summary>
        /// 根据玩家所在Chunk位置，获取需要显示的Chunk索引列表
        /// </summary>
        private List<(int x, int z)> GetNeedDrawChunk((int x, int z) pos,int length)
        {
            var (x, z) = pos;
            List<(int x, int z)> values = new();

            for (int i = x - length; i <= x +length; i++)
            {
                for (int k = z - length; k <= z + length; k++)
                {
                    if (i < minXChuckPos || i >= maxXChuckPos || k < minZChuckPos || k >= maxZChuckPos) continue;
                    values.Add((i, k));
                }
            }
            return values;
        }
        /// <summary>
        /// 根据玩家移动，更新Chunk显示状态（隐藏/生成）
        /// </summary>
        private void UpdateChucks()
        {
            (int, int) buf;
            if ((buf = GetChuckLoc(playerPos.position)) != lastUpdatePos)
            {
                var (x, z) = buf;
                List<(int x, int z)> values = GetNeedDrawChunk(buf,viewDistace);
                var add = Remove(values, oldChunkValue);
                foreach (var(_x,_z)in add)
                {
                    if(lockedChucks.Contains(new(_x,_z))) return;
                }
                var remove = Remove(oldChunkValue, values);
                HideChucks(remove);
                AddChucks(add);
                oldChunkValue = values;
                lastUpdatePos = (x, z);
            }

            // 内部工具方法，用于列表差集
            static List<(int x, int z)> Remove(List<(int x, int z)> a, List<(int x, int z)> b)
            {
                List<(int x, int z)> values = new();
                foreach (var v in a)
                {
                    bool needAdd = true;
                    foreach (var v2 in b)
                    {
                        if (v == v2)
                        {
                            needAdd = false;
                            break;
                        }
                    }
                    if (needAdd) values.Add(v);
                }
                return values;
            }
        }
        /// <summary>
        /// 根据新加入的Chunk索引生成Chunk并加入协程生成队列
        /// </summary>
        private void AddChucks(List<(int x, int z)> add)
        {
            if (add.Count == 0) return;
            List<Chuck> chus = new();     
            
            
            foreach (var (x, z) in add)
            {       
                Chuck chu = AddChuck(new(x,z));
                if (chu != null)
                    chus.Add(chu);
            }
            if(chus.Count==0)return;
            StartCoroutine(AddChucksCoroutine(chus));
        }
        /// <summary>
        /// 根据索引添加单个Chunk
        /// </summary>
        private Chuck AddChuck(Vector2Int pos)
        {
     
            if (chucks.TryGetValue(pos, out var chu))
            {
                chu.SetActive(true); // 已存在则激活
                return null;
            }
            else
            {
              
                GameObject obj = Instantiate(chuckView.gameObject);
                obj.transform.position = this.GetChuckPos(pos.x, pos.y);
                var (i, j) = (pos.x, pos.y);
                obj.name = $"{chuckView.name}:\t[{obj.transform.position}] ({i},{j})]";
                obj.layer = gameObject.layer;
                Chuck chuc = new(obj.GetComponent<ChunkView>())
                {
                    pos = pos
                };
                chucks.Add(chuc.pos, chuc);
                obj.transform.parent = transform;
                return chuc;
            }
        }
        /// <summary>
        /// 隐藏指定Chunk列表
        /// </summary>
        private void HideChucks(List<(int x, int z)> remove)
        {
            foreach (var v in remove)
                HideChuck(v);
        }
        /// <summary>
        /// 隐藏单个Chunk
        /// </summary>
        private void HideChuck((int x, int z) v)
        {
            if (chucks.TryGetValue(new(v.x,v.z), out var chu))
                chu.SetActive(false);
        }
        /// <summary>
        /// 尝试通过世界坐标获取Chunk
        /// </summary>
        private bool TryGetChuck(Vector3 pos, out Chuck chu)
        {
            var (x, z) = GetChuckLoc(pos);
            return chucks.TryGetValue(new(x,z), out chu);
        }
        /// <summary>获取位置所在的Chunk索引</summary>
        private (int x, int z) GetChuckLoc(Vector3 pos) =>
            (Tool.FloorDiv((int)pos.x, Chuck.WIDTH), Tool.FloorDiv((int)pos.z, Chuck.LENGTH));
        /// <summary>通过Chunk索引获取Chunk世界坐标</summary>
        private Vector3 GetChuckPos(int x, int z) =>
            new(x * Chuck.WIDTH, 0, z * Chuck.LENGTH);

        #endregion

        #region 地形生成协程与线程方法
        /// <summary>
        /// 主线程生成地形
        /// </summary>
        public void StartJob(List<Chuck> chucks)
        {
            needDetect = false;
            foreach (Chuck chuck in chucks)
            {
                chuck.InitBlock();
                chuck.AddVoxelByBlocks();
            }
            foreach (Chuck chuck in chucks)
                chuck.UpdateMesh();

            needDetect = true;
        }

        /// <summary>
        /// 子线程生成地形协程
        /// </summary>
        private IEnumerator AddChucksCoroutine(List<Chuck> chucks)
        { 
            List<Vector2Int> lists = new();
            foreach (Chuck chuck in chucks)
            {
                if (lockedChucks.Add(chuck.pos))
                {
                    lists.Add(chuck.pos);
                }
            }
            var task = Task.Run(() =>
            {
                foreach (Chuck chuck in chucks) chuck.InitBlock();               
                foreach (Chuck chuck in chucks) chuck.AddVoxelByBlocks();
            });

            while (!task.IsCompleted)
            {
                yield return null;
            }

            if (task.Exception != null)
            {
                throw task.Exception.Flatten();
            }
            int chunksPerFrame = 1;
            for (int i = 0; i < chucks.Count; i++)
            {
                chucks[i].UpdateMesh();
                if ((i + 1) % chunksPerFrame == 0)
                {
                    yield return null;
                }                  
            }      
            foreach (Vector2Int pos in lists)
            {
                lockedChucks.Remove(pos);
            }

        }
        private IEnumerator InitWorldCoroutine(List<Chuck> chucks)
        {
            needDetect = false;
            InfoStorer storer = InfoStorer.Instance;
            
            while (!storer.IsBlockLoaded)
            {
                yield return null;
            }
            Debug.Log("开始加载地形");
            var task = Task.Run(() =>
            {
                foreach (Chuck chuck in chucks) chuck.InitBlock();               
                foreach (Chuck chuck in chucks) chuck.AddVoxelByBlocks();
            });

            while (!task.IsCompleted)
            {
                yield return null;
            }

            if (task.Exception != null)
                throw task.Exception.Flatten();
            int chunksPerFrame = 1;
            for (int i = 0; i < chucks.Count; i++)
            {
                chucks[i].UpdateMesh();
                if ((i + 1) % chunksPerFrame == 0)
                {
                    yield return null;
                }             
            }
            needDetect = true;
            IsOver?.Invoke();
            IsOver = null;  
        }
        
        #endregion

        #region 请求Chunk相关操作
        /// <summary>
        /// 判断坐标是否在世界范围内
        /// </summary>
        public bool IsInWorld(Vector3 pos)
        {
            int x = (int)pos.x, z = (int)pos.z, y = (int)pos.y;
            return x >= minXPos && x < maxXPos && z >= minZPos && z < maxZPos && y >= minYpos && y < maxYpos;
        }

        /// <summary>
        /// 取消绘制指定位置的面
        /// </summary>
        /// <param id="pos"> 世界坐标 </param>
        /// <param id="face"> 面ID </param>
        /// <returns> 是否成功 </returns>
        public bool CancelDrawFace(Vector3 pos, int face)
        {
            if (!IsInWorld(pos)) return false;
          
            if (needDetect&&TryGetChuck(pos, out var chu)&& !lockedChucks.Contains(chu.pos))
            {
                bool isr = chu.DeleteFaceByWorldPos(pos, face);
                return isr;
            }
            else
            {
                //TODO :更新地形生成方法时更新
                int y =(int)plain.GetHeight((int)pos.x,(int)pos.z,maxYpos);
               
                int p_y = (int)pos.y;
                if(p_y < y) return true;
                else return false;
            }
        }

        /// <summary>
        /// 请求绘制指定位置的面
        /// </summary>
        public bool RequireDrawFace(Vector3 pos, int face)
        {
            if (TryGetChuck(pos, out var chu))
                return chu.AddFaceByWorldPos(pos, face);
            return false;
        }
        public bool IsFeelSpace(Vector3Int pos)
        {
            if (!IsInWorld(pos)) return false;

            if (needDetect && TryGetChuck(pos, out var chu) && !lockedChucks.Contains(chu.pos))
            {
                return chu.IsFreeSpace(chu.ToChunkPos(pos));
            }
            else
            {
                //TODO :更新地形生成方法时更新
                int y =plain.GetHeight((int)pos.x, (int)pos.z, maxYpos);
                int p_y = pos.y;
                if (p_y >= y) return true;
                else return false;
            }

        }
        #endregion

        #region Player 外部查询   
        public Vector3Int GetRightPos(Vector3Int pos)
        {
            if (IsFeelSpace(pos + Vector3Int.down))
            {
                return pos + Vector3Int.down;
            }
            if (!IsFeelSpace(pos))
            {
                if (IsFeelSpace(pos + Vector3Int.up))
                {
                    return pos+Vector3Int.up;
                }
            }
            return pos;
        }
        public void PlaceBlock(BlocksInfo BlockInfo , Vector3Int pos)
        {
            if( IsFeelSpace(pos + Vector3Int.down)      &&
                IsFeelSpace(pos + Vector3Int.right)     &&
                IsFeelSpace(pos + Vector3Int.left)      &&
                IsFeelSpace(pos + Vector3Int.forward)   &&
                IsFeelSpace(pos + Vector3Int.back)) return;
            if(TryGetChuck(pos,out var chuck))
            {
                Debug.Log("放置："+pos);
                chuck.AddBlo(BlockInfo.ID,chuck.ToChunkPos(pos));
            }
        }
        public void RePlaceBlock(Vector3Int pos)
        {
            if (TryGetChuck(pos, out var chuck))
            {
                chuck.RemoveBlo(chuck.ToChunkPos(pos));
            }
        }
        public int GetInitHeight(int x, int z)
        {
            return plain.GetHeight(x,z,maxYpos);
        }

        #endregion

        #region 寻路算法
      
        #endregion
    }
}
