const mapDots = document.getElementById('map-dots');

// Automatically point to the same server where frontend is hosted
const SERVER_URL = `${window.location.protocol}//${window.location.hostname}:9999`;
console.log(SERVER_URL)

// Global variable to store current user info
let currentUser = null;
function getToken(){
  return localStorage.getItem("authToken") || null;
}
/**
 * Fetch current user info from backend
 */
async function fetchCurrentUser() {
    try {
        const token = localStorage.getItem("authToken");
        const response = await fetch(`${SERVER_URL}/api/auth/me`, {
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        currentUser = await response.json();
        return currentUser;
    } catch (err) {
        console.error("❌ Failed to load current user:", err);
        return null;
    }
}

/**
 * Check if current user is admin
 */
function isAdmin() {
    return currentUser && (currentUser.role === 'Admin' || currentUser.is_admin === true);
}

/**
 * Update UI based on user role and user information
 */
function updateUIForUserRole() {
    const fab = document.querySelector('.fab');
    const usersNavLink = document.querySelector('.nav a[title="Users"]');
    
    // Update user profile information in dropdown
    updateUserProfile();
    
    if (isAdmin()) {
        // Show admin features
        fab.style.display = 'flex';
        if (usersNavLink) {
            usersNavLink.style.display = 'flex';
            // Make the Users link functional
            usersNavLink.href = '/admin';
        }
        
        // Update FAB click handler for admin
        fab.onclick = () => {
            openModal(`
            <h2>Add New Storage</h2>
            <div class="form-group">
                <label for="storageName">Name</label>
                <input type="text" id="storageName" placeholder="Enter storage name">
            </div>
            <div class="form-group">
                <label for="storageLat">Latitude</label>
                <input type="number" id="storageLat" step="0.0001" placeholder="e.g. 10.1234">
            </div>
            <div class="form-group">
                <label for="storageLng">Longitude</label>
                <input type="number" id="storageLng" step="0.0001" placeholder="e.g. 106.5678">
            </div>
            <button id="confirmAdd">✔️ Add Storage</button>
            `);


            document.getElementById("confirmAdd").onclick = async () => {
                const name = document.getElementById("storageName").value.trim();
                const lat = parseFloat(document.getElementById("storageLat").value);
                const lng = parseFloat(document.getElementById("storageLng").value);
                if (!name || isNaN(lat) || isNaN(lng)) {
                    alert("Please fill all fields correctly!");
                    return;
                }
                await addStorage(name, lat, lng);
                closeModal();
            };
        };
    } else {
        // Hide admin features for regular users
        fab.style.display = 'none';
        if (usersNavLink) usersNavLink.style.display = 'none';
    }
}

/**
 * Update user profile information in the dropdown
 */
function updateUserProfile() {
    const profileDropdown = document.querySelector('.profile-dropdown');
    if (!profileDropdown) return;
    
    if (!currentUser) {
        // Show error state if user info failed to load
        profileDropdown.innerHTML = `
            <div><strong>Error loading user</strong></div>
            <div>Please refresh</div>
            <hr>
            <a href="#" onclick="window.location.reload()">Refresh</a>
            <a href="#" onclick="doLogout()">Logout</a>
        `;
        return;
    }
    
    // Update the profile dropdown content with user info
    const displayName = currentUser.fullname || currentUser.username;
    const roleInfo = currentUser.role + (currentUser.clearance ? ` - Level ${currentUser.clearance}` : '');
    
    profileDropdown.innerHTML = `
        <div><strong>${displayName}</strong></div>
        <div>${roleInfo}</div>
        <hr>
        <a href="/profile">Profile</a>
        <a href="#">Settings</a>
        <a href="#" onclick="doLogout()">Logout</a>
    `;
}

/**
 * Add new storage (admin only)
 */
async function addStorage(locationName, latitude, longitude) {
    if (!isAdmin()) {
        alert("Access denied. Admin privileges required.");
        return;
    }
    
    try {
        const token = localStorage.getItem("authToken");
        const response = await fetch(`${SERVER_URL}/api/storages/add`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify({
                location_name: locationName,
                latitude: latitude,
                longitude: longitude
            })
        });
        
        if (response.ok) {
            alert("Storage added successfully!");
            // Refresh the storages on the map
            const storages = await fetchStorages();
            renderDots(storages);
        } else {
            const error = await response.json();
            alert(`Failed to add storage: ${error.error || 'Unknown error'}`);
        }
    } catch (err) {
        console.error("❌ Failed to add storage:", err);
        alert("Failed to add storage. Please try again.");
    }
}

/**
 * Fetch storage data from backend
 */
async function fetchStorages() {
    try {
        const response = await fetch(`${SERVER_URL}/api/storages/all`);
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        return await response.json(); // Expect backend to return JSON
    } catch (err) {
        console.error("❌ Failed to load storages:", err);
        return []; // fallback empty
    }
}

async function deleteStorage(storageId) {
    if (!confirm("⚠️ Are you sure you want to delete this storage? This action cannot be undone.")) {
        return;
    }

    try {
        const res = await fetch("http://192.168.2.11:9999/api/storages/delete", {
            method: "DELETE",
            headers: {
                "Content-Type": "application/json",
                // add auth headers if needed
            },
            body: JSON.stringify({ storage_id: storageId })
        });

        if (res.status === 204) {
            alert("✅ Storage deleted successfully!");
            closeModal();

            // Optional: refresh the map UI (remove the dot)
            const dot = document.querySelector(`[data-storage-id='${storageId}']`);
            if (dot) dot.remove();
        } else {
            const data = await res.json();
            alert("❌ Failed: " + (data.error || "unknown error"));
        }
    } catch (err) {
        console.error("Delete failed", err);
        alert("⚠️ Error deleting storage");
    }
}

async function fetchInventory(storageId) {
    try {
        const token = getToken();
        const response = await fetch(`${SERVER_URL}/api/inventory`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                ...(token ? { "Authorization": `Bearer ${token}` } : {})
            },
            body: JSON.stringify({ storage_id: storageId })
        });

        if (response.status === 404 || response.status === 204) {
            // represent as empty inventory; caller will use storage object for name
            return { StorageId: storageId, StorageName: null, Weapons: [] };
        }

        if (!response.ok) throw new Error("Failed to fetch inventory");
        return await response.json();
    } catch (err) {
        console.error("❌ Inventory fetch error:", err);
        return null;
    }
}

