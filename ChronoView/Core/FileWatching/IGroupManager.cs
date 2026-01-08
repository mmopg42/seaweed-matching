using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChronoView.Models;

namespace ChronoView.Core.FileWatching
{
    public interface IGroupManager
    {
        event Action<string> Log;
        IEnumerable<FileGroup> ActiveGroups { get; }
        
        FileGroup? FindGroupById(string groupId);
        
        Task<FileGroup?> CreateOrUpdateGroupAsync(string filePath, FileType fileType, ApplicationConfiguration config);
        
        bool RemoveGroup(string groupId);
        Task RemoveFileAsync(string filePath);
        void Clear();
        
        void ResetState();

        int GetActiveGroupsCount();

        event EventHandler<FileGroup> GroupCreated;
        event EventHandler<FileGroup> GroupUpdated;
        event EventHandler<string> GroupRemoved;
    }
}
