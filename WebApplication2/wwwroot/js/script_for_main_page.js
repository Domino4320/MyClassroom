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
    if (file) {
        const reader = new FileReader();
        reader.onload = function (ev) {
            document.getElementById('headerAvatar').src = ev.target.result;
            document.getElementById('sidebarAvatar').src = ev.target.result;
        }
        reader.readAsDataURL(file);
    }
}); 

