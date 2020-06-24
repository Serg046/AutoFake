namespace AutoFake
{
    internal interface IFakeArgument
    {
        IFakeArgumentChecker Checker { get; }
        bool Check(object argument);
    }
}