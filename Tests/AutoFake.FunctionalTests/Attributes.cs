namespace AutoFake.FunctionalTests;

internal class ExcludedFact : Xunit.FactAttribute
{
	public ExcludedFact()
	{
		Skip = "Excluded untill v1 is ready";
	}
}

internal class ExcludedTheory : Xunit.TheoryAttribute
{
	public ExcludedTheory()
	{
		Skip = "Excluded untill v1 is ready";
	}
}
