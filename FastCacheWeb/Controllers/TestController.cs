using System.Collections.Concurrent;
using AspectCore.DynamicProxy;
using FastCacheWeb.Providers;
using Microsoft.AspNetCore.Mvc;

namespace FastCacheWeb.Controllers;

[ApiController]
[Route("[controller]")]
public class TestController : ControllerBase
{
    private readonly ITestService _testService;
    private readonly ISingletonService _singletonService;

    public TestController(ITestService testService, ISingletonService singletonService)
    {
        _testService = testService;
        _singletonService = singletonService;
    }

    [HttpGet("Test")]
    public async Task<int> Test()
    {
        return await _testService.Get();
    }
    
    [HttpGet("Test2")]
    public async Task<int> Test2()
    {
        var i = await _singletonService.Get();
        Console.WriteLine(i);
        return i;
    }
}

public interface ITestService
{
    [Aop(Duration = 100)]
    Task<int> Get();
}

public class TestService : ITestService
{
    public async Task<int> Get()
    {
        await Task.Delay(TimeSpan.FromMilliseconds(10));

        return 1;
    }
}

public interface ISingletonService
{
    [Aop(Duration = 100)]
    Task<int> Get();
}

public class SingletonService : ISingletonService
{
    public async Task<int> Get()
    {
        await Task.Delay(TimeSpan.FromMilliseconds(10));

        return GetHashCode();
    }
}

public class AopAttribute : AbstractInterceptorAttribute
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new();

    public int Duration { get; set; }
    public bool Forever { get; set; } = false;

    public override async Task Invoke(AspectContext context, AspectDelegate next)
    {
        var cacheProvider = context.ServiceProvider.GetRequiredService<ICacheProvider>();
        var key = GetKey(context);

        if (cacheProvider.Contains(key))
        {
            var result = cacheProvider.Get(key);
            if (result is Exception ex)
            {
                throw ex;
            }

            context.ReturnValue = result;

            return;
        }

        var semaphoreSlim = Locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        var customAttribute = GetCustomAttribute(context.ProxyMethod, typeof(AopAttribute)) as AopAttribute;

        try
        {
            await semaphoreSlim.WaitAsync();

            await next(context);

            cacheProvider.Put(key, context.ReturnValue, customAttribute!.Duration, customAttribute.Forever);
        }
        catch (Exception e)
        {
            cacheProvider.Put(key, e, customAttribute!.Duration, customAttribute.Forever);
            throw;
        }
        finally
        {
            semaphoreSlim.Release();
        }
    }

    private static string GetKey(AspectContext context)
    {
        return
            $"{context.Proxy}_{context.ProxyMethod.Name}_{string.Join("_", context.Parameters.Select(a => (a ?? "").ToString()))}";
    }
}