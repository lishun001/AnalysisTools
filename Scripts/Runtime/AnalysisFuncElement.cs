namespace AnalysisTools
{
    public class AnalysisFuncElement
    {
        public string FuncName;

        /// <summary>
        /// 函数被调用次数
        /// </summary>
        public int FuncCalls;

        /// <summary>
        /// 函数累计占用内存
        /// </summary>
        public long FuncTotalMemory;

        /// <summary>
        /// 函数累计执行时间
        /// </summary>
        public float FuncTotalTime;

        /// <summary>
        /// 记录开始时的内存
        /// </summary>
        public long BeginMemory;

        /// <summary>
        /// 记录开始时的时间
        /// </summary>
        public float BeginTime;
    }
}