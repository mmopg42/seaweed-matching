using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChronoView.Models;
using ChronoView.UI.ViewModels;

namespace ChronoView.Core.FileWatching
{
    public interface IGroupManager
    {
        event Action<LogSeverity, string>? Log;
        IEnumerable<FileGroup> ActiveGroups { get; }
        
        FileGroup? FindGroupById(string groupId);
        
        Task<FileGroup?> CreateOrUpdateGroupAsync(string filePath, FileType fileType, ApplicationConfiguration config, bool captureSuccess = true);
        
        bool RemoveGroup(string groupId);
        Task RemoveFileAsync(string filePath);
        void Clear();
        
        void ResetState();

        int GetActiveGroupsCount();
        
        /// <summary>
        /// Returns all file paths that have been processed. Used for FileWatcher injection optimization.
        /// </summary>
        IEnumerable<string> GetAllProcessedFilePaths();

        event EventHandler<FileGroup> GroupCreated;
        event EventHandler<FileGroup> GroupUpdated;
        event EventHandler<string> GroupRemoved;
    }
}
