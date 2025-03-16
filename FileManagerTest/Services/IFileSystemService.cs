// IFileSystemService.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using FileManagerApp.Models;

namespace FileManagerApp.Services
{
    public interface IFileSystemService
    {
        // Get contents of a directory
        Task<DirectoryContentModel> GetDirectoryContentsAsync(string relativePath);

        // Download a file
        Task<(byte[] FileContents, string ContentType, string FileName)> GetFileAsync(string relativePath);

        // Upload a file to a directory
        Task<FileOperationResult> UploadFileAsync(string relativePath, IFormFile file);

        // Create a new directory
        Task<FileOperationResult> CreateDirectoryAsync(string relativePath);

        // Delete a file or directory
        Task<FileOperationResult> DeleteItemAsync(string relativePath);

        // search by name
        Task<List<FileItemModel>> SearchFilesAndFoldersAsync(string query);

        // get info
        Task<FileItemModel> GetFileOrFolderInfoAsync(string relativePath);


        // Validate a path is within the root directory
        bool IsPathSafe(string relativePath);

        // Get the full system path from relative path
        string GetFullPath(string relativePath);

    }
}
