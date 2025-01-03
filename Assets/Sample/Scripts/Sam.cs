using AnalysisTools;
using UnityEngine;

public class Sam
{
    public enum State
    {
        State1,
        State2,
        State3,
        State4,
    }
    
    [ProfilerSample]
    public static void TestBB(State state)
    {
        if (state == State.State1)
        {
            Debug.LogError("State1");
        }
        else if (state == State.State2)
        {
            Debug.LogError("State2");
        }
        else if (state == State.State3)
        {
            Debug.LogError("State3");
        }
        else if (state == State.State4)
        {
            Debug.LogError("State4");
        }
        else
        {
            Debug.LogError("Test...");
        }
    }
    
}