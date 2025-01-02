using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace AnalysisTools.Editor
{
    public class BuildPostProcessor: IPostBuildPlayerScriptDLLs
    {
        public void OnPostBuildPlayerScriptDLLs(BuildReport report)
        {
#if INJECT_FUNC
            HookEditor.ProcessInjection(typeof(AnalysisAllAttribute), true);
#endif
            
#if INJECT_SAMPLE
            HookEditor.ProcessInjection(typeof(ProfilerSampleAttribute), true);
#endif
            
        }

        public int callbackOrder { get; }
    }
}