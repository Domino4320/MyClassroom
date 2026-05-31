(function () {
    "use strict";

    let opened = false;
    let pendingLessonId = null;
    let onDoneCallback = null;

    function ensureModal() {
        let backdrop = document.getElementById("lessonFeedbackBackdrop");
        let modal = document.getElementById("lessonFeedbackModal");
        if (backdrop && modal) return { backdrop, modal };

        backdrop = document.createElement("div");
        backdrop.id = "lessonFeedbackBackdrop";
        backdrop.className = "lf-modal-backdrop";

        modal = document.createElement("div");
        modal.id = "lessonFeedbackModal";
        modal.className = "lf-modal";
        modal.innerHTML = `
            <div class="lf-modal-head">
                <h2 class="lf-modal-title">Оцените урок</h2>
                <button type="button" class="lf-close" id="lfCloseBtn" aria-label="Закрыть">×</button>
            </div>
            <p class="lf-lesson-name" id="lfLessonName"></p>
            <div class="lf-ratings" id="lfRatings"></div>
            <div class="lf-actions">
                <button type="button" class="lf-submit" id="lfSubmitBtn">Отправить</button>
                <button type="button" class="lf-skip" id="lfSkipBtn">Позже</button>
            </div>
            <p class="lf-error" id="lfError" style="display:none;"></p>`;

        document.body.appendChild(backdrop);
        document.body.appendChild(modal);

        const dims = [
            { key: "difficulty", label: "Сложность" },
            { key: "clarity", label: "Понятность" },
            { key: "interest", label: "Интересность" }
        ];

        const ratingsEl = modal.querySelector("#lfRatings");
        ratingsEl.innerHTML = dims.map(d => `
            <div class="lf-row" data-key="${d.key}">
                <span class="lf-label">${d.label}</span>
                <div class="lf-stars">
                    ${[1, 2, 3, 4, 5].map(n => `<button type="button" class="lf-star" data-val="${n}">${n}</button>`).join("")}
                </div>
            </div>`).join("");

        ratingsEl.querySelectorAll(".lf-star").forEach(btn => {
            btn.addEventListener("click", () => {
                const row = btn.closest(".lf-row");
                const val = Number(btn.dataset.val);
                row.dataset.value = String(val);
                row.querySelectorAll(".lf-star").forEach(s => {
                    s.classList.toggle("active", Number(s.dataset.val) <= val);
                });
            });
        });

        backdrop.addEventListener("click", close);
        modal.querySelector("#lfCloseBtn").addEventListener("click", close);
        modal.querySelector("#lfSkipBtn").addEventListener("click", close);
        modal.querySelector("#lfSubmitBtn").addEventListener("click", submit);

        document.addEventListener("keydown", e => {
            if (opened && e.key === "Escape") close();
        });

        return { backdrop, modal };
    }

    function open(lessonId, lessonTitle, onDone) {
        pendingLessonId = lessonId;
        onDoneCallback = typeof onDone === "function" ? onDone : null;
        const { backdrop, modal } = ensureModal();
        modal.querySelector("#lfLessonName").textContent = lessonTitle ? `«${lessonTitle}»` : "";
        modal.querySelector("#lfError").style.display = "none";
        modal.querySelectorAll(".lf-row").forEach(row => {
            row.dataset.value = "";
            row.querySelectorAll(".lf-star").forEach(s => s.classList.remove("active"));
        });
        backdrop.classList.add("open");
        modal.classList.add("open");
        opened = true;
    }

    function close() {
        document.getElementById("lessonFeedbackBackdrop")?.classList.remove("open");
        document.getElementById("lessonFeedbackModal")?.classList.remove("open");
        opened = false;
        const cb = onDoneCallback;
        onDoneCallback = null;
        cb?.();
    }

    async function submit() {
        const modal = document.getElementById("lessonFeedbackModal");
        const errEl = modal?.querySelector("#lfError");
        const rows = modal?.querySelectorAll(".lf-row") || [];

        const difficulty = Number(rows[0]?.dataset.value || 0);
        const clarity = Number(rows[1]?.dataset.value || 0);
        const interest = Number(rows[2]?.dataset.value || 0);

        if (!difficulty || !clarity || !interest) {
            if (errEl) {
                errEl.textContent = "Поставьте оценку по всем трём пунктам (1–5).";
                errEl.style.display = "block";
            }
            return;
        }

        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        const fd = new FormData();
        fd.append("lessonId", String(pendingLessonId));
        fd.append("difficulty", String(difficulty));
        fd.append("clarity", String(clarity));
        fd.append("interest", String(interest));
        if (token) fd.append("__RequestVerificationToken", token);

        const res = await fetch("/Course/SubmitLessonFeedback", { method: "POST", body: fd });
        const data = await res.json().catch(() => ({}));
        if (!data.success) {
            if (errEl) {
                errEl.textContent = data.message || "Ошибка отправки";
                errEl.style.display = "block";
            }
            return;
        }
        close();
    }

    function ensureStatsModal() {
        let backdrop = document.getElementById("lfStatsBackdrop");
        let modal = document.getElementById("lfStatsModal");
        if (backdrop && modal) return { backdrop, modal };

        backdrop = document.createElement("div");
        backdrop.id = "lfStatsBackdrop";
        backdrop.className = "lf-modal-backdrop";

        modal = document.createElement("div");
        modal.id = "lfStatsModal";
        modal.className = "lf-modal lf-stats-modal";
        modal.innerHTML = `
            <div class="lf-modal-head">
                <div>
                    <h2 class="lf-modal-title">Оценки уроков</h2>
                    <p class="lf-stats-sub" id="lfStatsSub"></p>
                </div>
                <button type="button" class="lf-close" id="lfStatsClose" aria-label="Закрыть">×</button>
            </div>
            <div class="lf-stats-body" id="lfStatsBody"></div>`;

        document.body.appendChild(backdrop);
        document.body.appendChild(modal);
        backdrop.addEventListener("click", () => closeStats());
        modal.querySelector("#lfStatsClose").addEventListener("click", () => closeStats());

        return { backdrop, modal };
    }

    function closeStats() {
        document.getElementById("lfStatsBackdrop")?.classList.remove("open");
        document.getElementById("lfStatsModal")?.classList.remove("open");
    }

    async function openLessonFeedbackStats(courseId) {
        const { backdrop, modal } = ensureStatsModal();
        const body = modal.querySelector("#lfStatsBody");
        const sub = modal.querySelector("#lfStatsSub");
        body.innerHTML = "<p class=\"lf-loading\">Загрузка…</p>";
        sub.textContent = "";
        backdrop.classList.add("open");
        modal.classList.add("open");

        try {
            const res = await fetch(`/TeacherAnalytics/LessonFeedbackStats?courseId=${encodeURIComponent(courseId)}`);
            if (!res.ok) throw new Error("forbidden");
            const data = await res.json();
            sub.textContent = data.courseTitle ? `«${data.courseTitle}»` : "";

            const lessons = data.lessons || [];
            if (!lessons.length) {
                body.innerHTML = "<p class=\"lf-empty\">В курсе пока нет уроков</p>";
                return;
            }

            body.innerHTML = `
                <table class="lf-stats-table">
                    <thead>
                        <tr>
                            <th>Урок</th>
                            <th>Ответов</th>
                            <th>Сложность</th>
                            <th>Понятность</th>
                            <th>Интерес</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${lessons.map(l => `
                            <tr>
                                <td>
                                    <span class="lf-mod">${escapeHtml(l.moduleTitle || "")}</span>
                                    <span class="lf-lesson">${escapeHtml(l.lessonTitle || "")}</span>
                                </td>
                                <td>${l.responses || 0}</td>
                                <td>${fmt(l.avgDifficulty)}</td>
                                <td>${fmt(l.avgClarity)}</td>
                                <td>${fmt(l.avgInterest)}</td>
                            </tr>`).join("")}
                    </tbody>
                </table>`;
        } catch {
            body.innerHTML = "<p class=\"lf-empty\">Не удалось загрузить статистику</p>";
        }
    }

    function fmt(v) {
        return v == null ? "—" : String(v);
    }

    function escapeHtml(s) {
        const d = document.createElement("div");
        d.textContent = s;
        return d.innerHTML;
    }

    function initLessonFeedbackStatsButtons() {
        document.querySelectorAll(".js-lesson-feedback-stats").forEach(btn => {
            if (btn.dataset.lfBound) return;
            btn.dataset.lfBound = "1";
            btn.addEventListener("click", e => {
                e.preventDefault();
                e.stopPropagation();
                const id = btn.dataset.courseId;
                if (id) openLessonFeedbackStats(id);
            });
        });
    }

    function openLessonFeedbackStatsFromButton(btn) {
        const id = btn?.dataset?.courseId;
        if (id) openLessonFeedbackStats(id);
        return false;
    }

    window.openLessonFeedbackModal = open;
    window.initLessonFeedbackStatsButtons = initLessonFeedbackStatsButtons;
    window.openLessonFeedbackStatsFromButton = openLessonFeedbackStatsFromButton;
})();
