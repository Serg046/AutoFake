using System;

namespace AutoFake.Abstractions.Expression;

internal interface IGetValueMemberVisitor : IExecutableMemberVisitor<(Type Type, object? Value)>
{
}
