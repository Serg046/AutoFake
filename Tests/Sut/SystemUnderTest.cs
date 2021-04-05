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
    }
}
