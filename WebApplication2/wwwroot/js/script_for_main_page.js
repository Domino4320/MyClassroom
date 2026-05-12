function toggleSidebar() {
    const sidebar = document.getElementById("sidebar");
    const content = document.querySelector(".content-area");
    if (sidebar) sidebar.classList.toggle("open");
    if (content) content.classList.toggle("shift");
}

function chooseAvatar() {
    const input = document.getElementById("avatarInput");
    if (input) input.click();
}

const avatarInput = document.getElementById("avatarInput");
if (avatarInput) {
    avatarInput.addEventListener("change", function (e) {
        const file = e.target.files[0];
        if (!file) return;

        const formData = new FormData();
        formData.append("Avatar", file);

        fetch("/Home/UploadAvatar", {
            method: "POST",
            body: formData,
        })
            .then((response) => response.json())
            .then((data) => {
                if (data.avatar) {
                    const cacheBuster = "?v=" + new Date().getTime();
                    const newSrc = data.avatar + cacheBuster;
                    const headerImg = document.getElementById("headerAvatar");
                    if (headerImg) headerImg.src = newSrc;
                    const sidebarImg = document.getElementById("sidebarAvatar");
                    if (sidebarImg) sidebarImg.src = newSrc;
                } else if (data.error) {
                    alert("Ошибка: " + data.error);
                }
            })
            .catch(() => {
                alert("Не удалось загрузить аватар.");
            });
    });
}
