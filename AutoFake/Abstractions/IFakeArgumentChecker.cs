namespace AutoFake.Abstractions
{
	public interface IFakeArgumentChecker
	{
		bool Check(object argument);
		public delegate bool Comparer<T>(T? left, T? right);
		public delegate bool Comparer(object? left, object? right);
	}
}
