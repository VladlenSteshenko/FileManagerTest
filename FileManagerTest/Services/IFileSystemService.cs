using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using FileManagerApp.Models;

namespace FileManagerApp.Services
{
    public interface IFileSystemService
    {
        // Retrieve the contents of a directory.
        Task<DirectoryContentModel> GetDirectoryContentsAsync(string relativePath);

        // Download a file from the specified relative path.
        Task<(byte[] FileContents, string ContentType, string FileName)> GetFileAsync(string relativePath);

        // Upload a file to a specific directory.
        Task<FileOperationResult> UploadFileAsync(string relativePath, IFormFile file);

        // Create a new directory at the specified relative path.
        Task<FileOperationResult> CreateDirectoryAsync(string relativePath);

        // Delete a file or directory at the specified relative path.
        Task<FileOperationResult> DeleteItemAsync(string relativePath);

        // Search for files and folders matching the query.
        Task<List<FileItemModel>> SearchFilesAndFoldersAsync(string query);

        // Retrieve detailed information about a file or folder.
        Task<FileItemModel> GetFileOrFolderInfoAsync(string relativePath);

        // Validate that the provided relative path is safe.
        bool IsPathSafe(string relativePath);

        // Get the full system path from the given relative path.
        string GetFullPath(string relativePath);
    }
}
