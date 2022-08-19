using AutoFake.Abstractions.Setup;

namespace AutoFake.Abstractions.Expression
{
	internal interface IGetSourceMemberVisitor : IMemberVisitor
	{
		ISourceMember SourceMember { get; }
	}
}
