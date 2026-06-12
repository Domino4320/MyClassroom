function toggleSidebar(forceOpen) {
    const sidebar = document.getElementById("sidebar");
    const backdrop = document.getElementById("sidebarBackdrop");
    if (!sidebar) return;

    const shouldOpen =
        typeof forceOpen === "boolean"
            ? forceOpen
            : !sidebar.classList.contains("open");

    sidebar.classList.toggle("open", shouldOpen);
    backdrop?.classList.toggle("visible", shouldOpen);
    document.body.classList.toggle("sidebar-open", shouldOpen);

    const burger = document.querySelector(".header .burger");
    if (burger) burger.setAttribute("aria-expanded", shouldOpen ? "true" : "false");
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

document.addEventListener("keydown", function (e) {
    if (e.key === "Escape") toggleSidebar(false);
});
