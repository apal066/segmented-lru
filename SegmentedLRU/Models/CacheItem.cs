namespace SegmentedLRU.Models
{
    public class CacheItem<T>
    {
        public CacheItem(string key, T value)
        {
            Key = key;
            Value = value;
            Frequency = 0;
        }
        public string Key { get; set; }
        public T Value { get; private set; }
        public DateTime LastAccessTime { get; set; }
        public int Frequency { get; set; }

        // Method to update access time and frequency
        public void Accessed()
        {
            LastAccessTime = DateTime.Now;
            Frequency++;
        }
    }

}
