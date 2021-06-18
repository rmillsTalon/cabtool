using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using dtos = CabTool.Dtos;
using CabTool;

public class IoItemsScript : MonoBehaviour
{
	//    [Inject] private XService Svc;

	public GameObject IoItemPrefab;
	public CabService Svc;

	// Start is called before the first frame update
	void Start()
	{
		//        GameObject Cupboard = GameObject.Find("Cupboard");
		Svc = (CabService) GetComponent<CabService>();
		StartCoroutine(waiter());
	}

	// Update is called once per frame
	void Update()
	{

	}

	IEnumerator waiter()
	{
//		Svc.OnChangeValue += ChangeValue;
//		while (!Svc.IsReady())
//		{
//			yield return new WaitForSeconds(1);
//		}
//		Debug.Log("IoItemsScript, waiter - Ready");

		Refresh();

		yield return new WaitForSeconds(1);
		/*
				var ios = cabinetService.ios;
				if (ios != null && ios.Count > 0)
				{

					var selected = cabinetService.GetIosSliders();
					numSelectors = selected.Count;

					if (numSelectors > 0)
					{
						selectorArr = new GameObject[numSelectors];
						sliderArr = new Slider[numSelectors];
						for (int i = 0; i < numSelectors; i++)
						{
							var sensor = selected[i];


							GameObject go = Instantiate(selector, new Vector3((float)100.0f, (float)((float)Screen.height - 50f - i * 30f ), 0f), Quaternion.identity) as GameObject;
							var name = go.transform.Find("Name").gameObject;
							name.GetComponent<Text>().text = sensor.Name;

							var valueObject = go.transform.Find("Value").gameObject;
							valueObject.GetComponent<SliderValueToText>().id = sensor.Id;

							var SliderObject = go.transform.Find("SliderObject").gameObject;
							var slider = SliderObject.GetComponent<Slider>();
							slider.value = string.IsNullOrWhiteSpace(sensor.Value)?0f: (float)System.Convert.ToInt32(sensor.Value);
							slider.maxValue= string.IsNullOrWhiteSpace(sensor.Range) ? 1f : (float)System.Convert.ToInt32(sensor.Range);

							if (slider.maxValue == 1|| slider.maxValue == 2)
							{
								var rectTransform= SliderObject.GetComponent<RectTransform>();
								float sliderSize = 50;
								rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 100, sliderSize* slider.maxValue);

							}
							//go.transform.parent = GameObject.Find("Canvas").transform;
							go.transform.SetParent(GameObject.Find("Canvas").transform);
							//go.transform.localScale = Vector3.one * 45;
							//go.transform.localRotation = Quaternion.identity;
							selectorArr[i] = go;
							sliderArr[i] = slider;
						}
					}
				}
		*/
	}

	public async void Refresh()
	{
		var containers = await Svc.GetContainers();
		if (containers == null || !containers.Any())
			return;
		if (IoItemPrefab == null)
			throw new MissingReferenceException("Cannot refresh IO Items control; the Io Item Prefab control is not provided.");

		var parent = transform.Find("Scroll View/Viewport/Content");

		foreach (var container in containers)
		{
			var items = container.IoItems;
//			var items = Svc.ios.Where(x => x.ContainerId == container.Id);
			foreach (var item in items)
			{
				var ioitem = Instantiate(IoItemPrefab);
				var ioitemObj = ioitem.GetComponent<IoItemScript>();
				var data = new dtos.IoItem()
				{
					Id = item.Id,
					Label = item.Label,
					Name = item.Name,
					Value = item.Value
				};
				ioitemObj.Setup(data, Svc, ioitem, container.Label);
				ioitem.transform.SetParent(parent.transform, false);
			}
		}
	}


	public void ChangeValue(IoItem sensor)
	{
		/*       
				bool found = false;
				var selected = cabinetService.GetIosSliders();
				int numSelectors = selected.Count;
				int i;
				for (  i = 0; i < numSelectors; i++)
				{
					var s = selected[i];
					if (sensor.Id == s.Id)
					{
						found = true;
						break;
					}
				}
				if (!found) return;


				var slider = sliderArr[i];
				slider.value = string.IsNullOrWhiteSpace(sensor.Value) ? 0f : (float)System.Convert.ToInt32(sensor.Value);
		 */
	}


} // ContainerPanelScript
