using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace AnalysisTools.Editor
{
    /// <summary>
    /// 性能分析工具的编辑器核心类
    /// 负责通过IL注入的方式向目标方法中插入性能统计代码
    /// 注入的函数不能在修改的程序集中，否则后续无法修改该dll
    /// </summary>
    public class HookEditor
    {
        private const string INJECT_PATHS = "inject_paths";
        private const string LOG_PREFIX = "[AnalysisTools] ";

        #region Menu Items

        [MenuItem("AnalysisTools/注入代码【ProfilerSample】")]
        private static void InjectProfilerSample()
        {
            ProcessInjection(typeof(ProfilerSampleAttribute));
        }

        [MenuItem("AnalysisTools/注入代码【All】")]
        private static void InjectAll()
        {
            ProcessInjection(typeof(AnalysisAllAttribute));
        }

        [MenuItem("AnalysisTools/导出CSV")]
        private static void ExportCSV()
        {
            HookUtils.ExportMethodAnalysisCSV();
        }

        #endregion
        
        #region Core Injection Logic

        public static void ProcessInjection(Type hookType, bool isBuild = false)
        {
            Debug.Log($"{LOG_PREFIX}开始注入处理 - Hook类型: {hookType.Name}");
            
            if (Application.isPlaying)
            {
                Debug.Log("正在运行或编译中，无法进行注入处理");
                return;
            }
            
            EditorApplication.LockReloadAssemblies();

            try
            {
                using var assemblyResolver = CreateAssemblyResolver();
                var (readerParams, writerParams) = CreateParameters(assemblyResolver);
                var paths = LoadInjectionPaths(isBuild);

                ProcessAssemblies(paths, readerParams, writerParams, hookType);
            }
            catch (Exception e)
            {
                Debug.LogError($"{LOG_PREFIX}注入过程发生错误:\n{e}");
            }
            finally
            {
                EditorApplication.UnlockReloadAssemblies();
                Debug.Log($"{LOG_PREFIX}注入处理完成");
            }
        }

        private static DefaultAssemblyResolver CreateAssemblyResolver()
        {
            var resolver = new DefaultAssemblyResolver();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!string.IsNullOrEmpty(assembly.Location))
                {
                    resolver.AddSearchDirectory(Path.GetDirectoryName(assembly.Location));
                }
            }

            string unityManagedPath = null;

            if (Environment.OSVersion.Platform == PlatformID.MacOSX || Environment.OSVersion.Platform == PlatformID.Unix)
            {
                unityManagedPath = Path.GetDirectoryName(EditorApplication.applicationPath) + "/Unity.app/Contents/Managed";
            }
            else if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                unityManagedPath = Path.GetDirectoryName(EditorApplication.applicationPath) + "/Data/Managed";
            }
            else
            {
                Debug.LogError("当前平台不支持");
            }

            if (!string.IsNullOrEmpty(unityManagedPath))
            {
                resolver.AddSearchDirectory(unityManagedPath);
            }

            return resolver;
        }
        
        private static (ReaderParameters, WriterParameters) CreateParameters(IAssemblyResolver resolver)
        {
            return (
                new ReaderParameters
                {
                    ReadSymbols = false,
                    ReadWrite = true,
                    AssemblyResolver = resolver,
                },
                new WriterParameters
                {
                    WriteSymbols = false,
                }
            );
        }

        private static List<string> LoadInjectionPaths(bool isBuild)
        {
            var pathsAsset = Resources.Load<TextAsset>(INJECT_PATHS);
            if (pathsAsset == null)
            {
                throw new FileNotFoundException($"{LOG_PREFIX}未找到注入路径配置文件: {INJECT_PATHS}");
            }

            var pathsData = JsonUtility.FromJson<PathsData>(pathsAsset.text);
            return isBuild ? pathsData.build_paths ?? new List<string>() : pathsData.paths ?? new List<string>();
        }

        private static void ProcessAssemblies(
            List<string> paths,
            ReaderParameters readerParams,
            WriterParameters writerParams,
            Type hookType)
        {
            foreach (var path in paths)
            {
                var assemblyPath = Application.dataPath + path;
                ProcessSingleAssembly(assemblyPath, readerParams, writerParams, hookType);
            }
        }

        private static void ProcessSingleAssembly(
            string assemblyPath,
            ReaderParameters readerParams,
            WriterParameters writerParams,
            Type hookType)
        {
            Debug.Log($"{LOG_PREFIX}处理程序集: {Path.GetFileName(assemblyPath)}");

            try
            {
                var assembly = AssemblyDefinition.ReadAssembly(assemblyPath, readerParams);
                if (ProcessAssembly(assembly, hookType))
                {
                    assembly.Write(writerParams);
                    assembly.Dispose();
                    Debug.Log($"{LOG_PREFIX}程序集处理成功: {Path.GetFileName(assemblyPath)}");
                }
                else
                {
                    Debug.Log($"{LOG_PREFIX}程序集中未找到需要注入的方法: {Path.GetFileName(assemblyPath)}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"{LOG_PREFIX}处理程序集失败 {Path.GetFileName(assemblyPath)}:\n{e}");
            }
        }

        #endregion

        #region Assembly Processing

        private static bool ProcessAssembly(AssemblyDefinition assembly, Type hookType)
        {
            bool wasProcessed = false;

            foreach (var module in assembly.Modules)
            {
                foreach (var type in module.Types)
                {
                    if (ShouldSkipType(type)) continue;
                    wasProcessed |= ProcessType(type, module, hookType);
                }
            }

            return wasProcessed;
        }

        private static bool ShouldSkipType(TypeDefinition type)
        {
            return type.Name == nameof(HookUtils) ||
                   type.Name == nameof(HookEditor) ||
                   type.IsAbstract ||
                   type.IsInterface;
        }

        private static bool ProcessType(TypeDefinition type, ModuleDefinition module, Type hookType)
        {
            bool wasProcessed = false;

            foreach (var method in type.Methods)
            {
                if (ShouldSkipMethod(method)) continue;
                if (!ShouldProcessMethod(method, hookType)) continue;

                InjectCode(method, module, type, hookType);
                wasProcessed = true;
            }

            return wasProcessed;
        }

        private static bool ShouldSkipMethod(MethodDefinition method)
        {
            return method.Name == ".ctor" ||
                   method.Name == ".cctor" ||
                   method.IsAbstract ||
                   method.IsVirtual ||
                   method.IsGetter ||
                   method.IsSetter;
        }

        private static bool ShouldProcessMethod(MethodDefinition method, Type hookType)
        {
            return hookType == typeof(AnalysisAllAttribute) ||
                   method.CustomAttributes.Any(attr => attr.AttributeType.FullName == hookType.FullName);
        }

        private static void InjectCode(
            MethodDefinition method,
            ModuleDefinition module,
            TypeDefinition type,
            Type hookType)
        {
            var beginMethod = module.ImportReference(
                typeof(HookUtils).GetMethod("Begin", new[] { typeof(string), typeof(string) }));
            var endMethod = module.ImportReference(
                typeof(HookUtils).GetMethod("End", new[] { typeof(string), typeof(string) }));

            var processor = method.Body.GetILProcessor();
            var methodId = $"[{type.FullName}.{method.Name}]-[{GetParamsStr(method)}]";

            InjectStartCode(processor, method, methodId, hookType, beginMethod);
            InjectEndCode(processor, method, methodId, hookType, endMethod);

            if (method.Body.ExceptionHandlers.Count > 0)
            {
                ProcessExceptionHandlers(processor, method, methodId, hookType, endMethod);
            }

            ComputeOffsets(method.Body);
        }

        private static void InjectStartCode(
            ILProcessor processor,
            MethodDefinition method,
            string methodId,
            Type hookType,
            MethodReference beginMethod)
        {
            var first = method.Body.Instructions[0];
            processor.InsertBefore(first, Instruction.Create(OpCodes.Ldstr, methodId));
            processor.InsertBefore(first, Instruction.Create(OpCodes.Ldstr, hookType.FullName));
            processor.InsertBefore(first, Instruction.Create(OpCodes.Call, beginMethod));
        }

        private static void InjectEndCode(
            ILProcessor processor,
            MethodDefinition method,
            string methodId,
            Type hookType,
            MethodReference endMethod)
        {
            var instructions = method.Body.Instructions.ToList();
            foreach (var instruction in instructions)
            {
                if (instruction.OpCode == OpCodes.Ret)
                {
                    var endBlock = Instruction.Create(OpCodes.Ldstr, methodId);
                    processor.InsertBefore(instruction, endBlock);
                    processor.InsertBefore(instruction, Instruction.Create(OpCodes.Ldstr, hookType.FullName));
                    processor.InsertBefore(instruction, Instruction.Create(OpCodes.Call, endMethod));
                }
            }
        }

        private static void ProcessExceptionHandlers(
            ILProcessor processor,
            MethodDefinition method,
            string methodId,
            Type hookType,
            MethodReference endMethod)
        {
            foreach (var handler in method.Body.ExceptionHandlers)
            {
                if (handler.HandlerType == ExceptionHandlerType.Finally)
                {
                    var leaveInstruction = handler.HandlerEnd.Previous;
                    processor.InsertBefore(leaveInstruction, Instruction.Create(OpCodes.Ldstr, methodId));
                    processor.InsertBefore(leaveInstruction, Instruction.Create(OpCodes.Ldstr, hookType.FullName));
                    processor.InsertBefore(leaveInstruction, Instruction.Create(OpCodes.Call, endMethod));
                }
            }
        }

        private static void ComputeOffsets(MethodBody body)
        {
            int offset = 0;
            foreach (var instruction in body.Instructions)
            {
                instruction.Offset = offset;
                offset += instruction.GetSize();
            }
        }

        private static string GetParamsStr(MethodDefinition method)
        {
            if (method.Parameters == null || method.Parameters.Count == 0)
            {
                return "Null";
            }

            return string.Join(",", method.Parameters.Select(p => p.ParameterType.FullName));
        }

        #endregion
    }
}
