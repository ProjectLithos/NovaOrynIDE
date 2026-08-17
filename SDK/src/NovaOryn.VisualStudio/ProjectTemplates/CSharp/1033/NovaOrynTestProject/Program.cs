using System;

namespace $safeprojectname$;

public static class Program
{
    public static Int32 Main(String[] args)
    {
        Boolean passed = RunTest();
        return passed ? 0 : 1;
    }

    public static Boolean RunTest() => true;
}
