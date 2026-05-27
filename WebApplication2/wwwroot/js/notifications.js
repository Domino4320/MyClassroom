(function () {
    async function postForm(url, params) {
        const body = new URLSearchParams();
        Object.entries(params).forEach(([k, v]) => body.append(k, String(v)));
        const token = params.__RequestVerificationToken;
        const res = await fetch(url, {
            method: "POST",
            headers: {
                "Content-Type": "application/x-www-form-urlencoded",
                "RequestVerificationToken": token
            },
            body
        });
        return await res.json();
    }

    function formatTime(iso) {
        try {
            const d = new Date(iso);
            return d.toLocaleString();
        } catch {
            return "";
        }
    }

    async function refreshBell() {
        const badge = document.getElementById("notifBadge");
        if (!badge) return;
        const res = await fetch("/Notifications/UnreadCount", { method: "GET" });
        const data = await res.json();
        const count = Number(data.count || 0);
        badge.style.display = count > 0 ? "inline-flex" : "none";
        badge.textContent = count > 99 ? "99+" : String(count);
    }

    async function loadDropdown() {
        const list = document.getElementById("notifDropdownList");
        const empty = document.getElementById("notifDropdownEmpty");
        if (!list) return;

        list.innerHTML = "";
        const res = await fetch("/Notifications/Latest", { method: "GET" });
        const data = await res.json();
        const items = data.items || [];

        if (!items.length) {
            if (empty) empty.style.display = "block";
            return;
        }

        if (empty) empty.style.display = "none";
        items.forEach((n) => {
            const a = document.createElement("a");
            a.className = n.isRead ? "notif-dd-item" : "notif-dd-item notif-dd-item--unread";
            a.href = n.url || "/Notifications/Index";
            a.dataset.notifId = n.id;
            a.innerHTML = `
                <div class="notif-dd-title">${(n.title || "").toString()}</div>
                ${n.body ? `<div class="notif-dd-body">${(n.body || "").toString()}</div>` : ""}
                <div class="notif-dd-meta">${formatTime(n.createdAt)}</div>
            `;
            a.addEventListener("click", async () => {
                const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
                if (!token) return;
                try {
                    await postForm("/Notifications/MarkRead", { id: n.id, __RequestVerificationToken: token });
                    await refreshBell();
                } catch {
                    // ignore
                }
            });
            list.appendChild(a);
        });
    }

    function initHeaderDropdown() {
        const btn = document.getElementById("notifBellBtn");
        const dropdown = document.getElementById("notifDropdown");
        const markAllBtn = document.getElementById("notifMarkAllBtn");
        if (!btn || !dropdown) return;

        function close() {
            dropdown.classList.remove("open");
        }

        btn.addEventListener("click", async (e) => {
            e.stopPropagation();
            const opening = !dropdown.classList.contains("open");
            if (opening) {
                dropdown.classList.add("open");
                await loadDropdown();
            } else {
                close();
            }
        });

        document.addEventListener("click", (e) => {
            if (!dropdown.contains(e.target) && e.target !== btn) close();
        });

        if (markAllBtn) {
            markAllBtn.addEventListener("click", async (e) => {
                e.preventDefault();
                const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
                if (!token) return;
                await postForm("/Notifications/MarkAllRead", { __RequestVerificationToken: token });
                await refreshBell();
                await loadDropdown();
            });
        }

        refreshBell();
        setInterval(refreshBell, 20000);
    }

    function initNotificationsPage() {
        const markAll = document.getElementById("markAllReadBtn");
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        if (markAll && token) {
            markAll.addEventListener("click", async () => {
                await postForm("/Notifications/MarkAllRead", { __RequestVerificationToken: token });
                location.reload();
            });
        }
    }

    window.initNotificationsPage = initNotificationsPage;
    window.initHeaderNotifications = initHeaderDropdown;
})();

