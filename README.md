# File Manager App

## Overview
This is a minimalist and intuitive File Manager App built using plain HTML, CSS, and JavaScript for the frontend and ASP.NET Core for the backend. The app allows users to:
- Search for files and folders.
- Upload files.
- Create new folders.
- View file/folder details via a modal.
- Download and delete files/folders.

## Project Structure

### Frontend
- index.html: Main HTML file for the frontend.
- css/style.css: CSS file containing styles for the app.
- js/app.js: JavaScript file handling UI interactions and API calls.

### Backend
-Controllers/FilesController.cs:
Contains API endpoints for file operations (upload, download, delete, search, etc.). It handles requests such as retrieving directory contents, file downloads, file uploads, directory creation, deletion, search, and information retrieval, validating paths using the file system service.
-Helpers/PathHelper.cs:
Provides methods to sanitize and validate file paths, preventing directory traversal and protecting system directories. It ensures that users cannot access system-critical directories by cleaning the path input.
-Services/FileSystemService.cs:
Implements the logic for managing files and directories by leveraging PathHelper for safety. It manages file uploads, downloads, directory creation, deletion, and searching through the file system.
-Services/IFileSystemService.cs:
Defines the interface for file system operations, ensuring a clean separation of concerns between the implementation and its usage.
-Program.cs:
Configures and runs the ASP.NET Core application. It registers necessary services, sets up the HTTP request pipeline (serving static files, HTTPS redirection, API controllers, etc.), and maps API endpoints.

## Features
- Search Functionality: Search for files or folders by name.
- File Upload: Upload files to the current directory.
- Create Folder: Create new folders in the current directory.
- File/Folder Actions: Download, delete, and view information (via a modal) for each item.
- Breadcrumb Navigation: Easily navigate through directory paths.
- Responsive Modal: Enhanced modal with animations for a smooth user experience.

## Setup Instructions
1. Frontend Setup
   - Serve the frontend files using a local web server (e.g., using Live Server extension or a simple HTTP server).
   - Open index.html in your browser.
2. Backend Setup
   - Open the solution in Visual Studio or your preferred IDE.
   - Configure any necessary settings in appsettings.json (if applicable).
   - Run the ASP.NET Core application.
3. Ensure the frontend is configured to point to the correct backend API endpoints.

## Author
Vladlen Steshenko
