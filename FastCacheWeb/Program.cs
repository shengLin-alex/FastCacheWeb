using AspectCore.Configuration;
using AspectCore.Extensions.DependencyInjection;
using FastCacheWeb.Controllers;
using FastCacheWeb.Providers;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseServiceProviderFactory(new DynamicProxyServiceProviderFactory());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMemoryCache();
builder.Services.AddTransient<ICacheProvider, MemoryCacheProvider>();

builder.Services.AddTransient<ITestService, TestService>();
builder.Services.AddSingleton<ISingletonService, SingletonService>();
builder.Services.ConfigureDynamicProxy(
    config =>
    {
        config.Interceptors.AddTyped<AopAttribute>(
            Predicates.ForService(nameof(ITestService)),
            Predicates.ForService(nameof(ISingletonService)));
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();