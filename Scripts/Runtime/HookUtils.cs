using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Profiling;

namespace AnalysisTools
{
    public class HookUtils
    {
        private static Thread _mainThread = Thread.CurrentThread;

        private static Dictionary<string, AnalysisFuncElement>
            _funcElements = new Dictionary<string, AnalysisFuncElement>();

        public static void Begin(string name, string hookType)
        {
            if (Thread.CurrentThread != _mainThread)
            {
                return;
            }

            Debug.LogError($"Begin....{name} {hookType}");
            switch (hookType)
            {
                case "AnalysisTools.ProfilerSampleAttribute":
                    Profiler.BeginSample(name);
                    break;
                case "AnalysisTools.AnalysisAllAttribute":
                    BeginAnalysisAll(name);
                    break;
            }
        }

        public static void End(string name, string hookType)
        {
            if (Thread.CurrentThread != _mainThread)
            {
                return;
            }

            Debug.LogError($"End....{name} {hookType}");

            switch (hookType)
            {
                case "AnalysisTools.ProfilerSampleAttribute":
                    Profiler.EndSample();
                    break;
                case "AnalysisTools.AnalysisAllAttribute":
                    EndAnalysisAll(name);
                    break;
            }
        }

        private static void BeginAnalysisAll(string methodName)
        {
            long curMemory = Profiler.GetTotalAllocatedMemoryLong();
            float curTime = Time.realtimeSinceStartup;
            if (_funcElements.ContainsKey(methodName))
            {
                var funcElement = _funcElements[methodName];
                funcElement.BeginMemory = curMemory;
                funcElement.BeginTime = curTime;
                _funcElements[methodName] = funcElement;
            }
            else
            {
                var funcElement = new AnalysisFuncElement();
                funcElement.FuncName = methodName;
                funcElement.FuncCalls = 0;
                funcElement.FuncTotalMemory = 0L;
                funcElement.FuncTotalTime = 0f;
                funcElement.BeginMemory = curMemory;
                funcElement.BeginTime = curTime;
                _funcElements.Add(methodName, funcElement);
            }
        }

        private static void EndAnalysisAll(string methodName)
        {
            long curMemory = Profiler.GetTotalAllocatedMemoryLong();
            float curTime = Time.realtimeSinceStartup;
            AnalysisFuncElement funcElement = _funcElements[methodName];

            if (curMemory - funcElement.BeginMemory >= 0)
            {
                funcElement.FuncTotalMemory += curMemory - funcElement.BeginMemory;
                funcElement.FuncTotalTime += curTime - funcElement.BeginTime;
                funcElement.FuncCalls += 1;
                funcElement.BeginMemory = 0L;
                funcElement.BeginTime = 0f;
                _funcElements[methodName] = funcElement;
            }
        }


        public static void ExportMethodAnalysisCSV()
        {
            if (_funcElements.Count <= 0)
            {
                Debug.Log("没有函数性能数据");
                return;
            }

            string fileCSVName = "";
            fileCSVName = System.DateTime.Now.ToString("[yyyy-MM-dd]-[HH-mm-ss]");
            fileCSVName += ".csv";
            fileCSVName = Path.Combine(Application.persistentDataPath, fileCSVName);

            string header = "函数名,平均内存/k,平均耗时/ms,调用次数";
            using (StreamWriter sw = new StreamWriter(fileCSVName))
            {
                sw.WriteLine(header);
                var ge = _funcElements.GetEnumerator();
                while (ge.MoveNext())
                {
                    var tmp = ge.Current.Value;
                    //过滤调用次数0的函数
                    if (tmp.FuncCalls <= 0) continue;
                    StringBuilder sb = new StringBuilder();
                    sb.AppendFormat("{0},", ReplaceComma(tmp.FuncName));
                    sb.AppendFormat("{0:f4},", tmp.FuncTotalMemory / (tmp.FuncCalls * 1024.0));
                    sb.AppendFormat("{0},", tmp.FuncTotalTime / tmp.FuncCalls * 1000);
                    sb.AppendFormat("{0}", tmp.FuncCalls);
                    sw.WriteLine(sb);
                }

                sw.Close();
                ge.Dispose();
            }

            Debug.Log($"函数性能报告{fileCSVName}文件输出完成");
        }
        
        // 把字符串中的逗号替换为分号
        public static string ReplaceComma(string str)
        {
            return str.Replace(",", ";");
        }
    }
}