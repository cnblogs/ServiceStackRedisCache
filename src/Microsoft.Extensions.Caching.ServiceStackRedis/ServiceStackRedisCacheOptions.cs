// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Caching.ServiceStackRedis
{
    public class ServiceStackRedisCacheOptions : IOptions<ServiceStackRedisCacheOptions>
    {
        public string Host { get; set; }

        public int Port { get; set; } = 6379;

        public string Password { get; set; }

        ServiceStackRedisCacheOptions IOptions<ServiceStackRedisCacheOptions>.Value
        {
            get { return this; }
        }
    }
}