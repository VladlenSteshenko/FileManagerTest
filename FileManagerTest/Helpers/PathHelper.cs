using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.StaticFiles;

namespace FileManagerApp.Helpers
{
    public class PathHelper
    {
        // Provides content type mappings based on file extensions.
        private readonly FileExtensionContentTypeProvider _contentTypeProvider;

        // Array of common Windows system directories that should be protected.
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

        // Sanitize the provided path to prevent directory traversal and normalize separators.
        public string SanitizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            // Replace backslashes with forward slashes and remove any starting slash.
            string sanitized = path.Replace('\\', '/').TrimStart('/');

            // Remove any ".." segments to prevent navigating up directories.
            sanitized = string.Join("/", sanitized.Split('/').Where(s => s != ".."));

            return sanitized;
        }

        // Check if a given relative path is safe with respect to the provided root path.
        public bool IsPathSafe(string rootPath, string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                return true; // Root directory is always safe.
            }

            try
            {
                // Combine the root path and sanitized relative path, then get the full canonical path.
                string fullPath = Path.GetFullPath(Path.Combine(rootPath, SanitizePath(relativePath)));
                string rootFullPath = Path.GetFullPath(rootPath);

                // Ensure the full path starts with the root full path.
                bool isInRoot = fullPath.StartsWith(rootFullPath, StringComparison.OrdinalIgnoreCase);
                if (!isInRoot)
                {
                    return false;
                }

                // Prevent access to protected Windows system directories.
                foreach (var systemDir in _windowsSystemDirectories)
                {
                    if (!string.IsNullOrEmpty(systemDir) &&
                        (fullPath.StartsWith(systemDir, StringComparison.OrdinalIgnoreCase) ||
                         systemDir.StartsWith(fullPath, StringComparison.OrdinalIgnoreCase)))
                    {
                        return false;
                    }
                }

                // Prevent access to drive roots by iterating through available drives.
                DriveInfo[] drives = DriveInfo.GetDrives();
                foreach (var drive in drives)
                {
                    if (fullPath.Equals(drive.RootDirectory.FullName, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }

                // Check against reserved file names.
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
                return false; // On error, consider the path unsafe.
            }
        }

        // Returns the parent directory of a given path.
        public string GetParentPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty; // Already at root.
            }
            var sanitizedPath = SanitizePath(path);
            var parentPath = Path.GetDirectoryName(sanitizedPath);
            // Normalize backslashes for web usage.
            return parentPath?.Replace('\\', '/') ?? string.Empty;
        }

        // Get the content type for a file based on its extension.
        public string GetContentType(string fileName)
        {
            if (!_contentTypeProvider.TryGetContentType(fileName, out var contentType))
            {
                contentType = "application/octet-stream"; // Default content type.
            }
            return contentType;
        }
    }
}
