namespace WhiteJackalStudio.TestTools.Tests.Dummies;

public interface ISampleDependency
{
    string GetValue();
}

public interface IAnotherDependency
{
    int Compute(int x);
}

public class SampleService
{
    private readonly ISampleDependency _dependency;
    private readonly IAnotherDependency _anotherDependency;
    private readonly string _privateField = "initial";
    private int PrivateProperty { get; set; } = 42;

    public SampleService(ISampleDependency dependency, IAnotherDependency anotherDependency)
    {
        _dependency = dependency;
        _anotherDependency = anotherDependency;
    }

    public string GetDependencyValue() => _dependency.GetValue();

    public int ComputeVia(int x) => _anotherDependency.Compute(x);

    private string PrivateMethod() => $"secret:{_privateField}";

    private string PrivateMethodWithArgs(string prefix, int number) => $"{prefix}-{number}";

    private void ThrowingMethod() => throw new InvalidOperationException("boom");

    private void ThrowingMethodWithArgs(string message) => throw new ArgumentException(message);
}
