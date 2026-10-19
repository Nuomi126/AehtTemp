using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements;
using XCharts.Runtime;
//public class TouchZoom : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
public class TouchZoom : MonoBehaviour
{
	public RawImage tableBackImage;
	public TMP_Text localScale;
	public LineChart lineChart;
	public GameObject glineChart;
	private Vector2 startPinchDistance;
	private Vector2 startTouchPosition;
	private Vector3 initPosition;
	private float startAspect;
	private RectTransform rectTransform;
	private float initialScale;
	private float initialYScale;
	private float minScale = 1f;
	private float maxScale = 3f;
	private float zoomSpeed = 1.2f;
	private RectTransform contentRect;
	private int chartFlag; // ��־λ��0��ʾûѡ������ͼ��1��ʾѡ������ͼ

	private RectTransform panelRectTransform;
	private CanvasGroup canvasGroup;
	private Vector2 originalLocalPointerPosition;
	private Vector3 originalPanelLocalPosition;
	private RectTransformUtility rectTransformUtility;

	void Start()
	{
		rectTransform = tableBackImage.GetComponent<RectTransform>();
		contentRect = tableBackImage.GetComponent<RectTransform>();
		initialScale = rectTransform.localScale.x; // ��¼��ʼ���ű���
		initialYScale = rectTransform.localScale.y; // ��¼��ʼ���ű���	
		startPinchDistance = Vector2.zero;
		// ����ͼ��û��ѡ��
		chartFlag = 0;
	}

	//public void OnBeginDrag(PointerEventData eventData)
	//{

	//	Debug.Log("OnBeginDrag");
	//	if (Input.touchCount == 2) // ȷ����������ָ����
	//	{
	//		startPinchDistance = GetPinchDistance(Input.GetTouch(0).position, Input.GetTouch(1).position);
	//		startAspect = GetPinchAspect(Input.GetTouch(0).position, Input.GetTouch(1).position);
	//	}
	//}
	////public void OnDrag(PointerEventData eventData)
	//public void OnDrag(PointerEventData eventData)
	//{
	//	Debug.Log($"OnDrag = {Input.mousePosition}");
	//	if (Input.touchCount == 2) // ȷ����������ָ����
	//	{


	//		Vector2 currentPinchDistance = GetPinchDistance(Input.GetTouch(0).position, Input.GetTouch(1).position);
	//		float currentAspect = GetPinchAspect(Input.GetTouch(0).position, Input.GetTouch(0).position);
	//		float scaleFactor = currentPinchDistance.magnitude / startPinchDistance.magnitude; // ������������
	//		float aspectFactor = currentAspect / startAspect; // ������߱ȱ仯���ӣ���ѡ��
	//		float newScale = initialScale * scaleFactor * aspectFactor; // Ӧ�����źͿ��߱ȱ仯
	//		rectTransform.localScale = new Vector3(newScale, newScale, 1); // Ӧ���µ�����ֵ
	//	}

	//}

	//public void OnEndDrag(PointerEventData eventData) { } // ���Բ�ʵ�ֻ����գ���Ϊ����Ҫ����Ľ�������

	private Vector2 GetPinchDistance(Vector2 touchZero, Vector2 touchOne)
	{
		return touchZero - touchOne; // ��������������֮��ľ�������
	}

	private float GetPinchAspect(Vector2 touchZero, Vector2 touchOne)
	{
		return (touchZero - (Vector2)Camera.main.transform.position).magnitude / (touchOne - (Vector2)Camera.main.transform.position).magnitude; // ���㴥�������������ľ���ȣ����ڿ��߱ȵ�������ѡ��
	}

