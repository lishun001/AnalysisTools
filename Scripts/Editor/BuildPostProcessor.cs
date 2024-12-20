// using UnityEditor;
// using UnityEditor.Android;
// using UnityEditor.Build;
// using UnityEditor.Build.Reporting;
// using UnityEditor.UnityLinker;
// using UnityEngine;
//
// namespace AnalysisTools.Editor
// {
//     public class BuildPostProcessor : IPostGenerateGradleAndroidProject, IPostBuildPlayerScriptDLLs, IPostprocessBuildWithReport, IPreprocessBuildWithReport
//     {
//         public int callbackOrder { get; }
//         public void OnPreprocessBuild(BuildReport report)
//         {
//             HookEditor.ProcessInjection(typeof(AnalysisAllAttribute));
//         }
//
//         public void OnPostprocessBuild(BuildReport report)
//         {
//             HookEditor.ProcessInjection(typeof(AnalysisAllAttribute));
//         }
//
//         public void OnPostBuildPlayerScriptDLLs(BuildReport report)
//         {
//             HookEditor.ProcessInjection(typeof(AnalysisAllAttribute));
//         }
//
//         public void OnPostGenerateGradleAndroidProject(string path)
//         {
//             HookEditor.ProcessInjection(typeof(AnalysisAllAttribute));
//         }
//     }
// }