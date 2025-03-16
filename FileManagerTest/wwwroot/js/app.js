let currentDirectoryData = null;

document.addEventListener('DOMContentLoaded', () => {
    // Modal initialization
    const modal = document.getElementById('modal');
    const closeButton = document.querySelector('.close-button');
    closeButton.addEventListener('click', () => {
        closeModal();
    });
    // Close modal when clicking outside the modal content
    window.addEventListener('click', (event) => {
        if (event.target === modal) {
            closeModal();
        }
    });

    // Handle navigation changes
    window.addEventListener('hashchange', loadDirectory);

    // Search functionality
    document.getElementById('searchBtn').addEventListener('click', () => {
        const query = document.getElementById('searchInput').value.trim();
        if (query) {
            performSearch(query);
        }
    });

    document.getElementById('clearSearchBtn').addEventListener('click', () => {
        document.getElementById('searchInput').value = '';
        loadDirectory();
    });

    // File Upload functionality
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

        // Explicitly check if currentPath is empty
        const isRootDirectory = currentPath === "" || currentPath === "#";

        const endpoint = isRootDirectory ?
            '/api/files/upload-root' :
            '/api/files/upload?path=' + encodeURIComponent(currentPath);

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
                loadDirectory();
                fileInput.value = "";
            })
            .catch(error => {
                console.error("Upload error:", error);
                alert("Error uploading file: " + (error.message || "Unknown error"));
            });
    });

    // Create Folder functionality
    document.getElementById('createFolderBtn').addEventListener('click', () => {
        const folderNameInput = document.getElementById('folderNameInput');
        const folderName = folderNameInput.value.trim();
        if (!folderName) {
            alert("Please enter a folder name.");
            return;
        }
        let currentPath = decodeURIComponent(location.hash.substring(1));
        // Determine the proper separator if a current path exists
        const separator = currentPath ? (currentPath.includes('\\') ? '\\' : '/') : '';
        const newFolderPath = currentPath ? (currentPath + separator + folderName) : folderName;
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
                loadDirectory();
                folderNameInput.value = "";
            })
            .catch(error => {
                alert("Folder creation failed: " + error.message);
            });
    });

    // Initial load of the directory view
    loadDirectory();
});

function loadDirectory() {
    let path = decodeURIComponent(location.hash.substring(1));
    fetchDirectory(path);
}

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

function performSearch(query) {
    const apiUrl = '/api/files/search?query=' + encodeURIComponent(query);
    fetch(apiUrl)
        .then(response => response.json())
        .then(data => renderSearchResults(data))
        .catch(error => {
            document.getElementById('explorer').innerText = 'Error: ' + error;
        });
}

function renderSearchResults(items) {
    const explorer = document.getElementById('explorer');
    explorer.innerHTML = '';
    // Update breadcrumb for search results
    document.getElementById('breadcrumb').innerHTML = `<span>Search Results</span>`;
    items.forEach(item => {
        const listItem = document.createElement('div');
        listItem.className = 'list-item';
        const icon = document.createElement('div');
        icon.className = 'icon';
        icon.innerText = item.isDirectory ? '📁' : '📄';
        listItem.appendChild(icon);
        const name = document.createElement('div');
        name.className = 'name';
        name.innerText = item.name;
        listItem.appendChild(name);
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

        // Delete button for files and folders
        const deleteBtn = document.createElement('button');
        deleteBtn.className = 'delete-btn';
        deleteBtn.innerText = '🗑️';
        deleteBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            deleteFileOrFolder(item.path, item.isDirectory);
        });
        listItem.appendChild(deleteBtn);

        // Info button
        const infoBtn = document.createElement('button');
        infoBtn.className = 'info-btn';
        infoBtn.innerText = 'ℹ️';
        infoBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            getFileInfo(item.path);
        });
        listItem.appendChild(infoBtn);

        explorer.appendChild(listItem);
    });
}
function renderDirectory(data) {
    const explorer = document.getElementById('explorer');
    explorer.innerHTML = '';
    const breadcrumb = document.getElementById('breadcrumb');
    breadcrumb.innerHTML = generateBreadcrumb(data.currentPath);

    // Add back button if not in root
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

    // Render directory items
    data.items.forEach(item => {
        const listItem = document.createElement('div');
        listItem.className = 'list-item';

        const icon = document.createElement('div');
        icon.className = 'icon';
        icon.innerText = item.isDirectory ? '📁' : '📄';
        listItem.appendChild(icon);

        const name = document.createElement('div');
        name.className = 'name';
        name.innerText = item.name;
        listItem.appendChild(name);

        // Navigate to folder on click
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

        // Delete button for files and folders
        const deleteBtn = document.createElement('button');
        deleteBtn.className = 'delete-btn';
        deleteBtn.innerText = '🗑️';
        deleteBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            deleteFileOrFolder(item.path, item.isDirectory);
        });
        listItem.appendChild(deleteBtn);

        // Info button to show file/folder info in a modal
        const infoBtn = document.createElement('button');
        infoBtn.className = 'info-btn';
        infoBtn.innerText = 'ℹ️';
        infoBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            getFileInfo(item.path);
        });
        listItem.appendChild(infoBtn);

        explorer.appendChild(listItem);
    });
}

function deleteFileOrFolder(path, isDirectory) {
    // Confirm deletion
    const itemType = isDirectory ? 'folder' : 'file';
    const confirmMessage = `Are you sure you want to delete this ${itemType}?`;

    if (!confirm(confirmMessage)) {
        return; // User cancelled the operation
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
                // Reload the current directory to reflect changes
                loadDirectory();
            } else {
                alert(`Failed to delete: ${result.message}`);
            }
        })
        .catch(error => {
            console.error("Delete error:", error);
            alert("Error deleting item: " + (error.message || "Unknown error"));
        });
}

// Opens the modal with the provided content
function openModal(content) {
    const modal = document.getElementById('modal');
    const modalBody = document.getElementById('modal-body');
    modalBody.innerHTML = content;
    modal.style.display = 'block';
}

// Closes the modal
function closeModal() {
    const modal = document.getElementById('modal');
    modal.style.display = 'none';
}

// getFileInfo now displays information in a modal popup
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

function downloadFile(filePath) {
    const apiUrl = `/api/files/download?path=${encodeURIComponent(filePath)}`;

    fetch(apiUrl)
        .then(response => {
            if (!response.ok) {
                return response.text().then(error => Promise.reject(error));
            }
            return response.blob(); // Get file content as a Blob
        })
        .then(blob => {
            const url = window.URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.style.display = 'none';
            a.href = url;
            a.download = filePath.split('/').pop(); // Extract the filename
            document.body.appendChild(a);
            a.click();
            window.URL.revokeObjectURL(url); // Clean up the object URL
            document.body.removeChild(a);
        })
        .catch(error => {
            openModal(`<p>Failed to download file: ${error}</p>`);
        });
}

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

function navigateToRoot() {
    location.hash = '';
}


