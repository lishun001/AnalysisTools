using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Text;

namespace AnalysisTools.Editor
{
    /// <summary>
    /// 性能分析工具的编辑器核心类
    /// 负责通过IL注入的方式向目标方法中插入性能统计代码
    /// 注入的函数不能在修改的程序集中，否则后续无法修改该dll
    /// </summary>
    public class HookEditor
    {
        [MenuItem("AnalysisTools/注入代码【ProfilerSample】")]
        private static void InjectProfilerSample()
        {
            AssemblyPostProcessorRun(typeof(ProfilerSampleAttribute));
        }

        [MenuItem("AnalysisTools/注入代码【All】")]
        private static void InjectAll()
        {
            AssemblyPostProcessorRun(typeof(AnalysisAllAttribute));
        }

        [MenuItem("AnalysisTools/导出CSV")]
        private static void ExportCSV()
        {
            HookUtils.ExportMethodAnalysisCSV();
        }

        private const string INJECT_PATHS = "inject_paths";
        
        private static void AssemblyPostProcessorRun(Type hookType)
        {
            try
            {
                Debug.Log("开始注入...");
                EditorApplication.LockReloadAssemblies();
                DefaultAssemblyResolver assemblyResolver = new DefaultAssemblyResolver();
                
                foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (string.IsNullOrEmpty(assembly.Location))
                    {
                        continue;
                    }
                    assemblyResolver.AddSearchDirectory(Path.GetDirectoryName(assembly.Location));
                }
                
                assemblyResolver.AddSearchDirectory(Path.GetDirectoryName(EditorApplication.applicationPath) + "/Data/Managed");

                ReaderParameters readerParameters = new ReaderParameters();
                readerParameters.ReadSymbols = true;
                readerParameters.ReadWrite = true;
                readerParameters.AssemblyResolver = assemblyResolver;

                WriterParameters writerParameters = new WriterParameters();
                writerParameters.WriteSymbols = true;
                
                PathsData pathsData = JsonUtility.FromJson<PathsData>(Resources.Load<TextAsset>(INJECT_PATHS).text);

                foreach (string path in pathsData.paths)
                {
                    string assemblyPath = Application.dataPath + path;
                    readerParameters.SymbolReaderProvider = new Mono.Cecil.Pdb.PdbReaderProvider();
                    writerParameters.SymbolWriterProvider = new Mono.Cecil.Pdb.PdbWriterProvider();

                    AssemblyDefinition assemblyDefinition =
                        AssemblyDefinition.ReadAssembly(assemblyPath, readerParameters);
                    Debug.Log($"开始处理: {assemblyPath}");
                    if (HookEditor.ProcessAssembly(assemblyDefinition, hookType))
                    {
                        Debug.Log($"开始写入: {assemblyPath}");
                        assemblyDefinition.Write(writerParameters);
                        Debug.Log("写入成功");
                    }
                    else
                    {
                        Debug.Log($"不存在需要注入的方法: {assemblyPath}");
                    }

                    assemblyDefinition.Dispose();
                }

                readerParameters.AssemblyResolver.Dispose();
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
            }

            EditorApplication.UnlockReloadAssemblies();
        }

        private static bool ProcessAssembly(AssemblyDefinition assemblyDefinition, Type hookType)
        {
            bool wasProcessed = false;

            foreach (ModuleDefinition moduleDefinition in assemblyDefinition.Modules)
            {
                foreach (TypeDefinition typeDefinition in moduleDefinition.Types)
                {
                    if (typeDefinition.Name == nameof(HookUtils)) continue;
                    if (typeDefinition.Name == nameof(HookEditor)) continue;
                    //过滤抽象类
                    if (typeDefinition.IsAbstract) continue;
                    //过滤抽象方法
                    if (typeDefinition.IsInterface) continue;
                    foreach (MethodDefinition methodDefinition in typeDefinition.Methods)
                    {
                        //过滤构造函数
                        if(methodDefinition.Name == ".ctor")continue;
                        if (methodDefinition.Name == ".cctor") continue;
                        //过滤抽象方法、虚函数、get set 方法
                        if (methodDefinition.IsAbstract) continue;
                        if (methodDefinition.IsVirtual) continue;
                        if (methodDefinition.IsGetter) continue;
                        if (methodDefinition.IsSetter) continue;
                        
                        // 获取方法的Attribute
                        if (hookType != typeof(AnalysisAllAttribute))
                        {
                            bool needInject = methodDefinition.CustomAttributes.Any(typeAttribute => typeAttribute.AttributeType.FullName == hookType.FullName);
                            if (!needInject)
                            {
                                continue;
                            }
                        }


                        //如果注入代码失败，可以打开下面的输出看看卡在了那个方法上。
                        //Debug.Log(methodDefinition.Name + "======= " + typeDefinition.Name + "======= " +typeDefinition.BaseType.GenericParameters +" ===== "+ moduleDefinition.Name);
                        MethodReference logMethodReference = moduleDefinition.ImportReference(
                            typeof(HookUtils).GetMethod("Begin", new Type[] {typeof(string), typeof(string)}));
                        MethodReference logMethodReference1 =
                            moduleDefinition.ImportReference(typeof(HookUtils).GetMethod("End",
                                new Type[] {typeof(string), typeof(string)}));

                        ILProcessor ilProcessor = methodDefinition.Body.GetILProcessor();

                        Instruction first = methodDefinition.Body.Instructions[0];
                        ilProcessor.InsertBefore(first,
                            Instruction.Create(OpCodes.Ldstr,
                                $"[{typeDefinition.FullName}.{methodDefinition.Name}]-[{GetParamsStr(methodDefinition)}]"));
                        ilProcessor.InsertBefore(first, Instruction.Create(OpCodes.Ldstr, hookType.FullName));
                        ilProcessor.InsertBefore(first, Instruction.Create(OpCodes.Call, logMethodReference));

                        //解决方法中直接 return 后无法统计的bug 
                        //https://lostechies.com/gabrielschenker/2009/11/26/writing-a-profiler-for-silverlight-applications-part-1/

                        Instruction last = methodDefinition.Body.Instructions[^1];
                        Instruction lastInstruction = Instruction.Create(OpCodes.Ldstr,
                            $"[{typeDefinition.FullName}.{methodDefinition.Name}]-[{GetParamsStr(methodDefinition)}]");
                        ilProcessor.InsertBefore(last, lastInstruction);
                        ilProcessor.InsertBefore(last, Instruction.Create(OpCodes.Ldstr, hookType.FullName));
                        ilProcessor.InsertBefore(last, Instruction.Create(OpCodes.Call, logMethodReference1));

                        ComputeOffsets(methodDefinition.Body);

                        var jumpInstructions = methodDefinition.Body.Instructions.Cast<Instruction>().Where(i => i.Operand == lastInstruction);
                        foreach (var jump in jumpInstructions)
                        {
                            jump.Operand = lastInstruction;
                        }

                        wasProcessed = true;
                    }
                }
            }

            return wasProcessed;
        }

        private static void ComputeOffsets(MethodBody body)
        {
            var offset = 0;
            foreach (var instruction in body.Instructions)
            {
                instruction.Offset = offset;
                offset += instruction.GetSize();
            }
        }

        private static string GetParamsStr(MethodDefinition methodDefinition)
        {
            if (methodDefinition.Parameters == null || methodDefinition.Parameters.Count == 0)
            {
                return "Null";
            }

            StringBuilder sb = new StringBuilder();
            foreach (ParameterDefinition parameter in methodDefinition.Parameters)
            {
                sb.Append(parameter.ParameterType.FullName);
            }

            return sb.ToString();
        }
    }
}