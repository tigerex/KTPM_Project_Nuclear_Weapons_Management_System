const mapDots = document.getElementById('map-dots');

// Automatically point to the same server where frontend is hosted
const SERVER_URL = `${window.location.protocol}//${window.location.hostname}:9999`;
console.log(SERVER_URL)

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

//Do cái background của mình không có theo một cái chuẩn nào hết nên là tui chỉnh công thức tính tạo độ một xíu dựa theo kho vũ khí ở nhà bè.
function latLngToXY(lat, lng, mapWidth, mapHeight) {
    const x = (lng + 155) * (mapWidth / 360); // long lệch 155px (gốc là 180 thì phải)
    const y = (106 - lat) * (mapHeight / 180); // lat lệch -106px (gốc này thì là -90)
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

        mapDots.appendChild(dot);
        mapDots.appendChild(details);
    });
}



// Initial load
(async () => {
    const storages = await fetchStorages();
    renderDots(storages);

    // Re-render on resize
    window.addEventListener('resize', () => renderDots(storages));
})();
