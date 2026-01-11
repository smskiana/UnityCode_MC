using Bags;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BagSystemUI : MonoBehaviour
{
    #region ===== Inspector =====

    [Header("背包系统")]
    [SerializeField] private WeakReference<BagSystem> bagSystem;
    [SerializeField] private BagSystem blindBagSystem;

    [Header("格子 Prefab")]
    [SerializeField] private BagCellUI bagCellPrefab;
    [SerializeField] private CellBack cellBackPrefab;
    [SerializeField] private GameObject SplitPanel;

    [Header("布局设置")]
    [SerializeField] private int maxColumns = 5;
    [SerializeField]float minSpacing = 5f;
    [SerializeField] private Vector2 cellSize = new(100, 100);
    [SerializeField] private float horizontalPadding = 10f;
    //轴心最好在上边界
    [SerializeField] private RectTransform backGround;
    [SerializeField] private RectTransform context;
    [SerializeField]Slider splitSlider;
    #endregion

    #region ===== Runtime Data =====
    private Vector2 usedCellSize;
    public readonly List<BagCellUI> BagCellUIs = new();
    public readonly List<CellBack> CellBackUIs = new();
    private int currentBagSize;
    private int splitCellPos = -1;

    public BagSystem BagSystem { get {
            BagSystem buf = null;
            bagSystem?.TryGetTarget(out buf);
            return buf;
        } set => bagSystem = new(value); }

    #endregion

    #region ===== Unity =====
    private IEnumerator Start()
    {
        yield return 2f;
        if(blindBagSystem != null)
        {
            ResetBagSystem(blindBagSystem);
        }
    }
    private void OnDestroy()
    {
        if (BagSystem != null)
            UnsubscribeEvents();
    }
    private void OnDisable()
    {
        if (BagSystem != null)
        {
            UnsubscribeEvents();
            BagSystem=null; 
        }
           
    }
    #endregion

    #region ===== Layout =====

    private void GenerateInitialLayout()
    {
        CalculateLayout(BagSystem.Size, out int columns, out float spacingX);
        ResizeContainer(BagSystem.Size, columns, spacingX);

        for (int i = 0; i < BagSystem.Size; i++)
        {
            Vector2 pos = CalculateCellPosition(i, columns, spacingX);

            CreateBackCell(i, pos);
            CreateOrUpdateItemCell(i, pos);
        }
    }

    private void RebuildLayout(int totalSize)
    {
        CalculateLayout(totalSize, out int columns, out float spacingX);
        ResizeContainer(totalSize, columns, spacingX);
        EnsureListSize(totalSize);

        for (int i = 0; i < totalSize; i++)
        {
            Vector2 pos = CalculateCellPosition(i, columns, spacingX);

            CreateBackCell(i, pos);
            CreateOrUpdateItemCell(i, pos);
        }

        HideOverflowCells(totalSize);
    }

    private void CalculateLayout(int totalSize, out int columns, out float spacingX)
    {
        float containerWidth = context.rect.width;

        columns = Mathf.Min(maxColumns, totalSize);
        usedCellSize = cellSize;

        while (columns > 1)
        {
            float need =
                columns * cellSize.x +
                (columns - 1) * minSpacing +
                2 * horizontalPadding;

            if (need <= containerWidth) break;
            columns--;
        }

        if (columns <= 1)
        {
            columns = 1;
            float available = containerWidth - 2 * horizontalPadding;
            usedCellSize.x = Mathf.Max(available, 20f);
            spacingX = 0;
        }
        else
        {
            spacingX = (containerWidth - 2 * horizontalPadding - columns * usedCellSize.x)
                       / (columns - 1);
        }
    }

    private void ResizeContainer(int totalSize, int columns, float spacingX)
    {
        int rows = Mathf.CeilToInt((float)totalSize / columns);
        float height = rows * usedCellSize.y +
                       (rows > 1 ? (rows - 1) * spacingX : 0);

        context.sizeDelta = new(context.sizeDelta.x, height);
        backGround.sizeDelta = new(backGround.sizeDelta.x, height);
    }

    private Vector2 CalculateCellPosition(int index, int columns, float spacingX)
    {
        int row = index / columns;
        int col = index % columns;

        float x = horizontalPadding + col * (usedCellSize.x + spacingX);
        float y = -row * (usedCellSize.y + spacingX);
        return new Vector2(x, y);
    }

    #endregion

    #region ===== Create / Update Cells =====

    private void CreateBackCell(int index, Vector2 pos)
    {
        while ( index >= CellBackUIs.Count)
        {
            CellBackUIs.Add(null);
        }
        if (CellBackUIs[index] == null)
        {
            var back = Instantiate(cellBackPrefab, backGround);
            back.UI = this;
            CellBackUIs[index] = back;
            CellBackUIs[index].Index = index;          
            SetupRect(CellBackUIs[index].GetComponent<RectTransform>(), pos);
        }
        else
        {
            CellBackUIs[index].gameObject.SetActive(true);
        }
    }

    private void CreateOrUpdateItemCell(int index, Vector2 pos)
    {
        var (item, count) = BagSystem.Peek(index);
        while (index >= BagCellUIs.Count)
        {
            BagCellUIs.Add(null);
        }

        if (count<=0||item==null)
        {
            if (BagCellUIs[index] != null)
            {
                Destroy(BagCellUIs[index].gameObject);
                BagCellUIs[index] = null;
            }
            return;
        }

        if (BagCellUIs[index] == null)
        {
            BagCellUIs[index] = Instantiate(bagCellPrefab, context);
        }

        var cell = BagCellUIs[index];
        cell.gameObject.SetActive(true);
        cell.bagSystemUI = this;
        cell.Index = index;
        cell.ReFlesh(item.ItemInfo.Image,count);
        SetupRect(cell.GetComponent<RectTransform>(), pos);
    }

    private void HideOverflowCells(int validSize)
    {
        for (int i = validSize; i < CellBackUIs.Count; i++)
        {
            if (CellBackUIs[i] != null)
                CellBackUIs[i].gameObject.SetActive(false);

            if (BagCellUIs[i] != null)
            {
                Destroy(BagCellUIs[i].gameObject);
                BagCellUIs[i] = null;
            }
        }
    }

    #endregion

    #region ===== Events =====

    private void SubscribeEvents()
    {
        BagSystem.RegisterItemRefreshed(OnItemRefreshed);
        BagSystem.RegisterItemSwapped(OnItemSwapped);
        BagSystem.RegisterAllItemsRefreshed(OnAllItemsRefreshed);
        BagSystem.RegisterSizeChanged(OnBagSizeChanged);
    }

    private void UnsubscribeEvents()
    {
        BagSystem.UnregisterItemRefreshed(OnItemRefreshed);
        BagSystem.UnregisterItemSwapped(OnItemSwapped);
        BagSystem.UnregisterAllItemsRefreshed(OnAllItemsRefreshed);
        BagSystem.UnregisterSizeChanged(OnBagSizeChanged);
    }

    private void OnItemRefreshed(int index)
    {
        Vector2 pos = CellBackUIs[index].GetComponent<RectTransform>().anchoredPosition;
        CreateOrUpdateItemCell(index, pos);
    }

    private void OnItemSwapped(int a, int b)
    {
        (BagCellUIs[a], BagCellUIs[b]) = (BagCellUIs[b], BagCellUIs[a]);

        UpdateSwapCell(a);
        UpdateSwapCell(b);
    }

    private void UpdateSwapCell(int index)
    {
        if (BagCellUIs[index] == null) return;
        var cell = BagCellUIs[index];
        cell.Index = index;
        var backRect = CellBackUIs[index].GetComponent<RectTransform>();
        cell.GetComponent<RectTransform>().position = backRect.position;
    }

    private void OnAllItemsRefreshed()
    {
        for (int i = 0; i < BagSystem.Size; i++)
            OnItemRefreshed(i);
    }

    private void OnBagSizeChanged(int newSize)
    {
        if (newSize == currentBagSize) return;

        RebuildLayout(newSize);
        currentBagSize = newSize;
    }

    public void OnSortBottonDown()
    {
        if(BagSystem!=null)
            BagSystem.SortAndMerge();
    }

    /// <summary>
    /// 重新设置并绑定一个新的 BagSystem
    /// 会自动取消旧事件、清空 UI、重新生成布局
    /// </summary>
    public void ResetBagSystem(BagSystem newBagSystem)
    {
        if (BagSystem == newBagSystem)
            return;

        // 取消旧背包事件
        if (BagSystem != null)
            UnsubscribeEvents();

        BagSystem = newBagSystem;

        // 清空旧 UI
        ClearAllCells();

        if (BagSystem == null)
        {
            currentBagSize = 0;
            return;
        }

        // 重新生成
        GenerateInitialLayout();
        currentBagSize = BagSystem.Size;

        // 订阅新背包事件
        SubscribeEvents();
    }
    private void ClearAllCells()
    {
        foreach (var cell in BagCellUIs)
        {
            if (cell != null)
                cell.gameObject.SetActive(false);
        }

        foreach (var back in CellBackUIs)
        {
            if (back != null)
                Destroy(back.gameObject);
        }

        BagCellUIs.Clear();
        CellBackUIs.Clear();
    }

    #endregion

    #region ===== Utils =====

    private void EnsureListSize(int size)
    {
        while (CellBackUIs.Count < size) CellBackUIs.Add(null);
        while (BagCellUIs.Count < size) BagCellUIs.Add(null);
    }
    private static void SetupTopLeftRect(RectTransform rect)
    {
        rect.anchorMin = Vector2.up;
        rect.anchorMax = Vector2.up;
        rect.pivot = Vector2.up;
    }
    private void SetupRect(RectTransform rect, Vector2 pos)
    {
        SetupTopLeftRect(rect);
        rect.sizeDelta = usedCellSize;
        rect.anchoredPosition = pos;
    }
    public void MoveAndShowSplitPane(Vector2 pos,int posinbag)
    {
        if (SplitPanel==null) return;
        splitCellPos = posinbag;
        SplitPanel.GetComponent<RectTransform>().position = pos;
        var (_, count) = BagSystem.Peek(posinbag);
        int maxCount = count;
        splitSlider.maxValue = maxCount;
        splitSlider.minValue = 0;
        splitSlider.value = maxCount / 2;
        SplitPanel.SetActive(true);
    }
    public void OnSplitDown()
    {
       if(splitCellPos >= 0)
        {
            if (BagSystem != null)
            {
                BagSystem.Split(splitCellPos, (int)splitSlider.value);
            }
        }
        splitCellPos = -1;
    }

    #endregion
}
