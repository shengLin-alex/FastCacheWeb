using AspectCore.Configuration;
using AspectCore.Extensions.DependencyInjection;
using FastCacheWeb.Controllers;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseServiceProviderFactory(new DynamicProxyServiceProviderFactory());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMemoryCache();
// builder.Services.AddTransient<ICacheProvider, MemoryCacheProvider>();

builder.Services.AddTransient<ITestService, TestService>();
builder.Services.ConfigureDynamicProxy(
    config =>
    {
        config.Interceptors.AddTyped<AopAttribute>();
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();