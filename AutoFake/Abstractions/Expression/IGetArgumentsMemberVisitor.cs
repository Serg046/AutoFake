using AutoFake.Abstractions;
using System.Collections.Generic;

namespace AutoFake.Abstractions.Expression;

internal interface IGetArgumentsMemberVisitor : IMemberVisitor<IReadOnlyList<IFakeArgument>>
{
}
