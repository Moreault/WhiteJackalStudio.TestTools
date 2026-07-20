namespace WhiteJackalStudio.TestTools.Tests.Dummies;

public interface IRegisteredService
{
    string Name { get; }
}

public class ConcreteRegisteredService : IRegisteredService
{
    public string Name => "concrete";
}

public class ServiceConsumer
{
    private readonly IServiceProvider _serviceProvider;

    public ServiceConsumer(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public object? ResolveService(Type type) => _serviceProvider.GetService(type);
}
