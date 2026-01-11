using UnityEngine;

public class PlainTerrainGenerator
{
    private float noiseScale;
    private float offsetX;
    private float offsetZ;

    /// <summary>
    /// 构造平原地形生成器
    /// </summary>
    /// <param id="noiseScale">噪声缩放（越大越平坦）</param>
    /// <param id="seed">随机种子</param>
    public PlainTerrainGenerator(float noiseScale = 0.01f, int seed = 0)
    {
        this.noiseScale = noiseScale;

        System.Random rand = new System.Random(seed);
        offsetX = rand.Next(-10000, 10000);
        offsetZ = rand.Next(-10000, 10000);
    }

    /// <summary>
    /// 获取平原地形高度
    /// </summary>
    /// <param id="x">平面 x 坐标</param>
    /// <param id="z">平面 z 坐标</param>
    /// <param id="h">高度上限（最大高度）</param>
    /// <returns>返回高度值(0~h)</returns>
    public int GetHeight(int x, int z, float h)
    {
        // 平缓噪声
        float nx = (x + offsetX) * noiseScale;
        float nz = (z + offsetZ) * noiseScale;

        float noiseValue = Mathf.PerlinNoise(nx, nz); // 范围 0~1
        
        int buf = (int)(noiseValue * h);
        buf = buf < 2 ? 2 : buf;
        return buf;
    }
}

public class TreesCenerator 
{


}

