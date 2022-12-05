using System;
using System.Linq;
using System.Linq.Expressions;
using LinqExpression = System.Linq.Expressions.Expression;

namespace AutoFake;

#pragma warning disable AF0001 // Public by design
public static class Indexer
#pragma warning restore AF0001
{
#pragma warning disable DI0002 // There is no way to invert control here as it is called from the client side
	public static Setter<TSut> Of<TSut>(params object[] indexes) => Of<TSut>("set_Item", indexes);
	public static Setter<TSut> Of<TSut>(string indexerName, params object[] indexes) => new(indexerName, indexes);
#pragma warning restore DI0002

#pragma warning disable AF0001 // Public by design
	public class Setter<TSut>
#pragma warning restore AF0001
	{
		private readonly string _indexerName;
		private readonly object[] _indexes;

		public Setter(string indexerName, object[] indexes)
		{
			_indexerName = indexerName;
			_indexes = indexes;
		}

		public Expression<Action<TSut>> Set<TIndexerType>(Expression<Func<TIndexerType>> value)
		{
			var type = typeof(TSut);
			var setter = type.GetMethod(_indexerName) ?? throw new MissingMemberException(type.FullName, _indexerName);
			var sut = LinqExpression.Parameter(type);
			var arguments = _indexes.Select(i => LinqExpression.Constant(i)).Concat(new[] { value.Body });
			return LinqExpression.Lambda<Action<TSut>>(LinqExpression.Call(sut, setter, arguments), sut);
		}
	}
}
