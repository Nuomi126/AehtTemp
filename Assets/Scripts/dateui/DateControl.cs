using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DateControl : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{

    public enum ItemType { _year, _month, _day }

    public ItemType _itemtype;

    RectTransform conentRect;

    RectTransform targetRec;

    Vector3 oldDragPos;

    Vector3 newDragPos;

    public AnimationCurve curve_scale;//�ı��С����
    public AnimationCurve curve_color;//����Ч������


    List<Text> textList = new List<Text>();

    Button testBtn;

    float
        itemHeight,             //����item�ĸ�
        contentParentHeight,    //Content�ְֵĸ�
        itemNum,                //��������
        itemHeight_min,         //������С�����ı�λ��
        itemHeight_max,         //����������ı�λ��
        conentLimit,            //Conent����λ��
        conentSpacing;          //��������С

    float deltaX, deltaY;

    [HideInInspector]
    public static int _year, _month, _day;

    [HideInInspector]
    int dateItemNum;

    Color itemColor_hig = new Color32(255, 255, 255, 255);

    void Awake()
    {
        conentRect = transform.Find("Content").GetComponent<RectTransform>();
        targetRec = transform.parent.Find("HighlightTarget").GetComponent<RectTransform>();

    }

    void OnEnable()
    {
        ItemList();
    }

    void Start()
    {
        switch (_itemtype)
        {
            case ItemType._year: InstantiateData(15, 2017); break;
            case ItemType._month: InstantiateData(12, 12); break;
            case ItemType._day: InstantiateData(31, 31); break;
        }

        itemNum = transform.Find("Content").childCount - 1;

        contentParentHeight = conentRect.parent.GetComponent<RectTransform>().sizeDelta.y;

        conentSpacing = conentRect.GetComponent<VerticalLayoutGroup>().spacing / 2;

        itemHeight = textList[0].rectTransform.sizeDelta.y + conentSpacing;

        if (itemNum % 2 == 0) conentLimit = (itemHeight + 5) / 2;

        else conentLimit = 0;

        conentRect.anchoredPosition = new Vector2(conentRect.anchoredPosition.x, conentLimit);

        deltaX = textList[0].GetComponent<RectTransform>().sizeDelta.x;
        deltaY = textList[0].GetComponent<RectTransform>().sizeDelta.y;

        Invoke("ItemList", 0.05f);

    }

    /// <summary>
    /// ��������item
    /// </summary>
    /// <param name="itemNum">��������</param>
    /// <param name="dat">�������ֵ</param>
    void InstantiateData(int itemNum, int dat)
    {
        GameObject go;
        Text testObj = conentRect.Find("Text").GetComponent<Text>();
        for (int i = dat - itemNum + 1; i <= dat; i++)
        {
            go = Instantiate(testObj.gameObject, conentRect);
            go.GetComponent<Text>().text = i.ToString();
            go.name = i.ToString();
            textList.Add(go.GetComponent<Text>());
            ShowItem(true);
        }
        Destroy(conentRect.Find("Text").gameObject);
    }

    /// <summary>
    /// �����ӻ����
    /// </summary>
    /// <param name="isIncreaseOrdecrease"></param>
    void ShowItem(bool isIncreaseOrdecrease)
    {
        itemHeight_min = -itemHeight;

        if (_itemtype == ItemType._day) itemHeight_max = -itemHeight * itemNum - 95;
        else itemHeight_max = -itemHeight * itemNum;

        if (isIncreaseOrdecrease)
        {
            foreach (Text rectItem in textList)
            {
                if (rectItem.GetComponent<RectTransform>().anchoredPosition.y > itemHeight_min)
                {
                    print("+");
                    rectItem.transform.SetSiblingIndex((int)itemNum);
                }
            }
            print(itemHeight_min);
        }
        else
        {
            foreach (Text rectItem in textList)
            {
                if (rectItem.GetComponent<RectTransform>().anchoredPosition.y < itemHeight_max)
                {
                    print("-");
                    rectItem.transform.SetSiblingIndex(0);
                }
            }
            print(itemHeight_max);

        }
    }

    /// <summary>
    /// ����Ч�����ı��С��������ʾ
    /// </summary>
    void ItemList()
    {
        foreach (Text item in textList)
        {
            float indexA = Mathf.Abs(item.GetComponent<RectTransform>().position.y - targetRec.position.y);
            float indexSc_scale = Mathf.Abs(curve_scale.Evaluate(indexA / contentParentHeight));
            float indexSc_color = Mathf.Abs(curve_color.Evaluate(indexA / contentParentHeight));
            if (indexA < 15)
            {
                item.color = itemColor_hig;
                switch (_itemtype)
                {
                    case ItemType._year: _year = int.Parse(item.text); break;
                    case ItemType._month: _month = int.Parse(item.text); break;
                    case ItemType._day: _day = int.Parse(item.text); break;
                }
            }
            else item.color = new Color(0, 0, 0, 1 - indexSc_color);

            item.GetComponent<RectTransform>().localScale = new Vector3(1 - indexSc_scale, 1 - indexSc_scale * 3, 1 - indexSc_scale);
            //item.GetComponent<RectTransform>().sizeDelta = new Vector2(deltaX - (deltaX * indexSc), deltaY - (deltaY * indexSc));
        }

    }

    /// <summary>
    /// ��ȡint�������ڣ���ת��Ϊָ����ʽ
    /// </summary>
    /// <returns></returns>
    public static string GetDateInfo() { return _year + "-" + _month + "-" + _day; }

    /// <summary>
    /// ����Conentλ��
    /// </summary>
    void UpdateEx()
    {
        if (conentRect.anchoredPosition.y > conentLimit)
        {
            ShowItem(true);
            conentRect.anchoredPosition = new Vector2(conentRect.anchoredPosition.x, conentRect.anchoredPosition.y - itemHeight);
        }
        if (conentRect.anchoredPosition.y < conentLimit)
        {
            ShowItem(false);
            conentRect.anchoredPosition = new Vector2(conentRect.anchoredPosition.x, conentRect.anchoredPosition.y + itemHeight);
        }
    }

    /// <summary>
    /// ��ȡ��ק��Ϣ���ı�Conentλ��
    /// </summary>
    /// <param name="eventData"></param>
    void SetDraggedPosition(PointerEventData eventData)
    {
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(conentRect, eventData.position, eventData.pressEventCamera, out newDragPos))
        {
            newDragPos = eventData.position;
            if (Mathf.Abs(newDragPos.y - oldDragPos.y) >= itemHeight)
            {
                if (newDragPos.y > oldDragPos.y)
                {
                    conentRect.anchoredPosition = new Vector2(conentRect.anchoredPosition.x, conentRect.anchoredPosition.y + itemHeight);
                    oldDragPos += new Vector3(0, itemHeight, 0);
                    ItemList();
                }
                else
                {
                    conentRect.anchoredPosition = new Vector2(conentRect.anchoredPosition.x, conentRect.anchoredPosition.y - itemHeight);
                    oldDragPos -= new Vector3(0, itemHeight, 0);
                    ItemList();
                }
            }
        }
    }

    /// <summary>
    /// ����ʼ��ק
    /// </summary>
    /// <param name="eventData"></param>
    public void OnBeginDrag(PointerEventData eventData)
    {
        oldDragPos = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        SetDraggedPosition(eventData);
        UpdateEx();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        SetDraggedPosition(eventData);
        UpdateEx();
    }
}