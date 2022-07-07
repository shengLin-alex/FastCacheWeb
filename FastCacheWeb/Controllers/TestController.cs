using AspectCore.DynamicProxy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace FastCacheWeb.Controllers;

[ApiController]
[Route("[controller]")]
public class TestController : ControllerBase
{
    private readonly ITestService _testService;

    public TestController(ITestService testService)
    {
        _testService = testService;
    }

    [HttpGet("Test")]
    public async Task<int> Test()
    {
        return await _testService.Get();
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

public class AopAttribute : AbstractInterceptorAttribute
{
    private static readonly SemaphoreSlim SemaphoreSlim = new(1, 1);

    public int Duration { get; set; }
    public bool Forever { get; set; } = false;

    public override async Task Invoke(AspectContext context, AspectDelegate next)
    {
        var memoryCache = context.ServiceProvider.GetRequiredService<IMemoryCache>();

        var key = GetKey(context);

        if (memoryCache.TryGetValue(key, out var result))
        {
            if (result is Exception ex)
            {
                throw ex;
            }

            context.ReturnValue = result;

            return;
        }

        var customAttribute = GetCustomAttribute(context.ProxyMethod, typeof(AopAttribute)) as AopAttribute;

        try
        {
            await SemaphoreSlim.WaitAsync();

            await next(context);

            if (customAttribute!.Forever)
            {
                memoryCache.Set(key, context.ReturnValue);
                return;
            }

            if (customAttribute.Duration <= 0)
            {
                throw new ArgumentException(
                    "Duration cannot be less or equal to zero",
                    nameof(customAttribute.Duration));
            }

            memoryCache.Set(key, context.ReturnValue, TimeSpan.FromMilliseconds(customAttribute.Duration));
        }
        catch (Exception e)
        {
            if (customAttribute!.Forever)
            {
                memoryCache.Set(key, e);
                return;
            }

            if (customAttribute.Duration <= 0)
            {
                throw new ArgumentException(
                    "Duration cannot be less or equal to zero",
                    nameof(customAttribute.Duration));
            }

            memoryCache.Set(key, e, TimeSpan.FromMilliseconds(customAttribute.Duration));
            throw;
        }
        finally
        {
            SemaphoreSlim.Release();
        }
    }

    private static string GetKey(AspectContext context)
    {
        return
            $"{context.Proxy}_{context.ProxyMethod.Name}_{string.Join("_", context.Parameters.Select(a => (a ?? "").ToString()))}";
    }
}