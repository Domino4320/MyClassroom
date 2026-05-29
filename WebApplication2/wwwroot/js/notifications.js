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

    function getToken() {
        return document.querySelector('input[name="__RequestVerificationToken"]')?.value;
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

    async function deleteNotification(id, token) {
        return postForm("/Notifications/Delete", { id, __RequestVerificationToken: token });
    }

    function buildDropdownItem(n, token) {
        const wrap = document.createElement("div");
        wrap.className = n.isRead ? "notif-dd-item" : "notif-dd-item notif-dd-item--unread";
        wrap.dataset.notifId = n.id;

        const link = document.createElement("a");
        link.className = "notif-dd-link";
        link.href = n.url || "/Notifications/Index";
        link.innerHTML = `
            <div class="notif-dd-title">${(n.title || "").toString()}</div>
            ${n.body ? `<div class="notif-dd-body">${(n.body || "").toString()}</div>` : ""}
            <div class="notif-dd-meta">${formatTime(n.createdAt)}</div>
        `;
        link.addEventListener("click", async () => {
            if (!n.isRead && token) {
                try {
                    await postForm("/Notifications/MarkRead", { id: n.id, __RequestVerificationToken: token });
                    await refreshBell();
                } catch { /* ignore */ }
            }
        });

        const delBtn = document.createElement("button");
        delBtn.type = "button";
        delBtn.className = "notif-dd-delete";
        delBtn.title = "Удалить";
        delBtn.innerHTML = '<i class="fa-solid fa-xmark"></i>';
        delBtn.addEventListener("click", async (e) => {
            e.preventDefault();
            e.stopPropagation();
            if (!token) return;
            const ok = confirm("Удалить это уведомление?");
            if (!ok) return;
            const result = await deleteNotification(n.id, token);
            if (result?.success) {
                wrap.remove();
                await refreshBell();
                const list = document.getElementById("notifDropdownList");
                const empty = document.getElementById("notifDropdownEmpty");
                if (list && !list.children.length && empty) empty.style.display = "block";
            }
        });

        wrap.appendChild(link);
        wrap.appendChild(delBtn);
        return wrap;
    }

    async function loadDropdown() {
        const list = document.getElementById("notifDropdownList");
        const empty = document.getElementById("notifDropdownEmpty");
        if (!list) return;

        list.innerHTML = "";
        const res = await fetch("/Notifications/Latest", { method: "GET" });
        const data = await res.json();
        const items = data.items || [];
        const token = getToken();

        if (!items.length) {
            if (empty) empty.style.display = "block";
            return;
        }

        if (empty) empty.style.display = "none";
        items.forEach((n) => list.appendChild(buildDropdownItem(n, token)));
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
                const token = getToken();
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
        const token = getToken();
        const list = document.getElementById("notifList");

        const markAll = document.getElementById("markAllReadBtn");
        if (markAll && token) {
            markAll.addEventListener("click", async () => {
                await postForm("/Notifications/MarkAllRead", { __RequestVerificationToken: token });
                location.reload();
            });
        }

        const deleteAll = document.getElementById("deleteAllNotifBtn");
        if (deleteAll && token) {
            deleteAll.addEventListener("click", async () => {
                if (!confirm("Удалить все уведомления? Это действие нельзя отменить.")) return;
                await postForm("/Notifications/DeleteAll", { __RequestVerificationToken: token });
                location.reload();
            });
        }

        if (!list || !token) return;

        list.addEventListener("click", async (e) => {
            const btn = e.target.closest("[data-action]");
            if (!btn) return;
            const id = btn.getAttribute("data-id");
            const action = btn.getAttribute("data-action");
            if (!id) return;

            if (action === "read") {
                await postForm("/Notifications/MarkRead", { id, __RequestVerificationToken: token });
                const card = list.querySelector(`[data-notif-id="${id}"]`);
                card?.classList.remove("notif-card--unread");
                btn.remove();
                await refreshBell();
                return;
            }

            if (action === "delete") {
                if (!confirm("Удалить это уведомление?")) return;
                const result = await deleteNotification(id, token);
                if (result?.success) {
                    list.querySelector(`[data-notif-id="${id}"]`)?.remove();
                    await refreshBell();
                    if (!list.children.length) location.reload();
                }
            }
        });
    }

    window.initNotificationsPage = initNotificationsPage;
    window.initHeaderNotifications = initHeaderDropdown;
})();
