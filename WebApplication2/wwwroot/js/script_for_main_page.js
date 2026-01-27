function toggleSidebar() {
    const sidebar = document.getElementById('sidebar');
    const main = document.getElementById('mainContent');
    sidebar.classList.toggle('open');
    main.classList.toggle('shift');
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
                document.getElementById("headerAvatar").src = data.avatar;
                document.getElementById("sidebarAvatar").src = data.avatar;
            }
        })
        .catch(err => console.error(err));
}); 

