[![Build status](https://ci.appveyor.com/api/projects/status/j95lb948sw02nqqd/branch/master?svg=true)](https://ci.appveyor.com/project/Serg046/autofake/branch/master)
[![codecov](https://codecov.io/gh/Serg046/AutoFake/branch/master/graph/badge.svg)](https://codecov.io/gh/Serg046/AutoFake)
[![NuGet version](https://badge.fury.io/nu/AutoFake.svg)](https://badge.fury.io/nu/AutoFake)

```csharp
public class Calendar
{
    public static DateTime Yesterday => DateTime.Now.AddDays(-1);
}

[Fact]
public void Yesterday_SomeDay_ThePrevDay()
{
    var fake = new Fake<Calendar>();

    fake.Rewrite(f => Calendar.Yesterday)
        .Replace(() => DateTime.Now)
        .Return(() => new DateTime(2016, 8, 8));

    fake.Execute(calendar => Assert.Equal(new DateTime(2016, 8, 7), Calendar.Yesterday));
}
```
