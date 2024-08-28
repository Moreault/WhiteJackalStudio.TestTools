namespace WhiteJackalStudio.TestTools;

public abstract class Tester
{
    protected Dummy Dummy { get; private set; } = null!;

    protected Ensure Ensure { get; private set; } = null!;

    protected JsonSerializerOptions JsonSerializerOptions => _jsonSerializerOptions.Value;
    private Lazy<JsonSerializerOptions> _jsonSerializerOptions = null!;

    [TestInitialize]
    public void TestInitializeBase()
    {
        Dummy = new Dummy();
        Ensure = new Ensure();
        _jsonSerializerOptions = new(() => new JsonSerializerOptions());
        InitializeTest();
    }

    protected virtual void InitializeTest()
    {

    }

    //Named as such to avoid unintentional shadowing
    [TestCleanup]
    public void TestCleanupOnBaseClass()
    {
        CleanupTest();
    }

    /// <summary>
    /// Runs after each test.
    /// </summary>
    protected virtual void CleanupTest()
    {

    }

    protected TValue? GetFieldValue<TInstance, TValue>(TInstance instance, string fieldName)
    {
        var fieldInfo = typeof(TInstance).GetSingleField(fieldName);
        return (TValue?)fieldInfo.GetValue(instance);
    }

    protected void SetFieldValue<TInstance, TValue>(TInstance instance, string fieldName, TValue value)
    {
        var fieldInfo = typeof(TInstance).GetSingleField(fieldName);
        fieldInfo.SetValue(instance, value);
    }

    protected TValue? GetPropertyValue<TInstance, TValue>(TInstance instance, string propertyName)
    {
        var propertyInfo = typeof(TInstance).GetSingleProperty(propertyName);
        return (TValue?)propertyInfo.GetValue(instance);
    }

    protected void SetPropertyValue<TInstance, TValue>(TInstance instance, string propertyName, TValue value)
    {
        var propertyInfo = typeof(TInstance).GetSingleProperty(propertyName);
        propertyInfo = propertyInfo.DeclaringType!.GetProperty(propertyName);
        propertyInfo!.SetValue(instance, value,
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance, null, null, null);
    }

    protected object? InvokeMethod<T>(T instance, string methodName, params object[] parameters)
    {
        if (instance == null) throw new ArgumentNullException(nameof(instance));
        if (string.IsNullOrWhiteSpace(methodName)) throw new ArgumentNullException(nameof(methodName));

        var methodInfo = parameters is null || !parameters.Any() ?
            instance.GetType().GetSingleMethod(methodName) :
            instance.GetType().GetSingleMethod(x => x.Name == methodName && x.HasParametersAssignableTo(parameters.Select(y => y?.GetType())));
        return methodInfo.Invoke(instance, parameters);
    }

    protected object? InvokeMethodAndIgnoreException<TInstance, TException>(TInstance instance, string methodName,
        params object[] parameters) where TException : Exception
    {
        try
        {
            return InvokeMethod(instance, methodName, parameters);
        }
        catch (TargetInvocationException e)
        {
            if (e.InnerException is not TException)
                throw;
        }

        return null;
    }
}

public abstract class Tester<T> : Tester where T : class
{
    private readonly IDictionary<Type, Mock> _mocks = new Dictionary<Type, Mock>();

    /// <summary>
    /// Parameters that were used to instantiate <see cref="Instance"/>.
    /// </summary>
    protected IReadOnlyList<object> ConstructorParameters => _constructorParameters;
    private readonly List<object> _constructorParameters = new();

    private readonly List<object> _overridenConstructorParameters = new();

    /// <summary>
    /// Instance of the class that is being tested.
    /// </summary>
    protected T Instance => _instance.Value;
    private Lazy<T> _instance = null!;

    protected Tester()
    {
        ResetInstance();
    }

    private void ResetInstance()
    {
        _instance = new Lazy<T>(() =>
        {
            var instance = InstanceProvider.Create<T>(Dummy, _overridenConstructorParameters, (IReadOnlyDictionary<Type, Mock>)_mocks);

            foreach (var mock in instance.Mocks)
            {
                if (_mocks.ContainsKey(mock.Key)) continue;
                _mocks[mock.Key] = mock.Value;
            }

            return instance.Value;
        });
    }

    protected override void CleanupTest()
    {
        base.CleanupTest();
        _mocks.Clear();
        _constructorParameters.Clear();
        _overridenConstructorParameters.Clear();
        ResetInstance();
    }

