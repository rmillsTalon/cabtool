using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using dtos = CabTool.Dtos;
using CabTool;
using System;

public class EventPanelScript : MonoBehaviour
{
//    public CabService Svc;
    public CabServiceGOScript Svc;

/*
    static EventPanelScript Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);
    }
*/

    // Start is called before the first frame update
    void Start()
    {
        if ( Svc == null)
            Svc = GetComponent<CabServiceGOScript>();
//        DontDestroyOnLoad(this.gameObject);
        Refresh();
        Dt = DateTimeOffset.Now.AddSeconds(10);
    }

    void Refresh()
    {
        Debug.Log("EventPanelScript - Refresh, svc: " + (Svc!=null?"(not null)":"(null)") + ", svc ver: " + (Svc?.DataVer.ToString()??"") + ", my ver: " + DataVer);
        Debug.Log("EventPanelScript - Refresh, svc.svc: " + (Svc.Svc != null ? "(not null)" : "(null)"));
//        Debug.Log("EventPanelScript - refresh, svc counter: " + Svc.Svc.Counter);

        var go = transform.Find("Scroll View/Viewport/Content/Text");
        EventText = go.GetComponent<Text>();
        Debug.Log("EventPanelScript - Refresh, EventText: " + (EventText==null?"(null)":"(not null)"));

        Text = Svc.Text ?? "";
//        Svc.Svc.Events += ReceivedEvent;
    }
/*
    private void ReceivedEvent(object sender, dtos.EventArgs e)
    {
        Debug.Log("EventPanelScript - ReceivedEvent, action: " + e.Action + ", item: " + e.Item.Label);

        var item = e.Item;

        int maxlen = 100;
        var value = item.Label + ": ";
        if (item.Value == null)
            value += "(null)";
        else if (item.Value.Length > maxlen)
            value += item.Value.Substring(0, maxlen - 3) + "...";
        else
            value += item.Value;

        Text += value;
    }
*/

    void Update()
    {
        // Use while to guard against race conditions. Could a flood of events cause this to never return ...nah
        if (DataVer < (Svc?.DataVer??0) && EventText != null)
        {
            Debug.Log("EventPanelScript - Update, dataVer: " + DataVer + ", Svc.DataVer: " + Svc.DataVer + ", text: " + Svc.Text);
            EventText.text = Svc.Text;
            DataVer = Svc.DataVer;
        }
        else if ( DateTimeOffset.Now > Dt)
        {
            Debug.Log("EventPanelScript - Update Timeout, dataVer: " + DataVer
                        + ", Svc.DataVer: " + (Svc?.DataVer.ToString() ?? "(null)")
                        + ", cond met: " + ((Svc != null && DataVer < Svc.DataVer && EventText != null) ? "true" : "false")
                        + ", cond met1 rev: " + ((Svc == null) ? "true" : "false")
                        + ", cond met2: " + ((DataVer < Svc.DataVer) ? "true" : "false")
                        + ", cond met3: " + ((EventText != null) ? "true" : "false")
                        + ", EventText: " + (EventText == null ? "(null)" : "(not null)")
                        + ", text: " + Svc?.Text??"(null)");
            Dt = DateTimeOffset.Now.AddSeconds(10);
        }
    }

    private Text EventText;
    private string Text;
    private int DataVer = 0;
    private DateTimeOffset Dt;
}
