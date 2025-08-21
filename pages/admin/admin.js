// Admin.js - Complete functionality for admin panel

// Configuration
const SERVER_URL = `${window.location.protocol}//${window.location.hostname}:9999`;

// Global variables
let currentUser = null;
let allUsers = [];
let allWeapons = [];
let allStorages = [];

// Utility functions
function getToken() {
    return localStorage.getItem("authToken") || null;
}

function createAuthHeaders() {
    const token = getToken();
    return token ? { 'Authorization': `Bearer ${token}` } : {};
}

// Authentication check
async function checkAuth() {
    try {
        const response = await fetch(`${SERVER_URL}/api/auth/me`, {
            headers: createAuthHeaders()
        });
        
        if (!response.ok) {
            window.location.href = '/login';
            return false;
        }
        
        currentUser = await response.json();
        if (!currentUser.is_admin && currentUser.role !== 'Admin') {
            alert("Access denied. Admin privileges required.");
            window.location.href = '/home';
            return false;
        }
        
        return true;
    } catch (error) {
        console.error("Auth check failed:", error);
        window.location.href = '/login';
        return false;
    }
}

// API Functions

// User Management
async function fetchAllUsers() {
    try {
        const response = await fetch(`${SERVER_URL}/api/admin/allUser`, {
            headers: {
                'Content-Type': 'application/json',
                ...createAuthHeaders()
            }
        });
        
        if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
        
        allUsers = await response.json();
        return allUsers;
    } catch (error) {
        console.error("Failed to fetch users:", error);
        return [];
    }
}

async function getUserById(userId) {
    try {
        const response = await fetch(`${SERVER_URL}/api/admin/getUser`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                ...createAuthHeaders()
            },
            body: JSON.stringify({ user_id: userId })
        });
        
        if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
        
        return await response.json();
    } catch (error) {
        console.error("Failed to fetch user:", error);
        return null;
    }
}

// Weapon Management
async function fetchAllWeapons() {
    try {
        const response = await fetch(`${SERVER_URL}/api/weapons/all`, {
            headers: {
                'Content-Type': 'application/json',
                ...createAuthHeaders()
            }
        });
        
        if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
        
        allWeapons = await response.json();
        return allWeapons;
    } catch (error) {
        console.error("Failed to fetch weapons:", error);
        return [];
    }
}

async function addWeapon(weaponData) {
    try {
        const response = await fetch(`${SERVER_URL}/api/weapons/add`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                ...createAuthHeaders()
            },
            body: JSON.stringify(weaponData)
        });
        
        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.error || 'Failed to add weapon');
        }
        
        return await response.json();
    } catch (error) {
        console.error("Failed to add weapon:", error);
        throw error;
    }
}

async function deleteWeapon(weaponId) {
    try {
        const response = await fetch(`${SERVER_URL}/api/weapons/delete`, {
            method: 'DELETE',
            headers: {
                'Content-Type': 'application/json',
                ...createAuthHeaders()
            },
            body: JSON.stringify({ weapon_id: weaponId })
        });
        
        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.error || 'Failed to delete weapon');
        }
        
        return true;
    } catch (error) {
        console.error("Failed to delete weapon:", error);
        throw error;
    }
}

// Storage Management
async function fetchAllStorages() {
    try {
        const response = await fetch(`${SERVER_URL}/api/storages/all`, {
            headers: {
                'Content-Type': 'application/json',
                ...createAuthHeaders()
            }
        });
        
        if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
        
        allStorages = await response.json();
        return allStorages;
    } catch (error) {
        console.error("Failed to fetch storages:", error);
        return [];
    }
}

async function addStorage(storageData) {
    try {
        const response = await fetch(`${SERVER_URL}/api/storages/add`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                ...createAuthHeaders()
            },
            body: JSON.stringify(storageData)
        });
        
        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.error || 'Failed to add storage');
        }
        
        return await response.json();
    } catch (error) {
        console.error("Failed to add storage:", error);
        throw error;
    }
}

async function deleteStorage(storageId) {
    try {
        const response = await fetch(`${SERVER_URL}/api/storages/delete`, {
            method: 'DELETE',
            headers: {
                'Content-Type': 'application/json',
                ...createAuthHeaders()
            },
            body: JSON.stringify({ storage_id: storageId })
        });
        
        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.error || 'Failed to delete storage');
        }
        
        return true;
    } catch (error) {
        console.error("Failed to delete storage:", error);
        throw error;
    }
}

