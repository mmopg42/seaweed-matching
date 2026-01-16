using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using ChronoView.Core.GroupIdGeneration;
using ChronoView.Models;
using ChronoView.UI.ViewModels;
using Microsoft.Extensions.Logging;

namespace ChronoView.Core.FileMatching
{
    /// <summary>
    /// Service for matching files into groups based on timestamp correlation
    /// Implements the core grouping logic from Python's group_manager.py
    /// </summary>
    public class FileGroupMatcherService : IFileGroupMatcher
    {
        private readonly HashSet<string> _consumedNirKeys;
        private int _groupCounter;
        private readonly ILogger<FileGroupMatcherService>? _logger;
        private readonly IGroupIdGenerator _idGenerator;
        private Action<LogSeverity, string, string>? _uiLog;

        public MatchingConfiguration Configuration { get; set; }

        public FileGroupMatcherService(IGroupIdGenerator idGenerator, ILogger<FileGroupMatcherService>? logger = null)
        {
            _idGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
            _consumedNirKeys = new HashSet<string>();
            _groupCounter = 0;
            Configuration = new MatchingConfiguration();
            _logger = logger;
        }

        /// <summary>
        /// Set UI log action for forwarding logs to UI
        /// </summary>
        public void SetUILog(Action<LogSeverity, string, string>? uiLog)
        {
            _uiLog = uiLog;
        }

        public void ResetConsumedNirKeys()
        {
            _consumedNirKeys.Clear();
        }

        /// <summary>
        /// Reset all state including group counter and consumed NIR keys
        /// Call this before performing a fresh scan (e.g., during refresh)
        /// </summary>
        public void ResetState()
        {
            _consumedNirKeys.Clear();
            _groupCounter = 0;
            _idGenerator.Reset();
        }

        public void AddConsumedNirKey(string nirKey)
        {
            if (!string.IsNullOrEmpty(nirKey))
            {
                _consumedNirKeys.Add(nirKey);
            }
        }

        public async Task<IEnumerable<FileGroup>> MatchFilesAsync(UnmatchedFiles unmatchedFiles)
        {
            return await Task.Run(() =>
            {
                // Delegate to FileMatchingEngine for matching logic
                var groups = FileMatchingEngine.MatchFiles(
                    unmatchedFiles,
                    Configuration.DataSequenceSettings,
                    _consumedNirKeys,
                    _idGenerator,
                    _logger,
                    _uiLog);

                // Update state management
                _groupCounter = groups.Count;

                // Update consumed NIR keys for groups with NIR files
                foreach (var group in groups)
                {
                    if (group.HasNir && !string.IsNullOrEmpty(group.NirKey))
                    {
                        _consumedNirKeys.Add(group.NirKey);
                    }
                }

                return groups;
            });
        }

        public async Task<FileGroup> UpdateGroupAsync(FileGroup group, string filePath, FileType fileType)
        {
            return await Task.Run(() =>
            {
                // Implementation for real-time group updates
                // This would update the group with the new file based on file type
                return group;
            });
        }

    }
}
