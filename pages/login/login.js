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
