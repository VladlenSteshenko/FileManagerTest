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

        public FilesController(IFileSystemService fileSystemService)
        {
            _fileSystemService = fileSystemService;
        }

        // get basic info
        [HttpGet("rootInfo")]
        public async Task<IActionResult> GetRootFolderContents()
        {
            // Get the absolute root path using GetFullPath with an empty relative path
            string absolutePath = _fileSystemService.GetFullPath("");

            // Retrieve the directory content for the root folder
            var content = await _fileSystemService.GetDirectoryContentsAsync("");

            return Ok(new { AbsolutePath = absolutePath, Content = content });
        }

        // Get directory contents
        [HttpGet("directory")]
        public async Task<IActionResult> GetDirectoryContents([FromQuery] string path = "")
        {
            if (!_fileSystemService.IsPathSafe(path))
                return BadRequest("Invalid path");

            var contents = await _fileSystemService.GetDirectoryContentsAsync(path);
            return Ok(contents);
        }

        // Download a file
        [HttpGet("download")]
        public async Task<IActionResult> DownloadFile([FromQuery] string path)
        {
            if (string.IsNullOrEmpty(path) || !_fileSystemService.IsPathSafe(path))
                return BadRequest("Invalid file path");

            var (fileContents, contentType, fileName) = await _fileSystemService.GetFileAsync(path);
            return File(fileContents, contentType, fileName);
        }

        // Upload a file
        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile([FromQuery] string path, IFormFile file)
        {
            if (file == null || string.IsNullOrEmpty(path) || !_fileSystemService.IsPathSafe(path))
                return BadRequest("Invalid upload request");

            var result = await _fileSystemService.UploadFileAsync(path, file);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("upload-root")]
        public async Task<IActionResult> UploadFileToRoot(IFormFile file)
        {
            if (file == null)
                return BadRequest("Invalid upload request");

            var result = await _fileSystemService.UploadFileAsync(string.Empty, file);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // Create a new directory
        [HttpPost("directory")]
        public async Task<IActionResult> CreateDirectory([FromQuery] string path)
        {
            if (string.IsNullOrEmpty(path) || !_fileSystemService.IsPathSafe(path))
                return BadRequest("Invalid directory path");

            var result = await _fileSystemService.CreateDirectoryAsync(path);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // Delete a file or directory
        [HttpDelete]
        public async Task<IActionResult> DeleteItem([FromQuery] string path)
        {
            if (string.IsNullOrEmpty(path) || !_fileSystemService.IsPathSafe(path))
                return BadRequest("Invalid deletion request");

            var result = await _fileSystemService.DeleteItemAsync(path);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // Search Route
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

        // Get file or folder info
        [HttpGet("info")]
        public async Task<IActionResult> GetFileOrFolderInfo([FromQuery] string path)
        {
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

