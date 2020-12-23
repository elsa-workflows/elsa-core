﻿using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace Elsa
{
    public static class CacheEntryExtensions
    {
        public static void Monitor(this ICacheEntry entry, IChangeToken changeToken) =>
            entry.ExpirationTokens.Add(changeToken);
    }
}