using System;
using System.IO;
using System.Threading.Tasks;
using FileManagerApp.Models;
using FileManagerApp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FileManagerApp.Controllers
{
    [ApiController]
    [Route("api/files")]
    public class FilesController : ControllerBase
    {
        private readonly IFileSystemService _fileSystemService;

        // Constructor: Injects the file system service for handling file operations.
        public FilesController(IFileSystemService fileSystemService)
        {
            _fileSystemService = fileSystemService;
        }

        // GET: api/files/rootInfo
        // Retrieves basic information about the root folder.
        [HttpGet("rootInfo")]
        public async Task<IActionResult> GetRootFolderContents()
        {
            // Get the absolute root path using the file system service.
            string absolutePath = _fileSystemService.GetFullPath("");

            // Retrieve the contents of the root directory.
            var content = await _fileSystemService.GetDirectoryContentsAsync("");

            return Ok(new { AbsolutePath = absolutePath, Content = content });
        }

        // GET: api/files/directory?path=...
        // Retrieves the contents of a specified directory.
        [HttpGet("directory")]
        public async Task<IActionResult> GetDirectoryContents([FromQuery] string path = "")
        {
            // Validate that the path is safe.
            if (!_fileSystemService.IsPathSafe(path))
                return BadRequest("Invalid path");

            var contents = await _fileSystemService.GetDirectoryContentsAsync(path);
            return Ok(contents);
        }

        // GET: api/files/download?path=...
        // Downloads a file from the specified path.
        [HttpGet("download")]
        public async Task<IActionResult> DownloadFile([FromQuery] string path)
        {
            // Validate the file path.
            if (string.IsNullOrEmpty(path) || !_fileSystemService.IsPathSafe(path))
                return BadRequest("Invalid file path");

            var (fileContents, contentType, fileName) = await _fileSystemService.GetFileAsync(path);
            return File(fileContents, contentType, fileName);
        }

        // POST: api/files/upload?path=...
        // Uploads a file to the specified directory.
        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile([FromQuery] string path, IFormFile file)
        {
            // Validate the upload request and path safety.
            if (file == null || string.IsNullOrEmpty(path) || !_fileSystemService.IsPathSafe(path))
                return BadRequest("Invalid upload request");

            var result = await _fileSystemService.UploadFileAsync(path, file);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // POST: api/files/upload-root
        // Uploads a file to the root directory.
        [HttpPost("upload-root")]
        public async Task<IActionResult> UploadFileToRoot(IFormFile file)
        {
            // Ensure a file is provided.
            if (file == null)
                return BadRequest("Invalid upload request");

            var result = await _fileSystemService.UploadFileAsync(string.Empty, file);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // POST: api/files/directory?path=...
        // Creates a new directory at the specified path.
        [HttpPost("directory")]
        public async Task<IActionResult> CreateDirectory([FromQuery] string path)
        {
            // Validate the directory path.
            if (string.IsNullOrEmpty(path) || !_fileSystemService.IsPathSafe(path))
                return BadRequest("Invalid directory path");

            var result = await _fileSystemService.CreateDirectoryAsync(path);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // DELETE: api/files?path=...
        // Deletes a file or directory at the specified path.
        [HttpDelete]
        public async Task<IActionResult> DeleteItem([FromQuery] string path)
        {
            // Validate the deletion request.
            if (string.IsNullOrEmpty(path) || !_fileSystemService.IsPathSafe(path))
                return BadRequest("Invalid deletion request");

            var result = await _fileSystemService.DeleteItemAsync(path);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // GET: api/files/search?query=...
        // Searches for files and folders matching the query.
        [HttpGet("search")]
        public async Task<IActionResult> SearchFilesAndFolders([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest("Search query cannot be empty.");
            }

            var results = await _fileSystemService.SearchFilesAndFoldersAsync(query);
            return Ok(results);
        }

        // GET: api/files/info?path=...
        // Retrieves detailed information about a file or folder.
        [HttpGet("info")]
        public async Task<IActionResult> GetFileOrFolderInfo([FromQuery] string path)
        {
            // Validate that the provided path is safe.
            if (string.IsNullOrEmpty(path) || !_fileSystemService.IsPathSafe(path))
            {
                return BadRequest("Invalid path");
            }

            var item = await _fileSystemService.GetFileOrFolderInfoAsync(path);
            if (item == null)
            {
                return NotFound("File or folder not found.");
            }

            return Ok(item);
        }
    }
}
