// app.js - Main JavaScript file for the File Manager App
// This script handles UI interactions, API calls, and dynamic rendering
// of the file explorer, search functionality, file uploads, folder creation,
// and modal operations.

// Global variable to hold the current directory data
let currentDirectoryData = null;

// Wait for the DOM to fully load before running scripts
document.addEventListener('DOMContentLoaded', () => {
    // -------------------------
    // Modal Initialization
    // -------------------------
    const modal = document.getElementById('modal');
    const closeButton = document.querySelector('.close-button');

    // Close modal when the close button is clicked
    closeButton.addEventListener('click', () => {
        closeModal();
    });

    // Close modal when clicking outside the modal content
    window.addEventListener('click', (event) => {
        if (event.target === modal) {
            closeModal();
        }
    });

    // -------------------------
    // Navigation and Hash Change
    // -------------------------
    // Update the file explorer when the URL hash changes (directory navigation)
    window.addEventListener('hashchange', loadDirectory);

    // -------------------------
    // Search Functionality
    // -------------------------
    document.getElementById('searchBtn').addEventListener('click', () => {
        const query = document.getElementById('searchInput').value.trim();
        if (query) {
            performSearch(query);
        }
    });

    // Clear search input and reload directory when clear button is clicked
    document.getElementById('clearSearchBtn').addEventListener('click', () => {
        document.getElementById('searchInput').value = '';
        loadDirectory();
    });

    // -------------------------
    // File Upload Functionality
    // -------------------------
    document.getElementById('uploadBtn').addEventListener('click', () => {
        const fileInput = document.getElementById('fileInput');
        const file = fileInput.files[0];
        if (!file) {
            alert("Please select a file to upload.");
            return;
        }
        let currentPath = decodeURIComponent(location.hash.substring(1));

        const formData = new FormData();
        formData.append("file", file);

        // Check if the current path is root
        const isRootDirectory = currentPath === "" || currentPath === "#";

        // Choose the proper endpoint based on the current directory
        const endpoint = isRootDirectory ?
            '/api/files/upload-root' :
            '/api/files/upload?path=' + encodeURIComponent(currentPath);

        // Send the file to the server via a POST request
        fetch(endpoint, {
            method: 'POST',
            body: formData
        })
            .then(response => {
                if (!response.ok) {
                    return response.json().then(error => Promise.reject(error));
                }
                return response.json();
            })
            .then(result => {
                alert(result.message);
                loadDirectory(); // Refresh the directory view
                fileInput.value = ""; // Clear the file input
            })
            .catch(error => {
                console.error("Upload error:", error);
                alert("Error uploading file: " + (error.message || "Unknown error"));
            });
    });

    // -------------------------
    // Create Folder Functionality
    // -------------------------
    document.getElementById('createFolderBtn').addEventListener('click', () => {
        const folderNameInput = document.getElementById('folderNameInput');
        const folderName = folderNameInput.value.trim();
        if (!folderName) {
            alert("Please enter a folder name.");
            return;
        }
        let currentPath = decodeURIComponent(location.hash.substring(1));
        // Determine proper separator based on current path
        const separator = currentPath ? (currentPath.includes('\\') ? '\\' : '/') : '';
        const newFolderPath = currentPath ? (currentPath + separator + folderName) : folderName;

        // Send request to create a new folder
        fetch('/api/files/directory?path=' + encodeURIComponent(newFolderPath), {
            method: 'POST'
        })
            .then(response => {
                if (!response.ok) {
                    return response.json().then(error => Promise.reject(error));
                }
                return response.json();
            })
            .then(result => {
                alert(result.message);
                loadDirectory(); // Refresh directory view
                folderNameInput.value = ""; // Clear the folder name input
            })
            .catch(error => {
                alert("Folder creation failed: " + error.message);
            });
    });

    // -------------------------
    // Initial Directory Load
    // -------------------------
    loadDirectory();
});

// -------------------------
// Directory and Search Functions
// -------------------------

// Load the directory based on the current URL hash
function loadDirectory() {
    let path = decodeURIComponent(location.hash.substring(1));
    fetchDirectory(path);
}

// Fetch the directory content from the API
function fetchDirectory(path) {
    const apiUrl = '/api/files/directory?path=' + encodeURIComponent(path);
    fetch(apiUrl)
        .then(response => response.json())
        .then(data => {
            currentDirectoryData = data;
            renderDirectory(data);
        })
        .catch(error => {
            document.getElementById('explorer').innerText = 'Error: ' + error;
        });
}

