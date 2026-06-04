(function () {
    "use strict";

    const STORAGE_KEY = "mc-theme";

    function getPreferred() {
        try {
            const stored = localStorage.getItem(STORAGE_KEY);
            if (stored === "light" || stored === "dark") return stored;
        } catch { /* ignore */ }
        if (window.matchMedia && window.matchMedia("(prefers-color-scheme: light)").matches) {
            return "light";
        }
        return "dark";
    }

    function apply(theme) {
        const t = theme === "light" ? "light" : "dark";
        document.documentElement.setAttribute("data-theme", t);
        try {
            localStorage.setItem(STORAGE_KEY, t);
        } catch { /* ignore */ }
        document.querySelectorAll(".theme-toggle").forEach(btn => {
            btn.setAttribute("aria-label", t === "light" ? "Включить тёмную тему" : "Включить светлую тему");
            btn.setAttribute("title", t === "light" ? "Тёмная тема" : "Светлая тема");
        });
    }

    function toggle() {
        const current = document.documentElement.getAttribute("data-theme") || "dark";
        apply(current === "light" ? "dark" : "light");
    }

    window.McTheme = {
        get: () => document.documentElement.getAttribute("data-theme") || "dark",
        set: apply,
        toggle
    };

    document.addEventListener("click", e => {
        const btn = e.target.closest(".theme-toggle");
        if (btn) {
            e.preventDefault();
            toggle();
        }
    });

    if (!document.documentElement.getAttribute("data-theme")) {
        apply(getPreferred());
    }
})();
