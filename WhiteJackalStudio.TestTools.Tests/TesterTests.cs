namespace WhiteJackalStudio.TestTools.Tests;

[TestClass]
public class TesterTests : Tester<SampleService>
{
    //--- Instance creation ---

    [TestMethod]
    public void Instance_ShouldBeCreated()
    {
        Assert.IsNotNull(Instance);
    }

    [TestMethod]
    public void Instance_AccessedTwice_ShouldReturnSameReference()
    {
        var first = Instance;
        var second = Instance;

        Assert.AreSame(first, second);
    }

    //--- GetMock ---

    [TestMethod]
    public void GetMock_ShouldReturnMockForDependency()
    {
        var mock = GetMock<ISampleDependency>();

        Assert.IsNotNull(mock);
    }

    [TestMethod]
    public void GetMock_CalledTwice_ShouldReturnSameMock()
    {
        var first = GetMock<ISampleDependency>();
        var second = GetMock<ISampleDependency>();

        Assert.AreSame(first, second);
    }

    [TestMethod]
    public void GetMock_SetupAndInvoke_ShouldWork()
    {
        GetMock<ISampleDependency>().Setup(x => x.GetValue()).Returns("hello");

        var result = Instance.GetDependencyValue();

        Assert.AreEqual("hello", result);
    }

    //--- ConstructWith ---

    [TestMethod]
    public void ConstructWith_BeforeInstanceAccess_ShouldUseProvidedParameters()
    {
        var mockDep = new Mock<ISampleDependency>();
        mockDep.Setup(x => x.GetValue()).Returns("custom");
        var mockAnother = new Mock<IAnotherDependency>();

        ConstructWith(mockDep.Object, mockAnother.Object);

        Assert.AreEqual("custom", Instance.GetDependencyValue());
    }