async function updateInventory(storageId, weapons) {
    try {
        const res = await fetch(`${SERVER_URL}/api/invenroty/edit`, {
            method: "PUT",
            headers: {
                "Content-Type": "application/json",
                // include auth headers if required
            },
            body: JSON.stringify({
                storage_id: storageId,
                weapons: weapons
            })
        });

        const data = await res.json();
        if (res.ok) {
            alert("✅ Inventory updated successfully!");
        } else {
            alert("❌ Failed: " + (data.error || "unknown error"));
        }
    } catch (err) {
        console.error("Update failed", err);
        alert("⚠️ Error updating inventory");
    }
}

async function fetchAllWeapons() {
    try {
        const token = getToken();
        const res = await fetch(`${SERVER_URL}/api/weapons/all`, {
            headers: {
                "Content-Type": "application/json",
                ...(token ? { "Authorization": `Bearer ${token}` } : {})
            }
        });
        if (!res.ok) throw new Error("Failed to fetch weapons");
        return await res.json();
    } catch (err) {
        console.error("❌ fetchAllWeapons error:", err);
        return [];
    }
}


//Do cái background của mình không có theo một cái chuẩn nào hết nên là tui chỉnh công thức tính tạo độ một xíu dựa theo kho vũ khí ở nhà bè.
function latLngToXY(lat, lng, mapWidth, mapHeight) {
    const x = (lng + 158) * (mapWidth / 360); // long lệch 155px (gốc là 180 thì phải)
    const y = (117 - lat) * (mapHeight / 180); // lat lệch -106px (gốc này thì là -90)
    return { x, y };
}

function showDetails(dot, details, x, y) {
    const tooltipWidth = 320;
    const tooltipHeight = 120;
    let left = x + 20;
    let top = y - 10;
    if (left + tooltipWidth > window.innerWidth) left = x - tooltipWidth - 20;
    if (top + tooltipHeight > window.innerHeight) top = window.innerHeight - tooltipHeight - 20;
    details.style.left = left + 'px';
    details.style.top = top + 'px';
    details.classList.add('show');
}

function hideDetails(dot) {
    let detailDiv = dot.nextElementSibling;
    detailDiv.classList.remove('show');
}

/**
 * Render dots dynamically from storage data
 */
