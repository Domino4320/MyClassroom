function toggleSidebar() {
    const sidebar = document.getElementById('sidebar');
    const main = document.getElementById('mainContent');
    // Проверь, чтобы ID соответствовали твоей верстке
    if (sidebar) sidebar.classList.toggle('open');
    if (main) main.classList.toggle('shift');
}

function chooseAvatar() {
    document.getElementById('avatarInput').click();
}

document.getElementById('avatarInput').addEventListener('change', function (e) {
    const file = e.target.files[0];
    if (!file) return;

    const formData = new FormData();
    formData.append("Avatar", file);

    fetch("/Home/UploadAvatar", {
        method: "POST",
        body: formData
    })
        .then(response => response.json())
        .then(data => {
            if (data.avatar) {
                // Добавляем уникальный параметр, чтобы обмануть кеш браузера
                const cacheBuster = "?v=" + new Date().getTime();
                const newSrc = data.avatar + cacheBuster;

                // Обновляем аватар в шапке
                const headerImg = document.getElementById("headerAvatar");
                if (headerImg) headerImg.src = newSrc;

                // Обновляем аватар в сайдбаре
                const sidebarImg = document.getElementById("sidebarAvatar");
                if (sidebarImg) sidebarImg.src = newSrc;

                console.log("Аватар успешно обновлен!");
            } else if (data.error) {
                alert("Ошибка: " + data.error);
            }
        })
        .catch(err => {
            console.error("Ошибка при загрузке:", err);
            alert("Не удалось загрузить аватар.");
        });
});