# ServiceStackRedisCache

Distributed cache implementation of Microsoft.Extensions.Caching.Distributed.IDistributedCache using ServiceStack.Redis.

### Install Package
https://www.nuget.org/packages/ServiceStackRedisCache or https://www.nuget.org/packages/Microsoft.Extensions.Caching.ServiceStackRedis

### Configure

Startup.cs

```cs
public void ConfigureServices(IServiceCollection services)
{
    services.AddDistributedServiceStackRedisCache("redis");
}
```
appsettings.json

```json
{
  "redis": {
    "Host": "xxx",
    "Password": "xxx"
  }
}
```
