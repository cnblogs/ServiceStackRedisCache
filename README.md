# ServiceStackRedisCache

Distributed cache implementation of Microsoft.Extensions.Caching.Distributed.IDistributedCache using ServiceStack.Redis.Core.

### Install Package
https://www.nuget.org/packages/ServiceStackRedisCache

### Configure

Startup.cs

```cs
public void ConfigureServices(IServiceCollection services)
{
    services.AddDistributedServiceStackRedisCache(options =>
    {
        Configuration.GetSection("redis").Bind(options);
        //Workaround for deadlock when resolving host name
        IPAddress ip;
        if (!IPAddress.TryParse(options.Host, out ip))
        {
            options.Host = Dns.GetHostAddressesAsync(options.Host)
            .Result.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork).ToString();
        }
    });
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
