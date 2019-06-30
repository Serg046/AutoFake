namespace AutoFake
{
    internal interface IMocker : IMethodMocker
    {
        void GenerateSetupBodyField();
        void GenerateRetValueField();
        void GenerateCallbackField();
        void GenerateCallsCounterFuncField();
    }
}