// Perform a search query using the API
function performSearch(query) {
    const apiUrl = '/api/files/search?query=' + encodeURIComponent(query);
    fetch(apiUrl)
        .then(response => response.json())
        .then(data => renderSearchResults(data))
        .catch(error => {
            document.getElementById('explorer').innerText = 'Error: ' + error;
        });
}

// Render search results in the explorer area
function renderSearchResults(items) {
    const explorer = document.getElementById('explorer');
    explorer.innerHTML = '';
    // Update breadcrumb to show that these are search results
    document.getElementById('breadcrumb').innerHTML = `<span>Search Results</span>`;
    items.forEach(item => {
        const listItem = document.createElement('div');
        listItem.className = 'list-item';

        // Create and append icon based on whether it's a directory or file
        const icon = document.createElement('div');
        icon.className = 'icon';
        icon.innerText = item.isDirectory ? '📁' : '📄';
        listItem.appendChild(icon);

        // Append the item name
        const name = document.createElement('div');
        name.className = 'name';
        name.innerText = item.name;
        listItem.appendChild(name);

        // Directory navigation or file download
        if (item.isDirectory) {
            listItem.addEventListener('click', () => {
                location.hash = encodeURIComponent(item.path);
                document.getElementById('searchInput').value = '';
            });
        } else {
            // Download button for files
            const downloadBtn = document.createElement('button');
            downloadBtn.className = 'download-btn';
            downloadBtn.innerText = '⬇️';
            downloadBtn.addEventListener('click', (e) => {
                e.stopPropagation();
                downloadFile(item.path);
            });
            listItem.appendChild(downloadBtn);
        }

        // Delete button for both files and folders
        const deleteBtn = document.createElement('button');
        deleteBtn.className = 'delete-btn';
        deleteBtn.innerText = '🗑️';
        deleteBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            deleteFileOrFolder(item.path, item.isDirectory);
        });
        listItem.appendChild(deleteBtn);

        // Info button to view details in a modal
        const infoBtn = document.createElement('button');
        infoBtn.className = 'info-btn';
        infoBtn.innerText = 'ℹ️';
        infoBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            getFileInfo(item.path);
        });
        listItem.appendChild(infoBtn);

        // Append the list item to the explorer container
        explorer.appendChild(listItem);
    });
}

// Render the directory view (folders and files)
function renderDirectory(data) {
    const explorer = document.getElementById('explorer');
    explorer.innerHTML = '';

    // Update breadcrumb navigation
    const breadcrumb = document.getElementById('breadcrumb');
    breadcrumb.innerHTML = generateBreadcrumb(data.currentPath);

    // If not at the root directory, add a "Back" button to navigate to parent folder
    if (data.currentPath && data.currentPath !== "") {
        let parentPath = data.parentPath;
        if (!parentPath) {
            const separator = data.currentPath.includes('\\') ? '\\' : '/';
            const parts = data.currentPath.split(separator);
            parts.pop();
            parentPath = parts.join(separator);
        }
        if (typeof parentPath !== 'undefined') {
            const backBtn = document.createElement('button');
            backBtn.className = 'back-btn';
            backBtn.innerText = 'Back';
            backBtn.addEventListener('click', () => {
                location.hash = encodeURIComponent(parentPath);
            });
            explorer.appendChild(backBtn);
        }
    }

    // Render each item (file/folder) in the directory
    data.items.forEach(item => {
        const listItem = document.createElement('div');
        listItem.className = 'list-item';

        // Create icon for item
        const icon = document.createElement('div');
        icon.className = 'icon';
        icon.innerText = item.isDirectory ? '📁' : '📄';
        listItem.appendChild(icon);

        // Append the name of the item
        const name = document.createElement('div');
        name.className = 'name';
        name.innerText = item.name;
        listItem.appendChild(name);

        // Directory navigation or file download
        if (item.isDirectory) {
            listItem.addEventListener('click', () => {
                location.hash = encodeURIComponent(item.path);
            });
        } else {
            // Download button for files
            const downloadBtn = document.createElement('button');
            downloadBtn.className = 'download-btn';
            downloadBtn.innerText = '⬇️';
            downloadBtn.addEventListener('click', (e) => {
                e.stopPropagation();
                downloadFile(item.path);
            });
            listItem.appendChild(downloadBtn);
        }

        // Delete button for both files and folders
        const deleteBtn = document.createElement('button');
        deleteBtn.className = 'delete-btn';
        deleteBtn.innerText = '🗑️';
        deleteBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            deleteFileOrFolder(item.path, item.isDirectory);
        });
        listItem.appendChild(deleteBtn);

        // Info button to display file/folder information in a modal popup
        const infoBtn = document.createElement('button');
        infoBtn.className = 'info-btn';
        infoBtn.innerText = 'ℹ️';
        infoBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            getFileInfo(item.path);
        });
        listItem.appendChild(infoBtn);

        // Append the list item to the explorer container
        explorer.appendChild(listItem);
    });
}

