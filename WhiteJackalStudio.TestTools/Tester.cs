namespace WhiteJackalStudio.TestTools;

public abstract class Tester
{
    protected Dummy Dummy { get; private set; } = null!;

    protected Ensure Ensure { get; private set; } = null!;

    protected JsonSerializerOptions JsonSerializerOptions => _jsonSerializerOptions.Value;
    private Lazy<JsonSerializerOptions> _jsonSerializerOptions = null!;

    /// <summary>
    /// Used to store information that is provided to unit tests.
    /// </summary>
    // ReSharper disable once ReplaceAutoPropertyWithComputedProperty : Automatically set by MSTest, not manually.
    public TestContext TestContext { get; } = null!;

    // ReSharper disable once UnusedMember.Global : Used by MSTest, not manually.
    public CancellationToken CancellationToken => TestContext.CancellationToken;

    [TestInitialize]
    public async Task TestInitializeBase()
    {
        Dummy = new Dummy();
        Ensure = new Ensure();
        _jsonSerializerOptions = new Lazy<JsonSerializerOptions>(() => new JsonSerializerOptions());
        // ReSharper disable once MethodHasAsyncOverload
        InitializeTest();
        await InitializeTestAsync();
    }

    protected virtual void InitializeTest()
    {

    }

    /// <summary>
    /// Runs before each test. Override this for async setup logic.
    /// </summary>
    protected virtual Task InitializeTestAsync() => Task.CompletedTask;

    //Named as such to avoid unintentional shadowing
    [TestCleanup]
    public async Task TestCleanupOnBaseClass()
    {
        // ReSharper disable once MethodHasAsyncOverload
        CleanupTest();
        await CleanupTestAsync();
    }

    /// <summary>
    /// Runs after each test.
    /// </summary>
    protected virtual void CleanupTest()
    {

    }

    /// <summary>
    /// Runs after each test. Override this for async cleanup logic.
    /// </summary>
    protected virtual Task CleanupTestAsync() => Task.CompletedTask;

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
        propertyInfo.SetValue(instance, value);
    }

    protected object? InvokeMethod<T>(T instance, string methodName, params object[] parameters)
    {
        if (instance == null) throw new ArgumentNullException(nameof(instance));
        if (string.IsNullOrWhiteSpace(methodName)) throw new ArgumentNullException(nameof(methodName));

        var methodInfo = parameters.Length == 0 ?
            instance.GetType().GetSingleMethod(methodName) :
            instance.GetType().GetSingleMethod(x => x.Name == methodName && x.HasParametersAssignableTo(parameters.Select(y => y.GetType())));
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
    private readonly Dictionary<Type, Mock> _mocks = new();
    private readonly Dictionary<Type, List<object>> _serviceProviderRegistrations = new();

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
            var instance = InstanceProvider.Create<T>(Dummy, _overridenConstructorParameters, _mocks);

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
        _serviceProviderRegistrations.Clear();
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
        AddToServiceProvider(type, _mocks[type].Object);
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

        if (!_serviceProviderRegistrations.TryGetValue(type, out var registrations))
        {
            registrations = new List<object>();
            _serviceProviderRegistrations[type] = registrations;
        }
        registrations.Add(instance);

        GetMock<IServiceProvider>().Setup(x => x.GetService(type)).Returns(instance);
        var genericEnumerable = typeof(IEnumerable<>).MakeGenericType(type);
        var typedArray = Array.CreateInstance(type, registrations.Count);
        for (var i = 0; i < registrations.Count; i++)
            typedArray.SetValue(registrations[i], i);
        GetMock<IServiceProvider>().Setup(x => x.GetService(genericEnumerable)).Returns(typedArray);
    }

    /// <summary>
    /// Overrides <see cref="Instance"/> constructor parameters. Call before first accessing the <see cref="Instance"/> property.
    /// </summary>
    protected void ConstructWith(params object[] parameters)
    {
        if (parameters == null) throw new ArgumentNullException(nameof(parameters));
        if (_instance.IsValueCreated) throw new InvalidOperationException($"Can't override constructor parameters : the {nameof(ConstructWith)} method must be called before accessing the {nameof(Instance)} property.");

        var constructors = typeof(T).GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        var parameterTypes = parameters.Select(p => p.GetType()).ToArray();
        var match = constructors.Any(c =>
        {
            var ctorParams = c.GetParameters();
            if (ctorParams.Length < parameterTypes.Length) return false;
            for (var i = 0; i < parameterTypes.Length; i++)
            {
                if (parameterTypes[i] == null) continue;
                if (!ctorParams[i].ParameterType.IsAssignableFrom(parameterTypes[i])) return false;
            }
            return true;
        });

        if (!match)
            throw new ArgumentException($"No constructor on {typeof(T).Name} accepts the provided parameter types: ({string.Join(", ", parameterTypes.Select(t => t?.Name ?? "null"))}).");

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