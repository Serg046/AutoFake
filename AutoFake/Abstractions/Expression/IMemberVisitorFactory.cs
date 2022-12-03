using System;

namespace AutoFake.Abstractions.Expression
{
	internal interface IMemberVisitorFactory
	{
		T GetMemberVisitor<T>();
		IGetValueMemberVisitor GetValueMemberVisitor(object? instance);
		ITargetMemberVisitor<T> GetTargetMemberVisitor<T>(IExecutableMemberVisitor<T> requestedVisitor, Type targetType);
	}
}
