// PathHelper.cs
using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.StaticFiles;

namespace FileManagerApp.Helpers
{
    public class PathHelper
    {
        private readonly FileExtensionContentTypeProvider _contentTypeProvider;
        private readonly string[] _windowsSystemDirectories = new string[]
        {
            @"C:\Windows",
            @"C:\Program Files",
            @"C:\Program Files (x86)",
            @"C:\ProgramData",
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            Environment.GetFolderPath(Environment.SpecialFolder.SystemX86),
            Environment.GetFolderPath(Environment.SpecialFolder.Windows)
        };

        public PathHelper()
        {
            _contentTypeProvider = new FileExtensionContentTypeProvider();
        }

        public string SanitizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            // Replace backslashes with forward slashes and remove any starting slash
            string sanitized = path.Replace('\\', '/').TrimStart('/');

            // Remove any attempts to navigate up using ".." path traversal
            sanitized = string.Join("/", sanitized.Split('/').Where(s => s != ".."));

            return sanitized;
        }

        public bool IsPathSafe(string rootPath, string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                return true; // Root directory is always safe
            }

            try
            {
                // Combine paths and get canonical form
                string fullPath = Path.GetFullPath(Path.Combine(rootPath, SanitizePath(relativePath)));
                string rootFullPath = Path.GetFullPath(rootPath);

                // 1. Check if the full path starts with the root path
                bool isInRoot = fullPath.StartsWith(rootFullPath, StringComparison.OrdinalIgnoreCase);
                if (!isInRoot)
                {
                    return false;
                }

                // 2. Check against system directories
                foreach (var systemDir in _windowsSystemDirectories)
                {
                    if (!string.IsNullOrEmpty(systemDir) &&
                        (fullPath.StartsWith(systemDir, StringComparison.OrdinalIgnoreCase) ||
                         systemDir.StartsWith(fullPath, StringComparison.OrdinalIgnoreCase)))
                    {
                        return false;
                    }
                }

                // 3. Check against common system drive paths
                DriveInfo[] drives = DriveInfo.GetDrives();
                foreach (var drive in drives)
                {
                    // Prevent access to drive roots
                    if (fullPath.Equals(drive.RootDirectory.FullName, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }

                // 4. Additional check for reserved file names
                string fileName = Path.GetFileName(fullPath);
                string[] reservedNames = { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5",
                                          "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4",
                                          "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" };

                if (!string.IsNullOrEmpty(fileName) && reservedNames.Contains(fileName.ToUpper()))
                {
                    return false;
                }

                return true;
            }
            catch
            {
                return false; // If there's any error, consider the path unsafe
            }
        }

        public string GetParentPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty; // Already at root
            }
            var sanitizedPath = SanitizePath(path);
            var parentPath = Path.GetDirectoryName(sanitizedPath);
            // Normalize for web paths
            return parentPath?.Replace('\\', '/') ?? string.Empty;
        }

        public string GetContentType(string fileName)
        {
            if (!_contentTypeProvider.TryGetContentType(fileName, out var contentType))
            {
                contentType = "application/octet-stream"; // Default content type
            }
            return contentType;
        }
    }
}