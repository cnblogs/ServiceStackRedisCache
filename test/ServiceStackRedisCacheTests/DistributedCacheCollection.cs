﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceStackRedisCacheTests
{
    [CollectionDefinition(nameof(DistributedCacheCollection), DisableParallelization = true)]
    public class DistributedCacheCollection : ICollectionFixture<DistributedCacheFixture>
    {
    }
}
