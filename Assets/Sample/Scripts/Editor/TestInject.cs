using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

public class TestInject
{
    // private static bool hasGen = false;
    // [PostProcessBuild(1000)]
    // private static void OnPostprocessBuildPlayer(BuildTarget buildTarget, string buildPath)
    // {
    //     hasGen = false;
    // }
    //
    // [PostProcessScene]
    // public static void TestInjectMothodOnPost()
    // {
    //     if (hasGen == true) return;
    //     hasGen = true;
    //
    //     TestInjectMothod();
    // }
    [MenuItem("Tools/InjectMethod")]
    public static void TestInjectMothod()
    {
        var assembly = AssemblyDefinition.ReadAssembly(@"/Users/a1104/Documents/guru/AnalysisTools/Library/ScriptAssemblies/Assembly-CSharp.dll");
        var types = assembly.MainModule.GetTypes();
        foreach(var type in types)
        {
            foreach(var Method in type.Methods)
            {
                if(Method.Name == "TestAA")
                {
                    InjectMethod(Method, assembly);
                }
            }
        }
        var writerParameters = new WriterParameters { WriteSymbols = true };
        assembly.Write(@"/Users/a1104/Documents/guru/AnalysisTools/Library/ScriptAssemblies/Assembly-CSharp.dll", writerParameters);
    }
    
    
    private static void InjectMethod(MethodDefinition method, AssemblyDefinition assembly)
    {
        var firstIns = method.Body.Instructions.First();
        var worker = method.Body.GetILProcessor();

        //获取Debug.Log方法引用
        var hasPatchRef = assembly.MainModule.Import(
            typeof(Debug).GetMethod("Log", new Type[] { typeof(string) }));
        //插入函数
        var current = InsertBefore(worker, firstIns, worker.Create(OpCodes.Ldstr, "Inject"));
        current = InsertBefore(worker, firstIns, worker.Create(OpCodes.Call, hasPatchRef));
        //计算Offset
        ComputeOffsets(method.Body);
    }
    
    /// <summary>
    /// 语句前插入Instruction, 并返回当前语句
    /// </summary>
    private static Instruction InsertBefore(ILProcessor worker, Instruction target, Instruction instruction)
    {
        worker.InsertBefore(target, instruction);
        return instruction;
    }

    /// <summary>
    /// 语句后插入Instruction, 并返回当前语句
    /// </summary>
    private static Instruction InsertAfter(ILProcessor worker, Instruction target, Instruction instruction)
    {
        worker.InsertAfter(target, instruction);
        return instruction;
    }
//计算注入后的函数偏移值
    private static void ComputeOffsets(MethodBody body)
    {
        var offset = 0;
        foreach (var instruction in body.Instructions)
        {
            instruction.Offset = offset;
            offset += instruction.GetSize();
        }
    }
}