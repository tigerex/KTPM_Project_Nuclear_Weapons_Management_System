// profile.js (module)

function getToken(){
  return localStorage.getItem("authToken") || null;
}

// gọi /api/auth/me — ưu tiên header Authorization, fallback cookie server-side
async function fetchMe(){
  const token = getToken();
  const headers = {};
  if (token) headers.Authorization = `Bearer ${token}`;
  const res = await fetch("/api/auth/me", { headers });
  const text = await res.text();
  let json = null; try { json = text ? JSON.parse(text) : null; } catch {}
  if (!res.ok) {
    // nếu chưa đăng nhập, quay về /login
    window.location.href = "/login";
    throw new Error(json?.error || `${res.status} ${res.statusText}`);
  }
  return json;
}

function paint(me){
  console.log("fullname please: ", me.fullname);
  document.getElementById("fullName").textContent = me.fullname;
  document.getElementById("username").textContent = me.username;
  document.getElementById("clearance").textContent = `Clearance: ${me.clearance}`;
  document.getElementById("admin").textContent = me.is_admin ? "Admin" : "User";
  document.getElementById("admin").className = "badge " + (me.is_admin ? "badge-ok" : "");
  document.getElementById("userId").textContent = me.id;
  // avatar demo: hash theo username (tuỳ ý)
//   const seed = encodeURIComponent(me.username);
//   document.getElementById("avatar").src = `https://api.dicebear.com/7.x/identicon/svg?seed=${seed}`;
}

async function doLogout(){
  try{
    const token = getToken();
    const headers = {};
    if (token) headers.Authorization = `Bearer ${token}`;
    await fetch("/api/auth/logout", { method:"POST", headers });
  }catch{}
  localStorage.removeItem("access_token");
  window.location.href = "/login";
}

document.addEventListener("DOMContentLoaded", async () => {
  document.getElementById("logoutTop")?.addEventListener("click",(e)=>{e.preventDefault();doLogout();});
  document.getElementById("logoutBtn")?.addEventListener("click",(e)=>{e.preventDefault();doLogout();});

  try{
    const me = await fetchMe();
    paint(me);
  }catch(e){
    console.error(e);
  }
});
