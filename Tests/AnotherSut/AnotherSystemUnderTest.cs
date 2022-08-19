using System;

namespace AnotherSut
{
	public class AnotherSystemUnderTest
	{
		public DateTime GetCurrentDate() => DateTime.Now;

		public virtual DateTime GetCurrentDateVirtual() => DateTime.Now;
	}
}