// UI Rendering Functions
function renderUsers(users = allUsers) {
    const userList = document.getElementById('user-list');
    if (!userList) return;
    
    userList.innerHTML = '';
    
    users.forEach(user => {
        const li = document.createElement('li');
        li.textContent = `${user.username} (${user.fullname || 'No name'}) - Level ${user.clearance || 'N/A'}`;
        li.dataset.userId = user.id;
        li.addEventListener('click', () => showUserDetails(user));
        userList.appendChild(li);
    });
    
    if (users.length === 0) {
        const li = document.createElement('li');
        li.textContent = 'No users found';
        li.style.opacity = '0.6';
        userList.appendChild(li);
    }
}

function renderWeapons(weapons = allWeapons) {
    const weaponList = document.getElementById('weapon-list');
    if (!weaponList) return;
    
    weaponList.innerHTML = '';
    
    weapons.forEach(weapon => {
        const li = document.createElement('li');
        li.textContent = `${weapon.name} (${weapon.type})`;
        li.dataset.weaponId = weapon.weapon_id || weapon.id;
        li.addEventListener('click', () => showWeaponDetails(weapon));
        weaponList.appendChild(li);
    });
    
    if (weapons.length === 0) {
        const li = document.createElement('li');
        li.textContent = 'No weapons found';
        li.style.opacity = '0.6';
        weaponList.appendChild(li);
    }
}

function renderStorages(storages = allStorages) {
    const storageList = document.getElementById('storage-list');
    if (!storageList) return;
    
    storageList.innerHTML = '';
    
    storages.forEach(storage => {
        const li = document.createElement('li');
        li.textContent = `${storage.location_name} (${storage.latitude.toFixed(4)}, ${storage.longitude.toFixed(4)})`;
        li.dataset.storageId = storage.storage_id || storage.id;
        li.addEventListener('click', () => showStorageDetails(storage));
        storageList.appendChild(li);
    });
    
    if (storages.length === 0) {
        const li = document.createElement('li');
        li.textContent = 'No storages found';
        li.style.opacity = '0.6';
        storageList.appendChild(li);
    }
}

// Detail view functions
function showUserDetails(user) {
    const modal = document.getElementById('add-modal');
    const modalContent = modal.querySelector('.modal-content');
    
    modalContent.innerHTML = `
        <span class="close-btn">&times;</span>
        <div class="user-details">
            <h3>User Details</h3>
            <p><strong>ID:</strong> ${user.id}</p>
            <p><strong>Username:</strong> ${user.username}</p>
            <p><strong>Full Name:</strong> ${user.fullname || 'Not set'}</p>
            <p><strong>Clearance Level:</strong> ${user.clearance || 'Not set'}</p>
            <p><strong>Admin:</strong> ${user.is_admin ? 'Yes' : 'No'}</p>
            <p><strong>Role:</strong> ${user.role || 'User'}</p>
            <div class="modal-actions" style="margin-top: 20px;">
                <button id="closeUserDetails" class="add-btn">Close</button>
            </div>
        </div>
    `;
    
    modal.style.display = 'flex';
    
    // Event listeners
    modalContent.querySelector('.close-btn').addEventListener('click', closeModal);
    document.getElementById('closeUserDetails').addEventListener('click', closeModal);
}