    protected Mock<TMock> GetMock<TMock>() where TMock : class
    {
        if (!_mocks.ContainsKey(typeof(TMock)))
            AddMock(typeof(TMock));
        return (Mock<TMock>)_mocks[typeof(TMock)];
    }

    private void AddMock(Type type)
    {
        var typeArgs = new[] { type };
        var mockType = typeof(Mock<>);
        var constructed = mockType.MakeGenericType(typeArgs);
        _mocks[type] = (Activator.CreateInstance(constructed) as Mock)!;
    }

    /// <summary>
    /// Returns field value by name on <see cref="Instance"/>.
    /// </summary>
    protected TValue? GetFieldValue<TValue>(string fieldName) => GetFieldValue<T, TValue>(Instance, fieldName);

    /// <summary>
    /// Sets field value by name on <see cref="Instance"/>.
    /// </summary>
    protected void SetFieldValue<TValue>(string fieldName, TValue value) => SetFieldValue(Instance, fieldName, value);

    /// <summary>
    /// Returns property value by name on <see cref="Instance"/>.
    /// </summary>
    protected TValue? GetPropertyValue<TValue>(string propertyName) => GetPropertyValue<T, TValue>(Instance, propertyName);

    /// <summary>
    /// Sets property value by name on <see cref="Instance"/>.
    /// </summary>
    protected void SetPropertyValue<TValue>(string propertyName, TValue value) => SetPropertyValue(Instance, propertyName, value);

    /// <summary>
    /// Sets up an options object (typically information found in an appsettings.json file.)
    /// </summary>
    protected TOptions SetupOptions<TOptions>(TOptions? options = null) where TOptions : class
    {
        options ??= Dummy.Create<TOptions>();
        GetMock<IOptions<TOptions>>().Setup(x => x.Value).Returns(options);
        return options;
    }

    /// <summary>
    /// Adds service of type <see cref="TService"/> to <see cref="IServiceProvider"/>.
    /// </summary>
    protected void AddToServiceProvider<TService>() where TService : class => AddToServiceProvider(typeof(TService));

    /// <summary>
    /// Adds service of specified type to <see cref="IServiceProvider"/>.
    /// </summary>
    protected void AddToServiceProvider(Type type)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));
        if (!_mocks.ContainsKey(typeof(IServiceProvider)))
            AddMock(typeof(IServiceProvider));
        if (!_mocks.ContainsKey(type))
            AddMock(type);
        GetMock<IServiceProvider>().Setup(x => x.GetService(type)).Returns(_mocks[type].Object);
    }

    /// <summary>
    /// Adds a service of type <see cref="TService"/> to <see cref="IServiceProvider"/> with a specific (non-mocked) instance.
    /// </summary>
    protected void AddToServiceProvider<TService>(object instance) where TService : class => AddToServiceProvider(typeof(TService), instance);

    /// <summary>
    /// Adds a service of the specified type to <see cref="IServiceProvider"/> with a specific (non-mocked) instance.
    /// </summary>
    protected void AddToServiceProvider(Type type, object instance)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));
        if (instance == null) throw new ArgumentNullException(nameof(instance));
        GetMock<IServiceProvider>().Setup(x => x.GetService(type)).Returns(instance);
    }

    /// <summary>
    /// Overrides <see cref="Instance"/> constructor parameters. Call before first accessing the <see cref="Instance"/> property.
    /// </summary>
    protected void ConstructWith(params object[] parameters)
    {
        if (parameters == null) throw new ArgumentNullException(nameof(parameters));
        if (_instance.IsValueCreated) throw new InvalidOperationException($"Can't override constructor parameters : the {nameof(ConstructWith)} method must be called before accessing the {nameof(Instance)} property.");
        //TODO Ensure that the parameters passed correspond to those of T's constructor
        _overridenConstructorParameters.AddRange(parameters);
    }

    /// <summary>
    /// Invokes a method by name on <see cref="Instance"/>.
    /// </summary>
    protected object? InvokeMethod(string methodName, params object[] parameters) => InvokeMethod(Instance, methodName, parameters);

    /// <summary>
    /// Invokes a method by name on <see cref="Instance"/>.
    /// </summary>
    protected object? InvokeMethodAndIgnoreException<TException>(string methodName, params object[] parameters) where TException : Exception => InvokeMethodAndIgnoreException<T, TException>(Instance, methodName, parameters);
}