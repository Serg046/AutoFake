namespace AutoFake.Abstractions.Setup.Configurations;

public interface IPatchConfiguration
{
    IReplacePatchConfiguration<TReturn> Replace<TReturn>(Func<TReturn> patchMember);
    IReplacePatchConfiguration<TReturn> Replace<TInput, TReturn>(Func<TInput,TReturn> patchMember);
}