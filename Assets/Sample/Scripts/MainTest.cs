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
        Sam.TestBB();
    }
    
    
}