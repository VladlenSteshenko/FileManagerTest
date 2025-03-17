using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using FileManagerApp.Models;
using FileManagerApp.Helpers;
using Microsoft.AspNetCore.StaticFiles;

namespace FileManagerApp.Services
{
    public class FileSystemService : IFileSystemService
    {
        // Root directory where files are managed.
        private readonly string _rootDirectory;

        // Helper for path sanitization and validation.
        private readonly PathHelper _pathHelper;

        // Path to the application executable (used to prevent deletion).
        private readonly string _executablePath;

        public FileSystemService(IConfiguration configuration, PathHelper pathHelper)
        {
            // Set the root directory to the directory where the application is running.
            _rootDirectory = AppDomain.CurrentDomain.BaseDirectory;

            // Get the executable path.
            _executablePath = Assembly.GetEntryAssembly().Location;

            // Ensure that the root directory exists.
            if (!Directory.Exists(_rootDirectory))
            {
                Directory.CreateDirectory(_rootDirectory);
            }

            _pathHelper = pathHelper;
        }

        // Retrieves the contents of a directory, including subdirectories and files.
        public async Task<DirectoryContentModel> GetDirectoryContentsAsync(string relativePath)
        {
            if (!IsPathSafe(relativePath))
            {
                throw new UnauthorizedAccessException("Access to the path is denied.");
            }

            string fullPath = GetFullPath(relativePath);
            var directoryInfo = new DirectoryInfo(fullPath);

            if (!directoryInfo.Exists)
            {
                throw new DirectoryNotFoundException($"Directory not found: {relativePath}");
            }

            var model = new DirectoryContentModel
            {
                CurrentPath = relativePath,
                ParentPath = _pathHelper.GetParentPath(relativePath),
                Items = new List<FileItemModel>()
            };

            // Add directories to the model.
            foreach (var dir in directoryInfo.GetDirectories())
            {
                model.Items.Add(new FileItemModel
                {
                    Name = dir.Name,
                    IsDirectory = true,
                    LastModified = dir.LastWriteTime,
                    Path = Path.Combine(relativePath, dir.Name)
                });
            }

            // Add files to the model.
            foreach (var file in directoryInfo.GetFiles())
            {
                model.Items.Add(new FileItemModel
                {
                    Name = file.Name,
                    IsDirectory = false,
                    Size = file.Length,
                    LastModified = file.LastWriteTime,
                    Path = Path.Combine(relativePath, file.Name),
                    ContentType = _pathHelper.GetContentType(file.Name)
                });
            }

            // Sort items: directories first, then files; then sort alphabetically.
            model.Items = model.Items
                .OrderBy(i => !i.IsDirectory)
                .ThenBy(i => i.Name)
                .ToList();

            return await Task.FromResult(model);
        }

        // Retrieves a file as a byte array along with its content type and file name.
        public async Task<(byte[] FileContents, string ContentType, string FileName)> GetFileAsync(string relativePath)
        {
            if (!IsPathSafe(relativePath))
            {
                throw new UnauthorizedAccessException("Access to the path is denied.");
            }

            string fullPath = GetFullPath(relativePath);
            var fileInfo = new FileInfo(fullPath);

            if (!fileInfo.Exists)
            {
                throw new FileNotFoundException($"File not found: {relativePath}");
            }

            byte[] fileContents = await File.ReadAllBytesAsync(fullPath);
            string contentType = _pathHelper.GetContentType(fileInfo.Name);

            return (fileContents, contentType, fileInfo.Name);
        }

        // Uploads a file to the specified directory.
        public async Task<FileOperationResult> UploadFileAsync(string relativePath, IFormFile file)
        {
            if (!IsPathSafe(relativePath))
            {
                return new FileOperationResult { Success = false, Message = "Access to the path is denied." };
            }

            if (file == null || file.Length == 0)
            {
                return new FileOperationResult { Success = false, Message = "No file was uploaded." };
            }

            try
            {
                string directoryPath = GetFullPath(relativePath);

                // Create the directory if it doesn't exist.
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                string filePath = Path.Combine(directoryPath, file.FileName);

                // Save the uploaded file.
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return new FileOperationResult
                {
                    Success = true,
                    Message = $"File {file.FileName} uploaded successfully.",
                    Path = Path.Combine(relativePath, file.FileName)
                };
            }
            catch (Exception ex)
            {
                return new FileOperationResult { Success = false, Message = $"Error uploading file: {ex.Message}" };
            }
        }

        // Creates a new directory at the specified relative path.
        public async Task<FileOperationResult> CreateDirectoryAsync(string relativePath)
        {
            if (!IsPathSafe(relativePath))
            {
                return new FileOperationResult { Success = false, Message = "Access to the path is denied." };
            }

            try
            {
                string fullPath = GetFullPath(relativePath);

                if (Directory.Exists(fullPath))
                {
                    return new FileOperationResult { Success = false, Message = "Directory already exists." };
                }

                Directory.CreateDirectory(fullPath);

                return await Task.FromResult(new FileOperationResult
                {
                    Success = true,
                    Message = "Directory created successfully.",
                    Path = relativePath
                });
            }
            catch (Exception ex)
            {
                return new FileOperationResult { Success = false, Message = $"Error creating directory: {ex.Message}" };
            }
        }