function showWeaponDetails(weapon) {
    const modal = document.getElementById('add-modal');
    const modalContent = modal.querySelector('.modal-content');
    
    modalContent.innerHTML = `
        <span class="close-btn">&times;</span>
        <div class="weapon-details">
            <h3>Weapon Details</h3>
            <p><strong>ID:</strong> ${weapon.weapon_id || weapon.id}</p>
            <p><strong>Name:</strong> ${weapon.name}</p>
            <p><strong>Type:</strong> ${weapon.type}</p>
            <p><strong>Classification:</strong> ${weapon.classification || 'Not set'}</p>
            <div class="modal-actions" style="margin-top: 20px;">
                <button id="deleteWeaponBtn" class="add-btn" style="background: linear-gradient(135deg, #ef4444 60%, #dc2626 100%);">Delete Weapon</button>
                <button id="closeWeaponDetails" class="add-btn">Close</button>
            </div>
        </div>
    `;
    
    modal.style.display = 'flex';
    
    // Event listeners
    modalContent.querySelector('.close-btn').addEventListener('click', closeModal);
    document.getElementById('closeWeaponDetails').addEventListener('click', closeModal);
    
    document.getElementById('deleteWeaponBtn').addEventListener('click', async () => {
        if (confirm('Are you sure you want to delete this weapon? This action cannot be undone.')) {
            try {
                await deleteWeapon(weapon.weapon_id || weapon.id);
                alert('Weapon deleted successfully!');
                closeModal();
                await loadAllData(); // Refresh data
            } catch (error) {
                alert(`Failed to delete weapon: ${error.message}`);
            }
        }
    });
}

function showStorageDetails(storage) {
    const modal = document.getElementById('add-modal');
    const modalContent = modal.querySelector('.modal-content');
    
    modalContent.innerHTML = `
        <span class="close-btn">&times;</span>
        <div class="storage-details">
            <h3>Storage Details</h3>
            <p><strong>ID:</strong> ${storage.storage_id || storage.id}</p>
            <p><strong>Location:</strong> ${storage.location_name}</p>
            <p><strong>Latitude:</strong> ${storage.latitude}</p>
            <p><strong>Longitude:</strong> ${storage.longitude}</p>
            <p><strong>Last Inspection:</strong> ${storage.last_inspection || 'Never'}</p>
            <div class="modal-actions" style="margin-top: 20px;">
                <button id="deleteStorageBtn" class="add-btn" style="background: linear-gradient(135deg, #ef4444 60%, #dc2626 100%);">Delete Storage</button>
                <button id="closeStorageDetails" class="add-btn">Close</button>
            </div>
        </div>
    `;
    
    modal.style.display = 'flex';
    
    // Event listeners
    modalContent.querySelector('.close-btn').addEventListener('click', closeModal);
    document.getElementById('closeStorageDetails').addEventListener('click', closeModal);
    
    document.getElementById('deleteStorageBtn').addEventListener('click', async () => {
        if (confirm('Are you sure you want to delete this storage? This action cannot be undone.')) {
            try {
                await deleteStorage(storage.storage_id || storage.id);
                alert('Storage deleted successfully!');
                closeModal();
                await loadAllData(); // Refresh data
            } catch (error) {
                alert(`Failed to delete storage: ${error.message}`);
            }
        }
    });
}

// Modal functions
function closeModal() {
    const modal = document.getElementById('add-modal');
    modal.style.display = 'none';
}

function showAddForm(type) {
    const modal = document.getElementById('add-modal');
    const forms = modal.querySelectorAll('.add-popup-form');
    
    // Hide all forms
    forms.forEach(form => form.style.display = 'none');
    
    // Show specific form
    const targetForm = document.getElementById(`add-${type}-form`);
    if (targetForm) {
        targetForm.style.display = 'block';
        modal.style.display = 'flex';
    }
}

// Search functions
function setupSearchFunctionality() {
    // User search
    const userSearchForm = document.querySelector('#users .search-form');
    if (userSearchForm) {
        userSearchForm.addEventListener('submit', (e) => {
            e.preventDefault();
            const query = e.target.querySelector('input').value.toLowerCase().trim();
            if (query) {
                const filtered = allUsers.filter(user => 
                    user.username.toLowerCase().includes(query) ||
                    (user.fullname && user.fullname.toLowerCase().includes(query))
                );
                renderUsers(filtered);
            } else {
                renderUsers(allUsers);
            }
        });
    }
    
    // Weapon search
    const weaponSearchForm = document.querySelector('#weapons .search-form');
    if (weaponSearchForm) {
        weaponSearchForm.addEventListener('submit', (e) => {
            e.preventDefault();
            const query = e.target.querySelector('input').value.toLowerCase().trim();
            if (query) {
                const filtered = allWeapons.filter(weapon => 
                    weapon.name.toLowerCase().includes(query) ||
                    weapon.type.toLowerCase().includes(query)
                );
                renderWeapons(filtered);
            } else {
                renderWeapons(allWeapons);
            }
        });
    }
    
    // Storage search
    const storageSearchForm = document.querySelector('#storage .search-form');
    if (storageSearchForm) {
        storageSearchForm.addEventListener('submit', (e) => {
            e.preventDefault();
            const query = e.target.querySelector('input').value.toLowerCase().trim();
            if (query) {
                const filtered = allStorages.filter(storage => 
                    storage.location_name.toLowerCase().includes(query)
                );
                renderStorages(filtered);
            } else {
                renderStorages(allStorages);
            }
        });
    }
}

