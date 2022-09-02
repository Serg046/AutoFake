using System;
using AutoFake.Expression;

namespace AutoFake.Abstractions.Expression
{
	internal interface IMemberVisitorFactory
	{
		T GetMemberVisitor<T>();
		GetValueMemberVisitor GetValueMemberVisitor(object? instance);
		TargetMemberVisitor<T> GetTargetMemberVisitor<T>(IExecutableMemberVisitor<T> requestedVisitor, Type targetType);
	}
}
