﻿using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using CabSvr.Fingerprint.Dtos;
using System.IO;
using Newtonsoft.Json;

public class Peripheral : MonoBehaviour
{
    // Start is called before the first frame update
    public Button m_ButtonGenerate;
    public Dropdown m_DropDown;
    public Dropdown m_DropDownData;
    public Text m_Text;
    void Start()
    {
        m_ButtonGenerate.onClick.AddListener(TaskOnClick);

    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void TaskOnClick()
    {
        if (m_DropDown.options.Count == 0) return;
        if (m_DropDownData.options.Count == 0) return;
        //Output this to console when Button1 or Button3 is clicked
        Debug.Log("You have clicked the button, dropdown value=" + m_DropDown.value);
        //m_Text.text = "New Value : " + m_DropDown.options[m_DropDown.value].text;
        GameObject Cupboard = GameObject.Find("Cupboard");
        var cabinetService = Cupboard.GetComponent<CabinetService>();
        var devices = cabinetService.devices;
        var deviceItem = devices[m_DropDown.value];
        var type = cabinetService.GetDeviceType(m_DropDown.value);
        var data = m_DropDownData.options[m_DropDownData.value].text;
       
        m_Text.text = $"{data} is sent";


        var item = CreateIo(deviceItem.Id, deviceItem.Type);

        switch (type)
        {
            
            case "FingerprintDPUruNet":
                var ret = new FingerprintResult();
                ret.Code = (ResultCode)ResultCode.Success;
                ret.Quality = (CaptureQuality)CaptureQuality.Good;
                ret.Score = 60;
                ret.Data = new FingerprintData();

                int width = 200;
                int height = 200;
               
                string path = Application.dataPath + "/Image/sample" + m_DropDownData.value + ".png";
                if (!File.Exists(path)) return;
                byte[] bytes = File.ReadAllBytes(path);
                ret.RawImage = bytes;
                ret.Width = width;
                ret.Height = height;
                item.Value = JsonConvert.SerializeObject(ret);
                break;
            default:
                item.Value = data;
                break;

        }
       
        
        cabinetService.ChangeIo(item);
    }
    protected IoItem CreateIo(string deviceId,string type)
    {
        var item = new IoItem();
        item.Id = System.Guid.NewGuid().ToString();
        item.DeviceId = deviceId;
        item.Direction = "r";
        item.Type = type;
        item.ValueType = "string";
        return item;
    }
}