        // Deletes a file or directory (recursively for directories).
        public async Task<FileOperationResult> DeleteItemAsync(string relativePath)
        {
            if (!IsPathSafe(relativePath))
            {
                return new FileOperationResult { Success = false, Message = "Access to the path is denied." };
            }

            // Prevent deletion of the root directory.
            if (string.IsNullOrEmpty(relativePath))
            {
                return new FileOperationResult { Success = false, Message = "Cannot delete the root directory." };
            }

            try
            {
                string fullPath = GetFullPath(relativePath);

                // Prevent deletion if the full path matches the root directory.
                if (Path.GetFullPath(fullPath).Equals(Path.GetFullPath(_rootDirectory), StringComparison.OrdinalIgnoreCase))
                {
                    return new FileOperationResult { Success = false, Message = "Cannot delete the root directory." };
                }

                // If the path points to a file, delete the file.
                if (File.Exists(fullPath))
                {
                    string fileNameToDelete = Path.GetFileName(fullPath);
                    string executableFileName = Path.GetFileName(_executablePath);

                    // Prevent deletion of the application executable.
                    if (string.Equals(fileNameToDelete, executableFileName, StringComparison.OrdinalIgnoreCase) ||
                        Path.GetFullPath(fullPath).Equals(Path.GetFullPath(_executablePath), StringComparison.OrdinalIgnoreCase))
                    {
                        return new FileOperationResult { Success = false, Message = "Cannot delete the application executable." };
                    }

                    File.Delete(fullPath);
                    return await Task.FromResult(new FileOperationResult
                    {
                        Success = true,
                        Message = "File deleted successfully.",
                        Path = relativePath
                    });
                }

                // If the path points to a directory, delete it recursively.
                if (Directory.Exists(fullPath))
                {
                    // Prevent deletion of a directory that contains the executable.
                    if (_executablePath.StartsWith(fullPath, StringComparison.OrdinalIgnoreCase))
                    {
                        return new FileOperationResult { Success = false, Message = "Cannot delete a directory containing the application executable." };
                    }

                    Directory.Delete(fullPath, true);
                    return await Task.FromResult(new FileOperationResult
                    {
                        Success = true,
                        Message = "Directory deleted successfully.",
                        Path = relativePath
                    });
                }

                return new FileOperationResult { Success = false, Message = "Item not found." };
            }
            catch (Exception ex)
            {
                return new FileOperationResult { Success = false, Message = $"Error deleting item: {ex.Message}" };
            }
        }

        // Searches for files and folders matching the provided query string.
        public async Task<List<FileItemModel>> SearchFilesAndFoldersAsync(string query)
        {
            var results = new List<FileItemModel>();

            await Task.Run(() =>
            {
                try
                {
                    // Search for directories that contain the query.
                    var directories = Directory.GetDirectories(_rootDirectory, "*", SearchOption.AllDirectories)
                        .Where(d => Path.GetFileName(d).Contains(query, StringComparison.OrdinalIgnoreCase))
                        .Select(d => new FileItemModel
                        {
                            Name = Path.GetFileName(d),
                            Path = d.Replace(_rootDirectory, "").TrimStart(Path.DirectorySeparatorChar),
                            IsDirectory = true
                        });

                    // Search for files that contain the query.
                    var files = Directory.GetFiles(_rootDirectory, "*", SearchOption.AllDirectories)
                        .Where(f => Path.GetFileName(f).Contains(query, StringComparison.OrdinalIgnoreCase))
                        .Select(f => new FileItemModel
                        {
                            Name = Path.GetFileName(f),
                            Path = f.Replace(_rootDirectory, "").TrimStart(Path.DirectorySeparatorChar),
                            IsDirectory = false
                        });

                    results.AddRange(directories);
                    results.AddRange(files);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error searching files and folders: {ex.Message}");
                }
            });

            return results;
        }

        // Retrieves detailed information about a file or folder.
        public async Task<FileItemModel> GetFileOrFolderInfoAsync(string relativePath)
        {
            if (!IsPathSafe(relativePath))
            {
                throw new UnauthorizedAccessException("Access to the path is denied.");
            }

            string fullPath = GetFullPath(relativePath);

            if (Directory.Exists(fullPath))
            {
                var dirInfo = new DirectoryInfo(fullPath);
                return await Task.FromResult(new FileItemModel
                {
                    Name = dirInfo.Name,
                    Path = relativePath,
                    IsDirectory = true,
                    Size = 0, // Directory size set to 0 (or calculated if needed).
                    LastModified = dirInfo.LastWriteTime,
                    ContentType = "folder"
                });
            }
            else if (File.Exists(fullPath))
            {
                var fileInfo = new FileInfo(fullPath);
                return await Task.FromResult(new FileItemModel
                {
                    Name = fileInfo.Name,
                    Path = relativePath,
                    IsDirectory = false,
                    Size = fileInfo.Length,
                    LastModified = fileInfo.LastWriteTime,
                    ContentType = _pathHelper.GetContentType(fileInfo.Name)
                });
            }
            else
            {
                return null;
            }
        }

        // Validates that the given relative path is safe with respect to the root directory.
        public bool IsPathSafe(string relativePath)
        {
            return _pathHelper.IsPathSafe(_rootDirectory, relativePath);
        }

        // Combines the root directory with the sanitized relative path to form the full system path.
        public string GetFullPath(string relativePath)
        {
            string sanitizedPath = _pathHelper.SanitizePath(relativePath);
            return Path.Combine(_rootDirectory, sanitizedPath);
        }
    }
}
