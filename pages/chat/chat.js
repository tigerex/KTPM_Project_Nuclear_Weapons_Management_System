// Automatically point to the same server where frontend is hosted
const SERVER_URL = `${window.location.hostname}:9999`;
console.log(SERVER_URL);

const chatDiv = document.getElementById("chat");
const messageInput = document.getElementById("message");

// Identity (global for chat)
let username = localStorage.getItem("username") || "Guest";
let color = localStorage.getItem("color") || "#00bfff";


// chatProfile.js

function getToken() {
  return localStorage.getItem("authToken") || null;
}

// Fetch current user info
async function fetchMe() {
  const token = getToken();
  const headers = {};
  if (token) headers.Authorization = `Bearer ${token}`;
  const res = await fetch("/api/auth/me", { headers });
  const text = await res.text();
  let json = null;
  try { json = text ? JSON.parse(text) : null; } catch {}
  if (!res.ok) {
    window.location.href = "/login"; // redirect if not logged in
    throw new Error(json?.error || `${res.status} ${res.statusText}`);
  }
  return json;
}

// Apply user info to chat page
function paintChatUser(me) {
  // store in localStorage for WebSocket
  localStorage.setItem("username", me.fullname || me.username);
  localStorage.setItem("color", me.color || getRandomColor());

  // optionally show username somewhere
  const el = document.getElementById("chatUserName");
  if (el) el.textContent = me.fullname || me.username;
}

// fallback random color
function getRandomColor() {
  const colors = ["#00bfff","#ff6f61","#ffb74d","#7fff7f","#d39eff"];
  return colors[Math.floor(Math.random() * colors.length)];
}

// Auto-grow textarea
messageInput.addEventListener("input", function () {
  this.style.height = "auto";
  this.style.height = this.scrollHeight + "px";
});

function scrollToBottom() {
  chatDiv.scrollTo({ top: chatDiv.scrollHeight, behavior: 'smooth' });
}

// Enter to send, Ctrl+Enter for newline
messageInput.addEventListener("keydown", function (event) {
  if (event.key === "Enter" && !event.ctrlKey) {
    event.preventDefault();
    sendMessage();
  }
});

// Identity (simple localStorage fallback)
document.addEventListener("DOMContentLoaded", async () => {
  try {
    const me = await fetchMe();
    paintChatUser(me);

    // update global username/color after fetch
    username = localStorage.getItem("username");
    color = localStorage.getItem("color");
  } catch(e) {
    console.error("Failed to fetch user info", e);
  }
});



// Connect WebSocket
const socket = new WebSocket(`ws://${SERVER_URL}/ws/chat`);

socket.onopen = () => addSystemMessage("✅ Connected to chat.");
socket.onmessage = (event) => addMessage(event.data);
socket.onclose = () => addSystemMessage("❌ Disconnected from server.");

// Send message
function sendMessage() {
  if (!messageInput.value.trim()) return;
  const payload = JSON.stringify({ username, color, msg: messageInput.value });
  socket.send(payload);
  messageInput.value = "";
}

function logout() {
  localStorage.clear();
  window.location.href = "/login";
}

// Append chat/system messages
function addMessage(raw) {
  try {
    const msgObj = typeof raw === "string" ? JSON.parse(raw) : raw;
    renderMessage(msgObj);
  } catch {
    addSystemMessage(raw);
  }
}

function renderMessage(msg) {
  if (!msg.msg || msg.msg.trim() === "") return;

  const wrapper = document.createElement("div");
  wrapper.className = "chat-message";

  const avatar = document.createElement("div");
  avatar.className = "avatar";
  avatar.style.backgroundColor = msg.color || "#888";
  avatar.textContent = msg.username ? msg.username.charAt(0).toUpperCase() : "?";

  const content = document.createElement("div");
  content.className = "message-content";

  const header = document.createElement("div");
  header.className = "message-header";

  const nameSpan = document.createElement("span");
  nameSpan.className = "username";
  nameSpan.style.color = msg.color;
  nameSpan.textContent = msg.username;

  const timeSpan = document.createElement("span");
  timeSpan.className = "timestamp";
  timeSpan.textContent = new Date().toLocaleTimeString();

  header.appendChild(nameSpan);
  header.appendChild(timeSpan);

  const text = document.createElement("div");
  text.className = "message-text";
  text.textContent = msg.msg;

  content.appendChild(header);
  content.appendChild(text);
  wrapper.appendChild(avatar);
  wrapper.appendChild(content);

  chatDiv.appendChild(wrapper);
  scrollToBottom();
}

function addSystemMessage(text) {
  const wrapper = document.createElement("div");
  wrapper.className = "system-message";
  wrapper.textContent = text;
  chatDiv.appendChild(wrapper);
  scrollToBottom();
}
