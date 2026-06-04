(function () {
    "use strict";

    function closeOnEscape(handler) {
        document.addEventListener("keydown", e => {
            if (e.key === "Escape") handler();
        });
    }

    function initDrawer(opts) {
        const { panelSelector, backdropSelector, openBtnSelector, closeBtnSelector, bodyClass } = opts;
        const panel = document.querySelector(panelSelector);
        const backdrop = document.querySelector(backdropSelector);
        if (!panel) return;

        const setOpen = open => {
            panel.classList.toggle("is-open", open);
            backdrop?.classList.toggle("is-open", open);
            document.body.classList.toggle(bodyClass || "drawer-open", open);
        };

        const toggle = () => setOpen(!panel.classList.contains("is-open"));
        const close = () => setOpen(false);

        document.querySelectorAll(openBtnSelector).forEach(btn => {
            btn.addEventListener("click", e => {
                e.preventDefault();
                toggle();
            });
        });

        document.querySelectorAll(closeBtnSelector || ".js-drawer-close").forEach(btn => {
            btn.addEventListener("click", e => {
                e.preventDefault();
                close();
            });
        });

        backdrop?.addEventListener("click", close);

        panel.querySelectorAll("a[href]").forEach(link => {
            link.addEventListener("click", () => {
                if (window.matchMedia("(max-width: 1023px)").matches) close();
            });
        });

        closeOnEscape(() => {
            if (panel.classList.contains("is-open")) close();
        });

        window.addEventListener("resize", () => {
            if (window.matchMedia("(min-width: 1024px)").matches) close();
        });

        return { open: () => setOpen(true), close, toggle };
    }

    document.addEventListener("DOMContentLoaded", () => {
        if (document.body.classList.contains("learn-page")) {
            initDrawer({
                panelSelector: ".learn-sidebar",
                backdropSelector: ".learn-drawer-backdrop",
                openBtnSelector: ".js-learn-nav-toggle",
                bodyClass: "learn-drawer-open"
            });
        }

        if (document.body.classList.contains("constructor-page")) {
            initDrawer({
                panelSelector: ".constructor-page .sidebar",
                backdropSelector: ".constructor-drawer-backdrop",
                openBtnSelector: ".js-constructor-nav-toggle",
                bodyClass: "constructor-drawer-open"
            });
        }

        const header = document.querySelector(".header");
        if (header && typeof toggleSidebar === "function") {
            const burger = header.querySelector(".burger");
            if (burger) {
                burger.setAttribute("role", "button");
                burger.setAttribute("aria-label", "Открыть меню");
                burger.setAttribute("tabindex", "0");
                burger.addEventListener("keydown", e => {
                    if (e.key === "Enter" || e.key === " ") {
                        e.preventDefault();
                        toggleSidebar();
                    }
                });
            }
        }
    });
})();