	void Update()
	{
		Vector2 PinchDistance;
		Vector2 positionDistance;
		Vector3 newV3Distance;
		float imageScale;
		HandleZoom();
		//if (Input.GetMouseButton(0))
		//{
		//	tableBackImage.transform.position = Input.mousePosition;
		//}
		//if (Input.touchCount == 1)
		//{
		//	tableBackImage.transform.position = Input.GetTouch(0).position;
		//}

		if (Input.touchCount > 0)
		{
			Touch touch = Input.GetTouch(0);
			if (touch.phase == TouchPhase.Began)
			{
				Ray ray = Camera.main.ScreenPointToRay(touch.position);
				RaycastHit hit;
				if (Physics.Raycast(ray, out hit))
				{
					Debug.Log("Touched object: " + hit.collider.gameObject.name);
					localScale.text = "Touched object: " + hit.collider.gameObject.name;
				}
			}
		}
		

		if (Input.touchCount == 0)
		{
			if (startPinchDistance != Vector2.zero)
			{
				if (rectTransform.localScale.x < initialScale)
				{
					Vector3 localScale = Vector3.one * initialScale;
					rectTransform.localScale = localScale * initialScale;
				}
				else if (rectTransform.localScale.x > initialScale * maxScale)
				{
					Vector3 localScale = Vector3.one * maxScale;
					rectTransform.localScale = localScale * initialScale;
				}
				startPinchDistance = Vector2.zero;
			}
			if (startTouchPosition != Vector2.zero)
			{
				startTouchPosition = Vector2.zero;
			}
		}
		// ����ָ���Ž��д��������ű���
		if (Input.touchCount == 2)
		{
			if (startPinchDistance == Vector2.zero)
			{
				startPinchDistance = GetPinchDistance(Input.GetTouch(0).position, Input.GetTouch(1).position);
			}
			else
			{
				PinchDistance = GetPinchDistance(Input.GetTouch(0).position, Input.GetTouch(1).position);
				imageScale = PinchDistance.magnitude / startPinchDistance.magnitude;
				//localScale.text = $"localScale = {rectTransform.localScale.x}; minScale={initialScale * minScale}; maxScale = {initialScale * maxScale}";
				if (rectTransform.localScale.x >= initialScale * minScale && rectTransform.localScale.x <= initialScale * maxScale)
				{
					rectTransform.localScale = rectTransform.localScale * imageScale;
				}

			}
			//PointerEventData PointerEventData = new PointerEventData(EventSystem.current);

		}
		
		if (Input.touchCount == 1 && !glineChart.activeSelf)
		{
			if (startTouchPosition == Vector2.zero)
			{
				startTouchPosition = Input.GetTouch(0).position;
                initPosition = rectTransform.localPosition;
			}
			else
			{
				positionDistance = Input.GetTouch(0).position - startTouchPosition;
				newV3Distance.x = positionDistance.x;
                newV3Distance.y = positionDistance.y;
                newV3Distance.z = 0;
				rectTransform.localPosition = initPosition + newV3Distance;
			}
		}
		
		
		void HandleZoom()
		{
			float scroll = Input.GetAxis("Mouse ScrollWheel");
			if (scroll != 0)
			{
				Vector3 mousePos = Input.mousePosition;
				Vector2 pivot = RectTransformUtility.WorldToScreenPoint(null, mousePos);
				ZoomAroundPoint(scroll, pivot);
			}
		}

		void ZoomAroundPoint(float scrollInput, Vector2 pivot)
		{
			float scaleFactor = 1f + scrollInput * zoomSpeed;
			Vector3 newScale = contentRect.localScale * scaleFactor;
			newScale = Vector3.Max(newScale, Vector3.one * minScale);
			newScale = Vector3.Min(newScale, Vector3.one * maxScale);

			// ��ȡ��ǰ��Ļ��Canvas���ݵ�λ��ƫ��
			Vector2 localPivot;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(contentRect, pivot, null, out localPivot);

			// ����ǰ��λ��
			Vector3 oldPos = contentRect.position;

			// Ӧ������
			contentRect.localScale = newScale;

			// ����λ�ã�ʹ����Χ�����λ��
			//Vector3 delta = (Vector3)(contentRect.TransformPoint(localPivot) - (Vector3)pivot);
			//contentRect.position = oldPos - delta;
		}
	}
	//�ر�����ͼ
	public void closeLinechart()
	{
		glineChart.SetActive(false);
		chartFlag = 0;
	}
}