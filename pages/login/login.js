function togglePassword() {
    const pwd = document.getElementById('password');
    const icon = document.getElementById('togglePwdIcon');
    const eyeCircle = icon.querySelector('#eyeCircle');

    if (pwd.type === 'password') {
        pwd.type = 'text';
        eyeCircle.setAttribute('fill', '#00bfff');
    } else {
        pwd.type = 'password';
        eyeCircle.setAttribute('fill', 'none');
    }
}

// attach event listener instead of inline onclick
document.addEventListener('DOMContentLoaded', () => {
    const toggleBtn = document.getElementById('togglePwdIcon');
    toggleBtn.addEventListener('click', togglePassword);
});

// Bắt sự kiện khi user submit form login
document.querySelector(".login-form").addEventListener("submit", async (e) => {
    e.preventDefault(); // Ngăn reload trang

    // Lấy username và password từ input
    const username = document.getElementById("username").value;
    const password = document.getElementById("password").value;

    try {
        // Gửi request tới LoginHandler (API backend)
        const response = await fetch("/api/auth/login", {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({ username, password })
        });

        if (!response.ok) {
            alert("Login failed!");
            return;
        }

        // Nhận token trả về
        const data = await response.json();
        const token = data.token;

        // Lưu token vào localStorage để dùng cho các request sau
        localStorage.setItem("authToken", token);
        console.log("tokennnn: ", token);

        console.log("token added?: ", localStorage.getItem("authToken"));

        alert("Login successful!");
        // Redirect sang home page sau khi login thành công
        window.location.href = "home";

    } catch (error) {
        console.error("Error during login:", error);
        alert("Something went wrong!");
    }
});