// -------------------------
// File Operations
// -------------------------

// Delete a file or folder after confirmation
function deleteFileOrFolder(path, isDirectory) {
    const itemType = isDirectory ? 'folder' : 'file';
    const confirmMessage = `Are you sure you want to delete this ${itemType}?`;

    // Confirm deletion with the user
    if (!confirm(confirmMessage)) {
        return;
    }

    const apiUrl = `/api/files?path=${encodeURIComponent(path)}`;

    fetch(apiUrl, {
        method: 'DELETE'
    })
        .then(response => {
            if (!response.ok) {
                return response.json().then(error => Promise.reject(error));
            }
            return response.json();
        })
        .then(result => {
            if (result.success) {
                alert(result.message);
                loadDirectory(); // Refresh directory view to reflect changes
            } else {
                alert(`Failed to delete: ${result.message}`);
            }
        })
        .catch(error => {
            console.error("Delete error:", error);
            alert("Error deleting item: " + (error.message || "Unknown error"));
        });
}

// -------------------------
// Modal Functions
// -------------------------

// Open the modal and display content
function openModal(content) {
    const modal = document.getElementById('modal');
    const modalBody = document.getElementById('modal-body');
    modalBody.innerHTML = content;
    modal.style.display = 'block';
}

// Close the modal
function closeModal() {
    const modal = document.getElementById('modal');
    modal.style.display = 'none';
}

// Fetch file/folder information and display it in a modal
function getFileInfo(filePath) {
    const apiUrl = `/api/files/info?path=${encodeURIComponent(filePath)}`;

    fetch(apiUrl)
        .then(response => {
            if (!response.ok) {
                return response.json().then(error => Promise.reject(error));
            }
            return response.json();
        })
        .then(data => {
            const content = `
                <h2>File Info</h2>
                <p><strong>Name:</strong> ${data.name}</p>
                <p><strong>Path:</strong> ${data.path}</p>
                <p><strong>Type:</strong> ${data.isDirectory ? 'Directory' : 'File'}</p>
                <p><strong>Size:</strong> ${data.size} bytes</p>
                <p><strong>Last Modified:</strong> ${new Date(data.lastModified).toLocaleString()}</p>
                <p><strong>Content Type:</strong> ${data.contentType}</p>
            `;
            openModal(content);
        })
        .catch(error => {
            openModal(`<p>Failed to get info: ${error.message || 'Unknown error'}</p>`);
        });
}

// Download a file by creating a temporary link and triggering a click
function downloadFile(filePath) {
    const apiUrl = `/api/files/download?path=${encodeURIComponent(filePath)}`;

    fetch(apiUrl)
        .then(response => {
            if (!response.ok) {
                return response.text().then(error => Promise.reject(error));
            }
            return response.blob(); // Retrieve file as a Blob
        })
        .then(blob => {
            const url = window.URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.style.display = 'none';
            a.href = url;
            a.download = filePath.split('/').pop(); // Extract the file name
            document.body.appendChild(a);
            a.click();
            window.URL.revokeObjectURL(url); // Cleanup the object URL
            document.body.removeChild(a);
        })
        .catch(error => {
            openModal(`<p>Failed to download file: ${error}</p>`);
        });
}

// -------------------------
// Breadcrumb Navigation
// -------------------------

// Generate breadcrumb HTML based on the current path
function generateBreadcrumb(currentPath) {
    let breadcrumbHTML = `<a href="#" onclick="navigateToRoot()">Root</a>`;
    if (currentPath) {
        const separator = currentPath.includes('\\') ? '\\' : '/';
        const parts = currentPath.split(separator);
        let pathSoFar = '';
        parts.forEach((part, index) => {
            pathSoFar = index === 0 ? part : pathSoFar + separator + part;
            breadcrumbHTML += ` / <a href="#${encodeURIComponent(pathSoFar)}">${part}</a>`;
        });
    }
    return breadcrumbHTML;
}

// Navigate back to the root directory
function navigateToRoot() {
    location.hash = '';
}
