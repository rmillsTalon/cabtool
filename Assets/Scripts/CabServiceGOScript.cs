using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CabTool;
using dtos = CabTool.Dtos;

public class CabServiceGOScript : MonoBehaviour
{
    static CabServiceGOScript Instance;

    private void Awake()
    {
        Debug.Log("CabServiceGOScript - Awake, instance: " + (Instance==null?"(null)":"(not null)"));
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);
    }

    public CabService Svc;

    void Start()
    {
        Debug.Log("CabServiceGOScript - Start");
        Instance.Svc = (CabService)GetComponent<CabService>();
        Svc.Events += ReceivedEvent;
    }

    private void ReceivedEvent(object sender, dtos.EventArgs e)
    {
        Debug.Log("CabServiceGOScript - ReceivedEvent, action: " + e.Action + ", item: " + e.Item.Label);

        var item = e.Item;

        int maxlen = 100;
        var value = item.Label + ": ";
        if (item.Value == null)
            value += "(null)";
        else if (item.Value.Length > maxlen)
            value += item.Value.Substring(0, maxlen - 3) + "...";
        else
            value += item.Value;

        Instance._Text += value + "\n";
        ++Instance._DataVer;
        Debug.Log("CabServiceGOScript - ReceivedEvent, leaving, ver: " + Instance._DataVer + ", text: " + Instance._Text);
    }

    private string _Text;
    private int _DataVer = 1;
    public string Text { get { return Instance._Text; } }
    public int DataVer { get { return Instance._DataVer; } }
}
