(function () {
    let opened = false;

    function ensureModal() {
        let backdrop = document.getElementById("analyticsModalBackdrop");
        let modal = document.getElementById("analyticsModal");
        if (backdrop && modal) return { backdrop, modal };

        backdrop = document.createElement("div");
        backdrop.id = "analyticsModalBackdrop";
        backdrop.className = "analytics-modal-backdrop";

        modal = document.createElement("div");
        modal.id = "analyticsModal";
        modal.className = "analytics-modal";
        modal.innerHTML = `
            <div class="analytics-modal-head">
                <div>
                    <h2 class="analytics-modal-title" id="analyticsTitle">Аналитика курса</h2>
                    <div class="analytics-modal-sub" id="analyticsSub">Загрузка…</div>
                </div>
                <button type="button" class="analytics-close" id="analyticsCloseBtn" aria-label="Закрыть">×</button>
            </div>
            <div class="analytics-modal-body" id="analyticsBody"></div>
        `;

        document.body.appendChild(backdrop);
        document.body.appendChild(modal);

        backdrop.addEventListener("click", close);
        modal.querySelector("#analyticsCloseBtn").addEventListener("click", close);
        document.addEventListener("keydown", (e) => {
            if (opened && e.key === "Escape") close();
        });

        return { backdrop, modal };
    }

    function open() {
        const { backdrop, modal } = ensureModal();
        backdrop.classList.add("open");
        modal.classList.add("open");
        opened = true;
    }

    function close() {
        const backdrop = document.getElementById("analyticsModalBackdrop");
        const modal = document.getElementById("analyticsModal");
        backdrop?.classList.remove("open");
        modal?.classList.remove("open");
        opened = false;
    }

    function setContent(stats) {
        const title = document.getElementById("analyticsTitle");
        const sub = document.getElementById("analyticsSub");
        const body = document.getElementById("analyticsBody");
        if (!body) return;

        if (title) title.textContent = "Аналитика курса";
        if (sub) sub.textContent = stats.courseTitle ? `«${stats.courseTitle}»` : "";

        const students = Number(stats.studentsCount || 0);
        const totalSteps = Number(stats.totalSteps || 0);
        const avgSteps = stats.avgCompletedSteps ?? 0;
        const avgPct = Number(stats.avgCompletedPercent || 0);
        const b = stats.buckets || {};

        const rows = [
            { label: "0%", count: Number(b.none || 0) },
            { label: "до 25%", count: Number(b.lt25 || 0) },
            { label: "до 50%", count: Number(b.lt50 || 0) },
            { label: "до 75%", count: Number(b.lt75 || 0) },
            { label: "≥ 75%", count: Number(b.gte75 || 0) }
        ];

        body.innerHTML = `
            <div class="analytics-kpis">
                <div class="analytics-kpi">
                    <div class="analytics-kpi-label">Студентов на курсе</div>
                    <div class="analytics-kpi-value">${students}</div>
                    <div class="analytics-kpi-hint">По количеству записавшихся</div>
                </div>
                <div class="analytics-kpi">
                    <div class="analytics-kpi-label">Шагов в курсе</div>
                    <div class="analytics-kpi-value">${totalSteps}</div>
                    <div class="analytics-kpi-hint">Всего шагов</div>
                </div>
                <div class="analytics-kpi">
                    <div class="analytics-kpi-label">Средний прогресс</div>
                    <div class="analytics-kpi-value">${avgPct}%</div>
                    <div class="analytics-kpi-hint">≈ ${avgSteps} шагов</div>
                </div>
            </div>

            <div class="analytics-bars">
                <h3>Распределение по прогрессу</h3>
                ${rows.map(r => {
                    const pct = students === 0 ? 0 : Math.round((r.count / students) * 100);
                    return `
                        <div class="analytics-bar-row">
                            <div class="analytics-bar-label">${r.label}</div>
                            <div class="analytics-bar"><span style="width:${pct}%"></span></div>
                            <div class="analytics-bar-count">${r.count}</div>
                        </div>
                    `;
                }).join("")}
            </div>
        `;
    }

    async function openCourseAnalytics(courseId) {
        open();
        const sub = document.getElementById("analyticsSub");
        const body = document.getElementById("analyticsBody");
        if (sub) sub.textContent = "Загрузка…";
        if (body) body.innerHTML = "";

        try {
            const res = await fetch(`/TeacherAnalytics/CourseStats?courseId=${encodeURIComponent(courseId)}`, { method: "GET" });
            if (!res.ok) throw new Error("bad response");
            const stats = await res.json();
            setContent(stats);
        } catch (e) {
            if (sub) sub.textContent = "Не удалось загрузить статистику";
            if (body) body.innerHTML = `<div style="color:var(--text-muted);">Попробуйте ещё раз позже.</div>`;
        }
    }

    function initCourseAnalyticsButtons() {
        document.addEventListener("click", (e) => {
            const btn = e.target.closest(".js-course-analytics");
            if (!btn) return;
            e.preventDefault();
            e.stopPropagation();
            const courseId = btn.getAttribute("data-course-id");
            if (!courseId) return;
            openCourseAnalytics(courseId);
        });
    }

    function openCourseAnalyticsFromButton(btn) {
        if (!btn) return false;
        const courseId = btn.getAttribute("data-course-id");
        if (!courseId) return false;
        openCourseAnalytics(courseId);
        return false;
    }

    window.initCourseAnalyticsButtons = initCourseAnalyticsButtons;
    window.openCourseAnalyticsFromButton = openCourseAnalyticsFromButton;
})();

