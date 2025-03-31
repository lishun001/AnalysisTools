using System;
using System.Collections.Generic;
using AnalysisTools;
using UnityEngine;
using UnityEngine.UI;

public class MainTest : MonoBehaviour
{
    public Button btnReport;
    
    private void Awake()
    {
        btnReport.onClick.AddListener(HookUtils.ExportMethodAnalysisCSV);
    }

    [ProfilerSample]
    public void Ruguo()
    {
        byte[] arr = new Byte[1024];
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            Ruguo();
        }
    }
}