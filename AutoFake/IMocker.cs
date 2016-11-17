namespace AutoFake
{
    internal interface IMocker : IMethodMocker
    {
        void GenerateRetValueField();
        void GenerateCallsCounter();
        void GenerateCallbackField();
    }
}