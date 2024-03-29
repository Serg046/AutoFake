using System;
using AnotherSut;

namespace Sut
{
	public class SystemUnderTest
	{
		public void SimpleMethod()
		{
		}

		internal void InternalMethod()
		{
		}

		public DateTime GetCurrentDate() => DateTime.Now;

		public DateTime GetCurrentDateFromAnotherSut() => new AnotherSystemUnderTest().GetCurrentDate();

		public TimeSpan GetDateVirtual() => GetDate(new DerivedAnotherSystemUnderTest()) - GetDate(new AnotherSystemUnderTest());

		private DateTime GetDate(AnotherSystemUnderTest x) => x.GetCurrentDateVirtual();

		private class DerivedAnotherSystemUnderTest : AnotherSystemUnderTest
		{
			public override DateTime GetCurrentDateVirtual() => DateTime.UtcNow;
		}
	}
}
