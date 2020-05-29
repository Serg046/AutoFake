[![Build status](https://ci.appveyor.com/api/projects/status/j95lb948sw02nqqd/branch/master?svg=true)](https://ci.appveyor.com/project/Serg046/autofake/branch/master)
[![codecov](https://codecov.io/gh/Serg046/AutoFake/branch/master/graph/badge.svg)](https://codecov.io/gh/Serg046/AutoFake)
[![NuGet version](https://badge.fury.io/nu/AutoFake.svg)](https://badge.fury.io/nu/AutoFake)

```csharp
public class Calendar
{
    public static DateTime Yesterday => DateTime.Now.AddDays(-1);
    internal Task<DateTime> AddSomeMinutesAsync(DateTime date)
        => Task.Run(() => date.AddMinutes(new Random().Next(1, 10)));
}

[Fact]
public void Yesterday_SomeDay_ThePrevDay()
{
    var fake = new Fake<Calendar>();

    fake.Rewrite(f => Calendar.Yesterday)
        .Replace(() => DateTime.Now)
        .Return(new DateTime(2016, 8, day: 8));

    fake.Execute(() => Assert.Equal(new DateTime(2016, 8, 7), Calendar.Yesterday));
}

[Fact]
public async Task AddSomeMinutes_SomeDay_MinutesAdded()
{
    var randomValue = 7;
    var fake = new Fake<Calendar>();

    fake.Rewrite(f => f.AddSomeMinutesAsync(Arg.IsAny<DateTime>()))
        .Replace((Random r) => r.Next(1, 10)) // Arg.Is<int>(i => i == 10) is also possible
        .CheckArguments() // r.Next(1, 11) fails with "Expected - 11, actual - 10"
        .ExpectedCalls(c => c > 0) // c => c > 1 fails with "Actual value - 1"
        .Return(randomValue);

    await fake.ExecuteAsync(async calendar => Assert.Equal(new DateTime(2016, 8, 8, 0, minute: randomValue, 0),
        await calendar.AddSomeMinutesAsync(new DateTime(2016, 8, 8, hour: 0, minute: 0, second: 0))));
}
```