// Form submission handlers
function setupFormHandlers() {
    // Add weapon form
    const addWeaponForm = document.getElementById('add-weapon-form');
    if (addWeaponForm) {
        addWeaponForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            const formData = new FormData(e.target);
            const weaponData = {
                name: formData.get('name') || e.target.querySelector('input[placeholder="Weapon Name"]').value,
                type: formData.get('type') || e.target.querySelector('input[placeholder="Type"]').value,
                classification: formData.get('classification') || e.target.querySelector('input[placeholder="Classification"]').value
            };
            
            try {
                await addWeapon(weaponData);
                alert('Weapon added successfully!');
                closeModal();
                await loadAllData();
            } catch (error) {
                alert(`Failed to add weapon: ${error.message}`);
            }
        });
    }
    
    // Add storage form
    const addStorageForm = document.getElementById('add-storage-form');
    if (addStorageForm) {
        addStorageForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            const inputs = e.target.querySelectorAll('input');
            const storageData = {
                location_name: inputs[0].value,
                location: inputs[1].value,
                latitude: parseFloat(inputs[2].value),
                longitude: parseFloat(inputs[3].value)
            };
            
            try {
                await addStorage(storageData);
                alert('Storage added successfully!');
                closeModal();
                await loadAllData();
            } catch (error) {
                alert(`Failed to add storage: ${error.message}`);
            }
        });
    }
}

// Add button handlers
function setupAddButtons() {
    // User add button
    const userAddBtn = document.querySelector('#users .add-btn');
    if (userAddBtn) {
        userAddBtn.addEventListener('click', () => showAddForm('user'));
    }
    
    // Weapon add button
    const weaponAddBtn = document.querySelector('#weapons .add-btn');
    if (weaponAddBtn) {
        weaponAddBtn.addEventListener('click', () => showAddForm('weapon'));
    }
    
    // Storage add button
    const storageAddBtn = document.querySelector('#storage .add-btn');
    if (storageAddBtn) {
        storageAddBtn.addEventListener('click', () => showAddForm('storage'));
    }
}

// Load all data function
async function loadAllData() {
    try {
        // Show loading states
        const lists = document.querySelectorAll('.item-list');
        lists.forEach(list => {
            list.innerHTML = '<li style="opacity: 0.6;">Loading...</li>';
        });
        
        // Load data in parallel
        const [users, weapons, storages] = await Promise.all([
            fetchAllUsers(),
            fetchAllWeapons(),
            fetchAllStorages()
        ]);
        
        // Render data
        renderUsers(users);
        renderWeapons(weapons);
        renderStorages(storages);
        
        console.log('Data loaded successfully:', { users: users.length, weapons: weapons.length, storages: storages.length });
    } catch (error) {
        console.error('Failed to load data:', error);
        alert('Failed to load data. Please refresh the page.');
    }
}

// Initialize the admin panel
async function initializeAdmin() {
    try {
        // Check authentication first
        const isAuthenticated = await checkAuth();
        if (!isAuthenticated) return;
        
        // Setup event handlers
        setupSearchFunctionality();
        setupFormHandlers();
        setupAddButtons();
        
        // Setup modal close handlers
        const modal = document.getElementById('add-modal');
        const closeBtn = modal.querySelector('.close-btn');
        if (closeBtn) {
            closeBtn.addEventListener('click', closeModal);
        }
        
        // Close modal when clicking outside
        modal.addEventListener('click', (e) => {
            if (e.target === modal) closeModal();
        });
        
        // Load all data
        await loadAllData();
        
        console.log('Admin panel initialized successfully');
    } catch (error) {
        console.error('Failed to initialize admin panel:', error);
        alert('Failed to initialize admin panel. Please refresh the page.');
    }
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', initializeAdmin);