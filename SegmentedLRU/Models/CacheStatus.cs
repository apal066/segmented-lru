namespace SegmentedLRU.Models
{
    public class CacheStatus
    {
        public CacheStatus()
        {
            ColdCacheKeys = new();
            HotCacheKeys = new();
        }
        public List<Item> ColdCacheKeys { get; set; }
        public List<Item> HotCacheKeys { get; set; }
    }

    public class Item
    {
        public string Key { get; set; } = string.Empty;
        public DateTime LastAccessTime { get; set; }
        public int Frequency { get; set; }
    }
}
