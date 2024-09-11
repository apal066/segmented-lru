using Newtonsoft.Json;
using SegmentedLRU.Models;
using StackExchange.Redis;

namespace SegmentedLRU.Services
{
    public class RedisSLRUService<T> : ICachingService<T> where T : class
    {
        private readonly IDatabase _redis;
        private readonly ILogger<RedisSLRUService<T>> _logger;

        private const string _hotCache = "hot_cache";
        private const string _coldCache = "cold_cache";
        private const int _hotCacheCapacity = 2;  
        private const int _coldCacheCapacity = 3;
        private const int _promotionThreshold = 1;
        public RedisSLRUService(IConnectionMultiplexer connectionMultiplexer,
            ILogger<RedisSLRUService<T>> logger)
        {
            _redis = connectionMultiplexer.GetDatabase();
            _logger = logger;
        }

        public async Task<T?> GetAsync(string key)
        {
            var redisValue = await _redis.HashGetAsync(_hotCache, key);

            if (!redisValue.HasValue)
            {
                // Not found in hot cache. Try in cold cache
                _logger.LogInformation($"Cache miss in hot cache: {key}");

                redisValue = await _redis.HashGetAsync(_coldCache, key);

                if (!redisValue.HasValue)
                {
                    // Not found anywhere
                    _logger.LogInformation($"Cache miss in cold cache: {key}");
                    return null;
                }
                // Found in cold cache
                _logger.LogInformation($"Cache hit in cold cache: {key}");

                var coldItem = AccessItem(redisValue);

                if (coldItem == null)
                    return null;

                await PromoteIfRequired(coldItem);

                return coldItem.Value;
            }

            // Found in hot cache
            _logger.LogInformation($"Cache hit in hot cache: {key}");

            var hotItem = AccessItem(redisValue);

            if (hotItem == null)
                return null;

            // Updates frequency of the cache item
            await _redis.HashSetAsync(_hotCache, key, JsonConvert.SerializeObject(hotItem));

            return hotItem.Value; 
        }

        public async Task SetAsync(string key, T value)
        {
            if (await _redis.HashExistsAsync(_hotCache, key) || await _redis.HashExistsAsync(_coldCache, key))
            {
                _logger.LogInformation($"Cache hit while set: {key}");
                return;  // Key already exists, no need to set again
            }

            var cacheItem = new CacheItem<T>(key, value);
            cacheItem.LastAccessTime = DateTime.Now;
            await _redis.HashSetAsync(_coldCache, key, JsonConvert.SerializeObject(cacheItem));

            await CheckCapacityAsync();
        }

        public async Task<CacheStatus> GetCurrentCacheKeys()
        {
            var coldCacheKeys = await _redis.HashKeysAsync(_coldCache);
            var hotCacheKeys = await _redis.HashKeysAsync(_hotCache);

            var coldCacheTasks = coldCacheKeys.Select(async x =>
            {
                var cacheItem = await GetItem(_coldCache, x);
                return new Item
                {
                    Key = x.ToString(),
                    Frequency = cacheItem?.Frequency ?? 0,
                    LastAccessTime = cacheItem?.LastAccessTime ?? DateTime.MinValue,
                };
            }).ToArray();

            var hotCacheTasks = hotCacheKeys.Select(async x =>
            {
                var cacheItem = await GetItem(_hotCache, x);
                return new Item
                {
                    Key = x.ToString(),
                    Frequency = cacheItem?.Frequency ?? 0,
                    LastAccessTime = cacheItem?.LastAccessTime ?? DateTime.MinValue,
                };
            }).ToArray();

            var coldCacheItems = await Task.WhenAll(coldCacheTasks);

            var hotCacheItems = await Task.WhenAll(hotCacheTasks);

            return new CacheStatus
            {
                ColdCacheKeys = coldCacheItems.OrderBy(x=>x.LastAccessTime).ToList(),
                HotCacheKeys = hotCacheItems.OrderBy(x => x.LastAccessTime).ToList(),
            };
        }

        public async Task ClearCache()
        {
            await _redis.ExecuteAsync("FLUSHDB");
        }

        #region Private Methods

        private CacheItem<T>? AccessItem(RedisValue redisValue)
        {
            var cacheItem = JsonConvert.DeserializeObject<CacheItem<T>>(redisValue.ToString());

            if (cacheItem == null)
                return null;

            // Increment frequency
            cacheItem.Accessed();

            return cacheItem;
        }

        private async Task PromoteIfRequired(CacheItem<T> cacheItem)
        {
            if (cacheItem.Frequency > _promotionThreshold)
            {
                await PromoteToHotCacheAsync(cacheItem);
            }
        }

        private async Task PromoteToHotCacheAsync(CacheItem<T> cacheItem)
        {
            _logger.LogInformation($"Promoting {cacheItem.Key} to hot segment");

            await _redis.HashSetAsync(_hotCache, cacheItem.Key, JsonConvert.SerializeObject(cacheItem));

            await _redis.HashDeleteAsync(_coldCache, cacheItem.Key);

            await CheckCapacityAsync();
        }

        private async Task CheckCapacityAsync()
        {
            // Check and evict from probation segment
            var coldCacheCount = await _redis.HashLengthAsync(_coldCache);
            if (coldCacheCount > _coldCacheCapacity)
            {
                var oldestItem = await GetOldestItem(_coldCache);
                if (oldestItem != null)
                {
                    _logger.LogInformation($"Evicting {oldestItem.Key} from cold cache");
                    await _redis.HashDeleteAsync(_coldCache, oldestItem.Key);
                }
            }

            // Check and evict from protected segment
            var hotCacheCount = await _redis.HashLengthAsync(_hotCache);
            if (hotCacheCount > _hotCacheCapacity)
            {
                var oldestItem = await GetOldestItem(_hotCache);
                if (oldestItem != null)
                {
                    _logger.LogInformation($"Evicting {oldestItem.Key} from hot cache");

                    await _redis.HashDeleteAsync(_hotCache, oldestItem.Key);

                    await _redis.HashSetAsync(_coldCache, oldestItem.Key, JsonConvert.SerializeObject(oldestItem));
                }
            }
        }

        private async Task<CacheItem<T>> GetOldestItem(string cacheSegment)
        {
            // Caution: This method should not be followed in live systems.
            // This gets all items in the cache.
            // I did this to get the demo working quickly.

            var coldCacheKeys = await _redis.HashKeysAsync(cacheSegment);
            var items = new List<CacheItem<T>>();

            foreach (var key in coldCacheKeys)
            {
                var cacheItem = await GetItem(cacheSegment, key);
                if (cacheItem != null)
                    items.Add(cacheItem);
            }
            return items.OrderBy(x => x.LastAccessTime).First();
        }     

        private async Task<CacheItem<T>?> GetItem(string cacheSegment, RedisValue key)
        {
            var redisValue = await _redis.HashGetAsync(cacheSegment, key);
            if (!redisValue.HasValue)
            {
                return null;
            }
            var cacheItem = JsonConvert.DeserializeObject<CacheItem<T>>(redisValue.ToString());

            if (cacheItem == null)
                return null;

            return cacheItem;
        }

        #endregion
    }
}
