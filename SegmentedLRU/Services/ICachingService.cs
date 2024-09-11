using SegmentedLRU.Models;

namespace SegmentedLRU.Services
{
    public interface ICachingService<T>
    {
        /// <summary>
        /// Gets a cached item by key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Task<T?> GetAsync(string key);

        /// <summary>
        /// Sets a key value to cache
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        Task SetAsync(string key, T value);

        /// <summary>
        /// Get cold and hot cache keys
        /// </summary>
        /// <returns></returns>
        Task<CacheStatus> GetCurrentCacheKeys();

        /// <summary>
        /// Clear all items in cache
        /// </summary>
        /// <returns></returns>
        Task ClearCache();
    }
}
