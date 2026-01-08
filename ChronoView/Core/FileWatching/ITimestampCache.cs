using System;

namespace ChronoView.Core.FileWatching
{
    public interface ITimestampCache
    {
        void Add(string folderPath, DateTime timestamp);
        bool TryGet(string folderPath, out DateTime timestamp);
        bool Remove(string folderPath);
        void Clear();
    }
}