function renderDots(storages) {
    const mapW = window.innerWidth;
    const mapH = window.innerHeight;
    mapDots.innerHTML = '';

    storages.forEach(storage => {
        const { x, y } = latLngToXY(storage.latitude, storage.longitude, mapW, mapH);

        const dot = document.createElement('div');
        dot.className = 'dot';
        dot.style.left = x + 'px';
        dot.style.top = y + 'px';
        dot.title = storage.location_name;
        dot.dataset.storageId = storage.storage_id;

        const detailText = `
            Location: ${storage.location_name}<br>
            Lat: ${storage.latitude.toFixed(4)}<br>
            Lng: ${storage.longitude.toFixed(4)}<br>
            Last Inspection: ${storage.last_inspection ? storage.last_inspection : "[REDACTED]"}
        `;

        const details = document.createElement('div');
        details.className = 'dot-details';
        details.innerHTML = `<strong>${storage.location_name}</strong><br>${detailText}`;

        dot.addEventListener('mouseenter', () => showDetails(dot, details, x, y));
        dot.addEventListener('mouseleave', () => hideDetails(dot));
        dot.addEventListener('click', async () => {
            const inv = await fetchInventory(storage.storage_id);

            // ---- Handle "no inventory" or failed fetch gracefully ----
            if (!inv || !Array.isArray(inv.Weapons)) {
                const sid = storage.storage_id;
                const title = storage.location_name || 'Unnamed Storage';

                let emptyHtml = `
                    <h2>${title}</h2>
                    <p><strong>Storage ID:</strong> ${sid}</p>
                    <p>No weapons found in this storage.</p>
                `;
                if (isAdmin()) {
                    emptyHtml += `
                                <div class="modal-actions">
                                <button id="addWeaponBtn">➕ Add Weapon</button>
                                <button id="saveInventoryBtn">💾 Save Changes</button>
                                <button id="deleteStorageBtn">🗑️ Delete Storage</button>
                                </div>
                                `               
                    ;
                }

                openModal(emptyHtml);

                if (isAdmin()) {
                    const delBtn = document.getElementById('deleteStorageBtn');
                    if (delBtn) {
                        delBtn.addEventListener('click', async () => {
                            await deleteStorage(sid);
                            closeModal();
                            // refresh the map (safer than manual DOM removal)
                            const storages = await fetchStorages();
                            renderDots(storages);
                        });
                    }
                }
                return; // IMPORTANT: only return AFTER wiring the delete button
            }

            // ---- Normal path: inventory exists (can be editable for Admin) ----
            let weaponsHtml = "";
            if (inv.Weapons.length > 0) {
                if (isAdmin()) {
                    weaponsHtml = `
                        <ul>
                            ${inv.Weapons.map(w => `
                                <li>
                                    <strong>${w.WeaponName}</strong> (${w.WeaponType}) —
                                    Qty: <input type="number" value="${w.Quantity}"
                                                min="0" data-weapon-id="${w.WeaponId}">
                                </li>
                            `).join("")}
                        </ul>
                    `;
                } else {
                    weaponsHtml = `
                        <ul>
                            ${inv.Weapons.map(w => `
                                <li>
                                    <strong>${w.WeaponName}</strong>
                                    (${w.WeaponType}) — Qty: ${w.Quantity}
                                </li>
                            `).join("")}
                        </ul>
                    `;
                }
            } else {
                weaponsHtml = `<p>No weapons found in this storage.</p>`;
            }

            let detailHtml = `
                <h2>${inv.StorageName || storage.location_name}</h2>
                <p><strong>Storage ID:</strong> ${inv.StorageId || storage.storage_id}</p>
                <h3>Inventory:</h3>
                ${weaponsHtml}
            `;

            if (isAdmin()) {
                detailHtml += `
                    <button id="addWeaponBtn">➕ Add Weapon</button>
                    <button id="saveInventoryBtn">💾 Save</button>
                    <button id="deleteStorageBtn">🗑️ Delete Storage</button>
                `;
            }

            openModal(detailHtml);

            if (isAdmin()) {
                const saveBtn = document.getElementById("saveInventoryBtn");
                if (saveBtn) {
                    saveBtn.addEventListener("click", async () => {
                        const inputs = document.querySelectorAll("input[data-weapon-id]");
                        const weapons = Array.from(inputs).map(input => ({
                            weapon_id: parseInt(input.dataset.weaponId),
                            quantity: parseInt(input.value)
                        }));
                        await updateInventory(inv.StorageId || storage.storage_id, weapons);
                    });
                }

                const delBtn = document.getElementById("deleteStorageBtn");
                if (delBtn) {
                    delBtn.addEventListener("click", async () => {
                        await deleteStorage(inv.StorageId || storage.storage_id);
                        closeModal();
                        const storages = await fetchStorages();
                        renderDots(storages);
                    });
                }

                const addBtn = document.getElementById("addWeaponBtn");
                if (addBtn) {
                    addBtn.addEventListener("click", async () => {
                        const allWeapons = await fetchAllWeapons();
                        if (!allWeapons.length) {
                            alert("⚠️ No weapons available to add");
                            return;
                        }

                        // Build dropdown + input
                        const selectorHtml = `
                            <label for="weaponSelect">Select Weapon:</label>
                            <select id="weaponSelect">
                                ${allWeapons.map(w => `
                                    <option value="${w.weapon_id}">${w.name} (${w.type})</option>
                                `).join("")}
                            </select>
                            <label for="weaponQty">Quantity:</label>
                            <input type="number" id="weaponQty" min="1" value="1">
                            <button id="confirmAddWeaponBtn">✔️ Add</button>
                        `;

                        // Append inside modal
                        const modalContent = document.querySelector(".modal-content");
                        const div = document.createElement("div");
                        div.innerHTML = selectorHtml;
                        modalContent.appendChild(div);

                        // Handle confirm add
                        document.getElementById("confirmAddWeaponBtn").addEventListener("click", async () => {
                            const weaponId = parseInt(document.getElementById("weaponSelect").value);
                            const qty = parseInt(document.getElementById("weaponQty").value);

                            if (isNaN(weaponId) || isNaN(qty) || qty < 1) {
                                alert("⚠️ Invalid input");
                                return;
                            }

                            // Merge with existing inputs (inventory)
                            const inputs = document.querySelectorAll("input[data-weapon-id]");
                            const weapons = Array.from(inputs).map(input => ({
                                weapon_id: parseInt(input.dataset.weaponId),
                                quantity: parseInt(input.value)
                            }));

                            // Add the new one
                            weapons.push({ weapon_id: weaponId, quantity: qty });

                            // Save back
                            await updateInventory(inv?.StorageId || storage.storage_id, weapons);

                            // Reload modal to reflect changes
                            const refreshed = await fetchInventory(storage.storage_id);
                            closeModal();
                            dot.click(); // reopen with updated data
                        });
                    });
                }

            }
        });
        mapDots.appendChild(dot);
        mapDots.appendChild(details);
    });
}

