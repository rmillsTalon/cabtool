using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using CabTool;
using dtos = CabTool.Dtos;

public class IoItemScript : MonoBehaviour
{

    public dtos.IoItem Item { get; private set; }
    public CabService Svc { get; set; }

    private InputField Fld;
    private UnityAction OnUpdate;
    private string Container;

    public void Setup(dtos.IoItem item, CabService svc, GameObject ctrl, string container)
    {
        Svc = svc;
        Item = item;
        Container = container;

        Refresh();

        var btn = ctrl.transform.Find("Update").GetComponent<Button>();
        btn.onClick.AddListener(UpdateValue);

        btn = ctrl.transform.Find("Refresh").GetComponent<Button>();
        btn.onClick.AddListener(RefreshValue);
    }

    void Refresh()
    {
        if (Item == null)
            return;

        //        var ctrl = transform.Find("ContainerLabel");
        //        var text = ctrl.GetComponent<Text>();
        //        text.text = Item.Container ?? "";

        var ctrl = transform.Find("Label");
        var text = ctrl.GetComponent<Text>();
        text.text = Item.Label ?? "";

        ctrl = transform.Find("Value");
        Fld = ctrl.GetComponent<InputField>();
        Fld.text = Item.Value ?? "(null)";
    }

    public async void UpdateValue()
    {
        if (Item == null || Svc == null)
            return;

        Debug.Log("You have clicked the update button, item id: " + Item.Id + ", name: " + Item.Name);
        await Svc.PutIoValue(Item.Id, Fld.text);
    }
    public void RefreshValue()
    {
        Debug.Log("You have clicked the refresh button!");
    }

}
