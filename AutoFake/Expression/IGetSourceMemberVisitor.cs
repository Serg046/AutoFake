using AutoFake.Setup;

namespace AutoFake.Expression
{
	internal interface IGetSourceMemberVisitor : IMemberVisitor
	{
		ISourceMember SourceMember { get; }
	}
}