// Modal helpers
function openModal(contentHtml) {
    const overlay = document.getElementById("modal-overlay");
    const body = document.getElementById("modal-body");
    body.innerHTML = contentHtml;
    overlay.style.display = "flex";
}

function closeModal() {
    document.getElementById("modal-overlay").style.display = "none";
}

document.addEventListener("DOMContentLoaded", () => {
    document.querySelector(".modal-close").onclick = closeModal;
    document.getElementById("modal-overlay").onclick = (e) => {
        if (e.target.id === "modal-overlay") closeModal();
    };
});


/**
 * Delete storage (admin only)
 */
async function deleteStorage(storageId) {
    if (!isAdmin()) {
        alert("Access denied. Admin privileges required.");
        return;
    }
    
    try {
        const token = localStorage.getItem("authToken");
        const response = await fetch(`${SERVER_URL}/api/storages/delete`, {
            method: 'DELETE',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify({
                storage_id: storageId
            })
        });
        
        if (response.ok) {
            alert("Storage deleted successfully!");
            // Refresh the storages on the map
            const storages = await fetchStorages();
            renderDots(storages);
        } else {
            const error = await response.json();
            alert(`Failed to delete storage: ${error.error || 'Unknown error'}`);
        }
    } catch (err) {
        console.error("❌ Failed to delete storage:", err);
        alert("Failed to delete storage. Please try again.");
    }
}

// Initial load
(async () => {
    // First, get current user info
    await fetchCurrentUser();
    
    // Update UI based on user role
    updateUIForUserRole();
    
    // Then load storages
    const storages = await fetchStorages();
    renderDots(storages);

    // Re-render on resize
    window.addEventListener('resize', () => renderDots(storages));
})();

// ======================================= LOGOUT
// Hàm logout
async function doLogout(){
  try{
    const token = getToken();
    const headers = {};
    if (token) headers.Authorization = `Bearer ${token}`;
    await fetch("/api/auth/logout", { method:"POST", headers });
    localStorage.removeItem("authToken");
    window.location.href = "/login";
  }catch{}
  localStorage.removeItem("authToken");
  console.log("token deleted: ", localStorage.getItem("authToken"));
  window.location.href = "/login"; // Redirect về trang login
}
// Gắn sự kiện click cho link Logout
document.addEventListener("DOMContentLoaded", async () => {
  document.getElementById("logout")?.addEventListener("click",(e)=>{e.preventDefault();doLogout();});
});