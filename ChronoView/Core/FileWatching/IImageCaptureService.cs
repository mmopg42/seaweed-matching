using System;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using ChronoView.Models;

namespace ChronoView.Core.FileWatching
{
    /// <summary>
    /// Interface for image capture service that handles race conditions between file creation and ML processing
    /// </summary>
    public interface IImageCaptureService
    {
        /// <summary>
        /// Load image into memory immediately with retry logic
        /// </summary>
        Task<BitmapImage?> LoadImageIntoMemoryAsync(string imagePath);

        /// <summary>
        /// Handle capture of stitched images before they are moved by ML program
        /// </summary>
        Task HandleStitchedImageCaptureAsync(string imagePath, int workerId, Action<FileGroup> onGroupUpdated);

        /// <summary>
        /// Get cached image by group ID
        /// </summary>
        BitmapImage? GetCapturedImage(string groupId);

        /// <summary>
        /// Get cached image by folder path (fallback)
        /// </summary>
        BitmapImage? GetCapturedImageByFolderPath(string folderPath);

        /// <summary>
        /// Promote cached image from folder-based key to GroupID-based key
        /// </summary>
        void PromoteCacheToGroupId(string folderPath, string groupId);

        /// <summary>
        /// Clear the image cache
        /// </summary>
        void Clear();
    }
}
