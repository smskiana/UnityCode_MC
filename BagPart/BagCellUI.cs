using Bags;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class BagCellUI : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{

    [SerializeField] private Image image;
    [SerializeField] private TMP_Text text;
    [SerializeField] protected LayerMask CellBack;
    [SerializeField] protected LayerMask HotBar;
    public int Index;
    private RectTransform rectTransform;
    private RectTransform parentRect;
    private Canvas canvas;
    private Vector2 offset;
    private GameObject dragIcon;
    private RectTransform dragIconRect;
    private Image dragIconImage;
    public BagSystemUI bagSystemUI;
    public  BagSystem BagSystem 
    {
        get
        {
          if(!bagSystemUI)return null;
          return bagSystemUI.BagSystem;
        }
    }
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        parentRect = transform.parent as RectTransform;
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("DraggableButton 必须在 Canvas 下");
        }
    }
    public void ReFlesh(Sprite sprite,int count)
    {
        if(sprite == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("未设置精灵");
#endif      
        }
        else
        {
            image.sprite = sprite;
        }
        this.text.text = count.ToString();
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            float x = rectTransform.rect.width/2;
            float y = rectTransform.rect.height/2;
            bagSystemUI.MoveAndShowSplitPane(rectTransform.position+new Vector3(x,-y,0), Index);
        }
        else if(eventData.button == PointerEventData.InputButton.Left)
        {
            // 创建拖拽影子
            dragIcon = new GameObject("DragIcon");
            dragIcon.transform.SetParent(canvas.transform, false);
            dragIcon.transform.SetAsLastSibling(); // 最上层

            dragIconRect = dragIcon.AddComponent<RectTransform>();
            dragIconImage = dragIcon.AddComponent<Image>();

            // 拷贝 RectTransform（关键）
            CopyRectTransform(rectTransform, dragIconRect);

            // 统一使用世界坐标，避免 Canvas 不同层级导致偏移
            dragIconRect.position = rectTransform.position;

            // 拷贝 Image 属性（完整）
            dragIconImage.sprite = image.sprite;
            dragIconImage.color = image.color;
            dragIconImage.material = image.material;
            dragIconImage.preserveAspect = image.preserveAspect;
            dragIconImage.type = image.type;
            dragIconImage.fillAmount = image.fillAmount;
            dragIconImage.raycastTarget = false;

            // 隐藏原图标
            image.enabled = false;
            text.enabled = false;
        }      
    }
    public void OnDrag(PointerEventData eventData)
    {
        if (!dragIconRect) return;

        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector3 worldPos
        );

        dragIconRect.position = worldPos;
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        if (!dragIconRect) return;
       TrySwap(eventData);
        if (dragIcon)
            Destroy(dragIcon);
        image.enabled = true;
        text.enabled = true;
    }
    private bool TrySwap(PointerEventData eventData)
    {
        // 1️⃣ 获取所有 UI 被点击的对象
        List<RaycastResult> results = new();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            if(result.gameObject.TryGetComponent<CellBack>(out CellBack targetCell))
            {
#if UNITY_EDITOR
                Debug.Log($"交换 {targetCell.Index} {this.Index}");
#endif
                // 2️⃣ 调用 BagSystem 的交换方法
                return BagSystem.Swap(this.Index,targetCell.BagSystem ,targetCell.Index);
            }     
        }
        return false;
    }
    private void CopyRectTransform(RectTransform source, RectTransform target)
    {
        target.anchorMin = source.anchorMin;
        target.anchorMax = source.anchorMax;
        target.anchoredPosition = source.anchoredPosition;
        target.sizeDelta = source.sizeDelta;
        target.pivot = source.pivot;
        target.localRotation = source.localRotation;
        target.localScale = source.localScale;
    }
}