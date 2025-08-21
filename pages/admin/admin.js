// Admin.js - Fixed functionality for admin panel

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
        // Fixed: Check both IsAdmin and is_admin properties
        if (!currentUser.IsAdmin && !currentUser.is_admin && currentUser.Role !== 'Admin') {
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

async function addUser(userData) {
    try {
        // Map the frontend data to match the database schema
        const mappedData = {
            username: userData.username,
            password: userData.password,
            full_name: userData.fullname || userData.full_name,
            clearance_level: userData.clearance || userData.clearance_level,
            role: userData.role || 'User',
            country: userData.country || null,
            organization: userData.organization || null,
            is_admin: userData.is_admin || false
        };

        console.log('Sending user data:', mappedData);
        
        const response = await fetch(`${SERVER_URL}/api/admin/addUser`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                ...createAuthHeaders()
            },
            body: JSON.stringify(mappedData)
        });
        
        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.error || 'Failed to add user');
        }
        
        return await response.json();
    } catch (error) {
        console.error("Failed to add user:", error);
        throw error;
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
        // Map the frontend data to match the database schema
        const mappedData = {
            name: weaponData.name,
            type: weaponData.type,
            yield_megatons: weaponData.yield_megatons ? parseFloat(weaponData.yield_megatons) : null,
            range_km: weaponData.range_km ? parseInt(weaponData.range_km) : null,
            weight_kg: weaponData.weight_kg ? parseInt(weaponData.weight_kg) : null,
            status: weaponData.status || 'Prototype',
            country_of_origin: weaponData.country_of_origin || null,
            year_craeted: weaponData.year_craeted ? parseInt(weaponData.year_craeted) : null,
            notes: weaponData.notes || null
        };

        console.log('Sending weapon data:', mappedData);
        
        const response = await fetch(`${SERVER_URL}/api/weapons/add`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                ...createAuthHeaders()
            },
            body: JSON.stringify(mappedData)
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

async function fetchStorageInventory(storageId) {
    try {
        const response = await fetch(`${SERVER_URL}/api/inventory`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                ...createAuthHeaders()
            },
            body: JSON.stringify({ storage_id: storageId })
        });
        
        if (response.status === 404 || response.status === 204) {
            // Empty inventory
            return { StorageId: storageId, StorageName: null, Weapons: [] };
        }
        
        if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
        
        return await response.json();
    } catch (error) {
        console.error("Failed to fetch storage inventory:", error);
        return null;
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
        // Fixed: Use capital property names from API response
        li.textContent = `${user.Username} - Level ${user.ClearanceLevel || '[REDACTED]'}`;
        li.dataset.userId = user.Id; // Fixed: Use capital Id
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
        const name = weapon.name || weapon.Name;
        const type = weapon.type || weapon.Type;
        const status = weapon.status || weapon.Status || 'Unknown';
        const yield_mt = weapon.yield_megatons || weapon.YieldMegatons;
        
        // Create more informative display text
        let displayText = `${name} (${type})`;
        if (status) displayText += ` - ${status}`;
        
        li.textContent = displayText;
        li.dataset.weaponId = weapon.weapon_id || weapon.id || weapon.Id;
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
        const locationName = storage.location_name || storage.LocationName;
        const lat = storage.latitude || storage.Latitude || 0;
        const lng = storage.longitude || storage.Longitude || 0;
        li.textContent = `${locationName}`;
        li.dataset.storageId = storage.storage_id || storage.id || storage.Id;
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
            <p><strong>ID:</strong> ${user.Id}</p>
            <p><strong>Username:</strong> ${user.Username}</p>
            <p><strong>Full Name:</strong> ${user.Fullname || 'Not set'}</p>
            <p><strong>Clearance Level:</strong> ${user.ClearanceLevel || 'Not set'}</p>
            <p><strong>Admin:</strong> ${user.IsAdmin ? 'Yes' : 'No'}</p>
            <p><strong>Role:</strong> ${user.Role || 'User'}</p>
            <div class="modal-actions" style="margin-top: 20px;">
                <button id="closeUserDetails" class="add-btn">Close</button>
            </div>
        </div>
    `;
    
    modal.style.display = 'flex';
    
    // Event listeners
    setupModalCloseHandlers();
}

function showWeaponDetails(weapon) {
    const modal = document.getElementById('add-modal');
    const modalContent = modal.querySelector('.modal-content');
    
    const weaponId = weapon.weapon_id || weapon.id || weapon.Id;
    const weaponName = weapon.name || weapon.Name;
    const weaponType = weapon.type || weapon.Type;
    const yieldMt = weapon.yield_megatons || weapon.YieldMegatons;
    const rangeKm = weapon.range_km || weapon.RangeKm;
    const weightKg = weapon.weight_kg || weapon.WeightKg;
    const status = weapon.status || weapon.Status;
    const country = weapon.country_of_origin || weapon.CountryOfOrigin;
    const yearCreated = weapon.year_craeted || weapon.YearCraeted; // Note the typo in DB
    const notes = weapon.notes || weapon.Notes;
    
    modalContent.innerHTML = `
        <span class="close-btn">&times;</span>
        <div class="weapon-details">
            <h3>Weapon Details</h3>
            <p><strong>ID:</strong> ${weaponId}</p>
            <p><strong>Name:</strong> ${weaponName}</p>
            <p><strong>Type:</strong> ${weaponType}</p>
            <p><strong>Status:</strong> ${status || 'Not set'}</p>
            <p><strong>Yield:</strong> ${yieldMt ? yieldMt + ' Megatons' : 'Not set'}</p>
            <p><strong>Range:</strong> ${rangeKm ? rangeKm + ' KM' : 'Not set'}</p>
            <p><strong>Weight:</strong> ${weightKg ? weightKg + ' KG' : 'Not set'}</p>
            <p><strong>Country of Origin:</strong> ${country || 'Not set'}</p>
            <p><strong>Year Created:</strong> ${yearCreated || 'Not set'}</p>
            <p><strong>Notes:</strong> ${notes || 'None'}</p>
            <div class="modal-actions" style="margin-top: 20px;">
                <button id="deleteWeaponBtn" class="add-btn" style="background: linear-gradient(135deg, #ef4444 60%, #dc2626 100%);">Delete Weapon</button>
                <button id="closeWeaponDetails" class="add-btn">Close</button>
            </div>
        </div>
    `;
    
    modal.style.display = 'flex';
    
    // Setup event handlers
    setupModalCloseHandlers();
    
    const deleteBtn = document.getElementById('deleteWeaponBtn');
    if (deleteBtn) {
        deleteBtn.addEventListener('click', async () => {
            if (confirm('Are you sure you want to delete this weapon? This action cannot be undone.')) {
                try {
                    await deleteWeapon(weaponId);
                    alert('Weapon deleted successfully!');
                    closeModal();
                    await loadAllData(); // Refresh data
                } catch (error) {
                    alert(`Failed to delete weapon: ${error.message}`);
                }
            }
        });
    }
}

function showStorageDetails(storage) {
    const modal = document.getElementById('add-modal');
    const modalContent = modal.querySelector('.modal-content');
    
    const storageId = storage.storage_id || storage.id || storage.Id;
    const locationName = storage.location_name || storage.LocationName;
    const lat = storage.latitude || storage.Latitude || 0;
    const lng = storage.longitude || storage.Longitude || 0;
    
    modalContent.innerHTML = `
        <span class="close-btn">&times;</span>
        <div class="storage-details">
            <h3>${locationName}</h3>
            <p><strong>Storage ID:</strong> ${storageId}</p>
            <p><strong>Location:</strong> ${locationName}</p>
            <p><strong>Coordinates:</strong> ${lat}, ${lng}</p>
            <h4>Inventory:</h4>
            <div id="inventory-content">Loading inventory...</div>
        </div>
    `;
    
    modal.style.display = 'flex';
    
    // Setup close handler
    setupModalCloseHandlers();
    
    // Load inventory asynchronously
    loadStorageInventory(storageId, locationName);
}

async function loadStorageInventory(storageId, storageName) {
    try {
        const inventory = await fetchStorageInventory(storageId);
        const inventoryDiv = document.getElementById('inventory-content');
        
        if (!inventory || !Array.isArray(inventory.Weapons)) {
            inventoryDiv.innerHTML = `
                <p style="color: #9cc7ff; opacity: 0.8;">No weapons found in this storage.</p>
                <div class="modal-actions" style="margin-top: 20px;">
                    <button id="deleteStorageBtn" class="add-btn" style="background: linear-gradient(135deg, #ef4444 60%, #dc2626 100%);">Delete Storage</button>
                    <button id="closeStorageDetails" class="add-btn">Close</button>
                </div>
            `;
        } else {
            let weaponsHtml = '';
            if (inventory.Weapons.length > 0) {
                weaponsHtml = `
                    <div class="inventory-list">
                        ${inventory.Weapons.map(weapon => `
                            <div class="inventory-item" style="background: rgba(0, 191, 255, 0.1); padding: 8px 12px; margin: 6px 0; border-radius: 6px; border: 1px solid rgba(23, 75, 114, 0.5);">
                                <strong>${weapon.WeaponName}</strong> (${weapon.WeaponType})
                                <div style="color: #9cc7ff; font-size: 0.9rem;">Quantity: ${weapon.Quantity}</div>
                            </div>
                        `).join('')}
                    </div>
                `;
            } else {
                weaponsHtml = '<p style="color: #9cc7ff; opacity: 0.8;">No weapons in inventory.</p>';
            }
            
            inventoryDiv.innerHTML = `
                ${weaponsHtml}
                <div class="modal-actions" style="margin-top: 20px;">
                    <button id="deleteStorageBtn" class="add-btn" style="background: linear-gradient(135deg, #ef4444 60%, #dc2626 100%);">Delete Storage</button>
                    <button id="closeStorageDetails" class="add-btn">Close</button>
                </div>
            `;
        }
        
        // Setup action buttons
        setupStorageActionHandlers(storageId);
        
    } catch (error) {
        console.error("Failed to load inventory:", error);
        const inventoryDiv = document.getElementById('inventory-content');
        inventoryDiv.innerHTML = `
            <p style="color: #ef4444;">Failed to load inventory. Please try again.</p>
            <div class="modal-actions" style="margin-top: 20px;">
                <button id="retryInventoryBtn" class="add-btn">Retry</button>
                <button id="closeStorageDetails" class="add-btn">Close</button>
            </div>
        `;
        
        const retryBtn = document.getElementById('retryInventoryBtn');
        if (retryBtn) {
            retryBtn.addEventListener('click', () => {
                loadStorageInventory(storageId, storageName);
            });
        }
        
        const closeBtn = document.getElementById('closeStorageDetails');
        if (closeBtn) {
            closeBtn.addEventListener('click', closeModal);
        }
    }
}

function setupStorageActionHandlers(storageId) {
    const deleteBtn = document.getElementById('deleteStorageBtn');
    const closeBtn = document.getElementById('closeStorageDetails');
    
    if (deleteBtn) {
        deleteBtn.addEventListener('click', async () => {
            if (confirm('Are you sure you want to delete this storage? This action cannot be undone.')) {
                try {
                    await deleteStorage(storageId);
                    alert('Storage deleted successfully!');
                    closeModal();
                    await loadAllData();
                } catch (error) {
                    alert(`Failed to delete storage: ${error.message}`);
                }
            }
        });
    }
    
    if (closeBtn) {
        closeBtn.addEventListener('click', closeModal);
    }
}

// Modal functions
function closeModal() {
    const modal = document.getElementById('add-modal');
    modal.style.display = 'none';
}

function setupModalCloseHandlers() {
    const modal = document.getElementById('add-modal');
    const closeBtn = modal.querySelector('.close-btn');
    
    if (closeBtn) {
        closeBtn.addEventListener('click', closeModal);
    }
    
    // Setup any close detail buttons
    const closeDetailBtns = modal.querySelectorAll('#closeUserDetails, #closeWeaponDetails, #closeStorageDetails');
    closeDetailBtns.forEach(btn => {
        btn.addEventListener('click', closeModal);
    });
}

function showAddForm(type) {
    const modal = document.getElementById('add-modal');
    
    // Reset modal content to original forms
    modal.querySelector('.modal-content').innerHTML = `
        <span class="close-btn">&times;</span>
        <form id="add-user-form" class="add-popup-form" style="display:none;">
            <h3>Add User</h3>
            <input type="text" placeholder="Username" name="username" required>
            <input type="password" placeholder="Password" name="password" required>
            <input type="text" placeholder="Full Name" name="full_name" required>
            <input type="text" placeholder="Country (Optional)" name="country">
            <input type="text" placeholder="Organization (Optional)" name="organization">
            <select name="clearance_level" required style="width: 100%; margin-bottom: 1rem; padding: 12px 16px; border: 1px solid #174b72; border-radius: 8px; background: rgba(0, 191, 255, 0.08); color: #e6f2ff; font-size: 1rem; outline: none; font-family: inherit;">
                <option value="">Select Clearance Level</option>
                <option value="Low">Low</option>
                <option value="Medium">Medium</option>
                <option value="High">High</option>
                <option value="Ultra-Secret">Ultra-Secret</option>
            </select>
            <select name="role" required style="width: 100%; margin-bottom: 1rem; padding: 12px 16px; border: 1px solid #174b72; border-radius: 8px; background: rgba(0, 191, 255, 0.08); color: #e6f2ff; font-size: 1rem; outline: none; font-family: inherit;">
                <option value="">Select Role</option>
                <option value="User">User</option>
                <option value="Admin">Admin</option>
                <option value="Operator">Operator</option>
                <option value="Supervisor">Supervisor</option>
            </select>
            <label style="display: flex; align-items: center; color: #e6f2ff; margin-bottom: 1rem;">
                <input type="checkbox" name="is_admin" style="margin-right: 8px;">
                Admin Privileges
            </label>
            <button type="submit">Add User</button>
        </form>
        <form id="add-weapon-form" class="add-popup-form" style="display:none;">
            <h3>Add Weapon</h3>
            <input type="text" placeholder="Weapon Name" name="name" required>
            <input type="text" placeholder="Type (e.g., ICBM, SLBM, Tactical)" name="type" required>
            <input type="number" placeholder="Yield (Megatons)" name="yield_megatons" step="0.01" min="0">
            <input type="number" placeholder="Range (KM)" name="range_km" min="0">
            <input type="number" placeholder="Weight (KG)" name="weight_kg" min="0">
            <select name="status" required style="width: 100%; margin-bottom: 1rem; padding: 12px 16px; border: 1px solid #174b72; border-radius: 8px; background: rgba(0, 191, 255, 0.08); color: #e6f2ff; font-size: 1rem; outline: none; font-family: inherit;">
                <option value="">Select Status</option>
                <option value="Active">Active</option>
                <option value="Decommissioned">Decommissioned</option>
                <option value="Prototype">Prototype</option>
            </select>
            <input type="text" placeholder="Country of Origin" name="country_of_origin">
            <input type="number" placeholder="Year Created" name="year_craeted" min="1900" max="2100">
            <textarea placeholder="Notes (Optional)" name="notes" rows="3" style="width: 100%; margin-bottom: 1rem; padding: 12px 16px; border: 1px solid #174b72; border-radius: 8px; background: rgba(0, 191, 255, 0.08); color: #e6f2ff; font-size: 1rem; outline: none; font-family: inherit; resize: vertical;"></textarea>
            <button type="submit">Add Weapon</button>
        </form>
        <form id="add-storage-form" class="add-popup-form" style="display:none;">
            <h3>Add Weapon Storage</h3>
            <input type="text" placeholder="Storage Name" required>
            <input type="text" placeholder="Location" required>
            <input type="number" placeholder="Latitude" step="0.0001" required>
            <input type="number" placeholder="Longitude" step="0.0001" required>
            <button type="submit">Add Storage</button>
        </form>
    `;
    
    // Show specific form
    const targetForm = document.getElementById(`add-${type}-form`);
    if (targetForm) {
        targetForm.style.display = 'block';
        modal.style.display = 'flex';
        
        // Setup form handlers for this specific form
        setupSpecificFormHandler(type);
        setupModalCloseHandlers();
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
                    user.Username.toLowerCase().includes(query) ||
                    (user.Fullname && user.Fullname.toLowerCase().includes(query))
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
                const filtered = allWeapons.filter(weapon => {
                    const name = weapon.name || weapon.Name || '';
                    const type = weapon.type || weapon.Type || '';
                    return name.toLowerCase().includes(query) || type.toLowerCase().includes(query);
                });
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
                const filtered = allStorages.filter(storage => {
                    const locationName = storage.location_name || storage.LocationName || '';
                    return locationName.toLowerCase().includes(query);
                });
                renderStorages(filtered);
            } else {
                renderStorages(allStorages);
            }
        });
    }
}

// Fixed form submission handlers
function setupSpecificFormHandler(type) {
    if (type === 'user') {
        const form = document.getElementById('add-user-form');
        if (form) {
            form.addEventListener('submit', async (e) => {
                e.preventDefault();
                
                // Use FormData to get all form values properly
                const formData = new FormData(form);
                
                const userData = {
                    username: formData.get('username')?.trim(),
                    password: formData.get('password'),
                    full_name: formData.get('full_name')?.trim(),
                    country: formData.get('country')?.trim() || null,
                    organization: formData.get('organization')?.trim() || null,
                    clearance_level: formData.get('clearance_level'),
                    role: formData.get('role'),
                    is_admin: formData.get('is_admin') === 'on' // checkbox handling
                };
                
                // Basic validation
                if (!userData.username || !userData.password || !userData.full_name || 
                    !userData.clearance_level || !userData.role) {
                    alert('Please fill in all required fields');
                    return;
                }
                
                console.log('Form data being sent:', userData);
                
                try {
                    await addUser(userData);
                    alert('User added successfully!');
                    closeModal();
                    await loadAllData();
                    form.reset(); // Clear form
                } catch (error) {
                    console.error('Add user error:', error);
                    alert(`Failed to add user: ${error.message}`);
                }
            });
        }
    } else if (type === 'weapon') {
        const form = document.getElementById('add-weapon-form');
        if (form) {
            form.addEventListener('submit', async (e) => {
                e.preventDefault();
                
                // Use FormData to get all form values properly
                const formData = new FormData(form);
                
                const weaponData = {
                    name: formData.get('name')?.trim(),
                    type: formData.get('type')?.trim(),
                    yield_megatons: formData.get('yield_megatons')?.trim(),
                    range_km: formData.get('range_km')?.trim(),
                    weight_kg: formData.get('weight_kg')?.trim(),
                    status: formData.get('status'),
                    country_of_origin: formData.get('country_of_origin')?.trim(),
                    year_craeted: formData.get('year_craeted')?.trim(), // Note: keeping the typo to match DB
                    notes: formData.get('notes')?.trim()
                };
                
                // Basic validation - only name, type, and status are required
                if (!weaponData.name || !weaponData.type || !weaponData.status) {
                    alert('Please fill in all required fields (Name, Type, Status)');
                    return;
                }
                
                // Validate numeric fields if provided
                if (weaponData.yield_megatons && isNaN(parseFloat(weaponData.yield_megatons))) {
                    alert('Yield must be a valid number');
                    return;
                }
                
                if (weaponData.range_km && isNaN(parseInt(weaponData.range_km))) {
                    alert('Range must be a valid number');
                    return;
                }
                
                if (weaponData.weight_kg && isNaN(parseInt(weaponData.weight_kg))) {
                    alert('Weight must be a valid number');
                    return;
                }
                
                if (weaponData.year_craeted && (isNaN(parseInt(weaponData.year_craeted)) || 
                    parseInt(weaponData.year_craeted) < 1900 || parseInt(weaponData.year_craeted) > 2100)) {
                    alert('Year must be a valid year between 1900 and 2100');
                    return;
                }
                
                console.log('Weapon data being sent:', weaponData);
                
                try {
                    await addWeapon(weaponData);
                    alert('Weapon added successfully!');
                    closeModal();
                    await loadAllData();
                    form.reset(); // Clear form
                } catch (error) {
                    console.error('Add weapon error:', error);
                    alert(`Failed to add weapon: ${error.message}`);
                }
            });
        }
    } else if (type === 'storage') {
        const form = document.getElementById('add-storage-form');
        if (form) {
            form.addEventListener('submit', async (e) => {
                e.preventDefault();
                const inputs = e.target.querySelectorAll('input');
                
                const storageData = {
                    location_name: inputs[0].value.trim(),
                    location: inputs[1].value.trim(),
                    latitude: parseFloat(inputs[2].value),
                    longitude: parseFloat(inputs[3].value)
                };
                
                // Basic validation
                if (!storageData.location_name || !storageData.location || 
                    isNaN(storageData.latitude) || isNaN(storageData.longitude)) {
                    alert('Please fill in all required fields with valid data');
                    return;
                }
                
                try {
                    await addStorage(storageData);
                    alert('Storage added successfully!');
                    closeModal();
                    await loadAllData();
                    e.target.reset(); // Clear form
                } catch (error) {
                    alert(`Failed to add storage: ${error.message}`);
                }
            });
        }
    }
}

// Add button handlers
function setupAddButtons() {
    // User add button
    const userAddBtn = document.querySelector('#users .add-btn');
    if (userAddBtn) {
        userAddBtn.addEventListener('click', (e) => {
            e.preventDefault();
            showAddForm('user');
        });
    }
    
    // Weapon add button
    const weaponAddBtn = document.querySelector('#weapons .add-btn');
    if (weaponAddBtn) {
        weaponAddBtn.addEventListener('click', (e) => {
            e.preventDefault();
            showAddForm('weapon');
        });
    }
    
    // Storage add button
    const storageAddBtn = document.querySelector('#storage .add-btn');
    if (storageAddBtn) {
        storageAddBtn.addEventListener('click', (e) => {
            e.preventDefault();
            showAddForm('storage');
        });
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
        
        console.log('Data loaded successfully:', { 
            users: users.length, 
            weapons: weapons.length, 
            storages: storages.length 
        });
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
        setupAddButtons();
        
        // Setup global modal close handlers
        const modal = document.getElementById('add-modal');
        
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