    [TestMethod]
    public void ConstructWith_AfterInstanceAccess_ShouldThrow()
    {
        _ = Instance; // force creation

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            ConstructWith(new Mock<ISampleDependency>().Object, new Mock<IAnotherDependency>().Object));
    }

    [TestMethod]
    public void ConstructWith_Null_ShouldThrow()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => ConstructWith(null!));
    }

    [TestMethod]
    public void ConstructWith_WrongParameterTypes_ShouldThrow()
    {
        Assert.ThrowsExactly<ArgumentException>(() => ConstructWith("wrong", "types"));
    }

    //--- Reflection: Fields ---

    [TestMethod]
    public void GetFieldValue_ShouldReturnPrivateFieldValue()
    {
        var value = GetFieldValue<string>("_privateField");

        Assert.AreEqual("initial", value);
    }

    [TestMethod]
    public void SetFieldValue_ShouldUpdatePrivateField()
    {
        SetFieldValue("_privateField", "modified");

        var value = GetFieldValue<string>("_privateField");
        Assert.AreEqual("modified", value);
    }

    //--- Reflection: Properties ---

    [TestMethod]
    public void GetPropertyValue_ShouldReturnPrivatePropertyValue()
    {
        var value = GetPropertyValue<int>("PrivateProperty");

        Assert.AreEqual(42, value);
    }

    [TestMethod]
    public void SetPropertyValue_ShouldUpdatePrivateProperty()
    {
        SetPropertyValue("PrivateProperty", 99);

        var value = GetPropertyValue<int>("PrivateProperty");
        Assert.AreEqual(99, value);
    }

    //--- Reflection: Methods ---

    [TestMethod]
    public void InvokeMethod_Parameterless_ShouldReturnResult()
    {
        var result = InvokeMethod("PrivateMethod");

        Assert.AreEqual("secret:initial", result);
    }

    [TestMethod]
    public void InvokeMethod_WithArgs_ShouldReturnResult()
    {
        var result = InvokeMethod("PrivateMethodWithArgs", "test", 42);

        Assert.AreEqual("test-42", result);
    }

    [TestMethod]
    public void InvokeMethodAndIgnoreException_WhenMatchingException_ShouldReturnNull()
    {
        var result = InvokeMethodAndIgnoreException<InvalidOperationException>("ThrowingMethod");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void InvokeMethodAndIgnoreException_WhenNonMatchingException_ShouldRethrow()
    {
        Assert.ThrowsExactly<TargetInvocationException>(() =>
            InvokeMethodAndIgnoreException<InvalidOperationException>("ThrowingMethodWithArgs", "oops"));
    }

    //--- SetupOptions ---

    [TestMethod]
    public void SetupOptions_WithExplicitOptions_ShouldReturnThem()
    {
        var options = new SampleOptions { ConnectionString = "test-conn", RetryCount = 3 };

        var result = SetupOptions(options);

        Assert.AreSame(options, result);
    }

    //--- AddToServiceProvider ---

    [TestMethod]
    public void AddToServiceProvider_ByType_ShouldRegisterMockedService()
    {
        AddToServiceProvider<IRegisteredService>();

        var provider = GetMock<IServiceProvider>();
        var resolved = provider.Object.GetService(typeof(IRegisteredService));

        Assert.IsNotNull(resolved);
    }

    [TestMethod]
    public void AddToServiceProvider_WithInstance_ShouldRegisterThatInstance()
    {
        var concrete = new ConcreteRegisteredService();

        AddToServiceProvider<IRegisteredService>(concrete);

        var provider = GetMock<IServiceProvider>();
        var resolved = provider.Object.GetService(typeof(IRegisteredService));

        Assert.AreSame(concrete, resolved);
    }

    [TestMethod]
    public void AddToServiceProvider_WithInstance_ShouldAlsoRegisterAsEnumerable()
    {
        var concrete = new ConcreteRegisteredService();

        AddToServiceProvider<IRegisteredService>(concrete);

        var provider = GetMock<IServiceProvider>();
        var resolved = provider.Object.GetService(typeof(IEnumerable<IRegisteredService>));

        Assert.IsNotNull(resolved);
        var services = ((IEnumerable<IRegisteredService>)resolved!).ToArray();
        Assert.AreEqual(1, services.Length);
        Assert.AreSame(concrete, services[0]);
    }

    [TestMethod]
    public void AddToServiceProvider_MultipleInstances_ShouldReturnAllViaEnumerable()
    {
        var first = new ConcreteRegisteredService();
        var second = new ConcreteRegisteredService();

        AddToServiceProvider<IRegisteredService>(first);
        AddToServiceProvider<IRegisteredService>(second);

        var provider = GetMock<IServiceProvider>();
        var resolved = provider.Object.GetService(typeof(IEnumerable<IRegisteredService>));

        Assert.IsNotNull(resolved);
        var services = ((IEnumerable<IRegisteredService>)resolved!).ToArray();
        Assert.AreEqual(2, services.Length);
        Assert.AreSame(first, services[0]);
        Assert.AreSame(second, services[1]);
    }

    [TestMethod]
    public void AddToServiceProvider_MultipleInstances_GetServiceReturnsMostRecent()
    {
        var first = new ConcreteRegisteredService();
        var second = new ConcreteRegisteredService();

        AddToServiceProvider<IRegisteredService>(first);
        AddToServiceProvider<IRegisteredService>(second);

        var provider = GetMock<IServiceProvider>();
        var resolved = provider.Object.GetService(typeof(IRegisteredService));

        Assert.AreSame(second, resolved);
    }

    [TestMethod]
    public void AddToServiceProvider_NullType_ShouldThrow()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => AddToServiceProvider(null!));
    }

    //--- Lifecycle ---

    [TestMethod]
    public void Dummy_ShouldBeAvailableInTest()
    {
        Assert.IsNotNull(Dummy);
    }

    [TestMethod]
    public void Ensure_ShouldBeAvailableInTest()
    {
        Assert.IsNotNull(Ensure);
    }

    [TestMethod]
    public void JsonSerializerOptions_ShouldBeAvailableInTest()
    {
        Assert.IsNotNull(JsonSerializerOptions);
    }

}
