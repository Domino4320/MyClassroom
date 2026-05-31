(function () {
    "use strict";

    const ALLOWED_EXT = [".pptx", ".docx", ".pdf", ".xlsx", ".txt"];
    const MAX_MB = 100;
    const MAX_FILES = 5;
    const MAX_LINKS = 10;

    function extOf(name) {
        const i = (name || "").lastIndexOf(".");
        return i >= 0 ? name.slice(i).toLowerCase() : "";
    }

    function linkHost(url) {
        try {
            return new URL(url).hostname.replace(/^www\./, "");
        } catch {
            return url || "";
        }
    }

    function countByKind(items) {
        let files = 0;
        let links = 0;
        for (const m of items || []) {
            if (m.kind === 0) files++;
            else if (m.kind === 1) links++;
        }
        return { files, links };
    }

    function syncActions(rootEl, items) {
        if (!rootEl) return;
        const { files, links } = countByKind(items);
        const uploadBtn = rootEl.querySelector(".materials-upload-btn");
        const addLinkBtn = rootEl.querySelector(".materials-add-link-btn");
        const linkForm = rootEl.querySelector(".materials-link-form");
        const limitsEl = rootEl.querySelector(".materials-limits");

        const fileLimit = files >= MAX_FILES;
        const linkLimit = links >= MAX_LINKS;

        if (uploadBtn) {
            uploadBtn.classList.toggle("is-disabled", fileLimit);
            uploadBtn.title = fileLimit ? `Лимит: ${MAX_FILES} файлов на урок` : "";
        }

        if (addLinkBtn) {
            addLinkBtn.disabled = linkLimit;
            addLinkBtn.classList.toggle("is-disabled", linkLimit);
            addLinkBtn.title = linkLimit ? `Лимит: ${MAX_LINKS} ссылок на урок` : "";
        }

        if (linkLimit && linkForm) {
            linkForm.classList.remove("is-open");
        }

        if (limitsEl) {
            limitsEl.textContent = `Файлов: ${files}/${MAX_FILES} · Ссылок: ${links}/${MAX_LINKS}`;
        }
    }

    async function saveCaption(id, title, msgEl) {
        const caption = (title || "").trim();
        if (!caption) {
            if (msgEl) showMsg(msgEl, "Укажите подпись к файлу.", true);
            return false;
        }
        const res = await fetch("/CourseConstructor/UpdateLessonMaterial", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ id, title: caption })
        });
        const data = await res.json().catch(() => ({}));
        if (!res.ok) {
            if (msgEl) showMsg(msgEl, data.message || "Не удалось сохранить подпись", true);
            return false;
        }
        if (msgEl) showMsg(msgEl, "");
        return true;
    }

    function bindFileRows(container, rootEl, msgEl) {
        container.querySelectorAll(".materials-caption-input").forEach(input => {
            input.addEventListener("keydown", e => {
                if (e.key === "Enter") {
                    e.preventDefault();
                    input.closest(".materials-row")?.querySelector(".materials-caption-save")?.click();
                }
            });
        });

        container.querySelectorAll(".materials-caption-save").forEach(btn => {
            btn.addEventListener("click", async () => {
                const row = btn.closest(".materials-row");
                const id = row?.dataset.id;
                const input = row?.querySelector(".materials-caption-input");
                if (!id || !input) return;
                btn.disabled = true;
                const ok = await saveCaption(id, input.value, msgEl);
                btn.disabled = false;
                if (ok) {
                    await refreshMaterials(container.dataset.lessonId, container, rootEl);
                }
            });
        });

        container.querySelectorAll(".materials-del").forEach(btn => {
            btn.addEventListener("click", async () => {
                const id = btn.dataset.id;
                if (!confirm("Удалить материал?")) return;
                const res = await fetch(`/CourseConstructor/DeleteLessonMaterial/${id}`, { method: "POST" });
                if (!res.ok) {
                    alert("Не удалось удалить");
                    return;
                }
                await refreshMaterials(container.dataset.lessonId, container, rootEl);
            });
        });
    }

    function renderList(container, items) {
        if (!items.length) {
            container.innerHTML = `<p class="materials-empty">До ${MAX_FILES} файлов и ${MAX_LINKS} ссылок</p>`;
            return;
        }
        container.innerHTML = items.map(m => {
            const isFile = m.kind === 0;
            if (isFile) {
                const href = m.downloadUrl || "#";
                return `
                <div class="materials-row materials-row--file" data-id="${m.id}">
                    <div class="materials-file-top">
                        <span class="materials-link-icon">📄</span>
                        <span class="materials-file-name" title="${escapeAttr(m.fileName || "")}">${escapeHtml(m.fileName || "")}</span>
                        <a class="btn btn-outline materials-download-btn" href="${escapeAttr(href)}" download>Скачать</a>
                        <button type="button" class="materials-del" title="Удалить" data-id="${m.id}">×</button>
                    </div>
                    <div class="materials-file-caption-block">
                        <label class="materials-caption-label">Подпись</label>
                        <div class="materials-caption-row">
                            <input type="text" class="materials-caption-input" value="${escapeAttr(m.title)}" maxlength="200" placeholder="Описание для учеников" />
                            <button type="button" class="btn btn-outline materials-caption-save" title="Сохранить подпись">✓</button>
                        </div>
                    </div>
                </div>`;
            }
            const href = m.url || "#";
            const host = linkHost(m.url);
            return `
                <div class="materials-row materials-row--link" data-id="${m.id}">
                    <a class="materials-link materials-link--url" href="${escapeAttr(href)}" target="_blank" rel="noopener noreferrer">
                        <span class="materials-link-icon">↗</span>
                        <span class="materials-link-body">
                            <span class="materials-title">${escapeHtml(m.title)}</span>
                            <span class="materials-sub">${escapeHtml(host)}</span>
                        </span>
                        <span class="materials-open-badge">Открыть</span>
                    </a>
                    <button type="button" class="materials-del" title="Удалить" data-id="${m.id}">×</button>
                </div>`;
        }).join("");
    }

    async function refreshMaterials(lessonId, listEl, rootEl) {
        const res = await fetch(`/CourseConstructor/GetLessonMaterials?lessonId=${encodeURIComponent(lessonId)}`);
        if (!res.ok) return;
        const items = await res.json();
        renderList(listEl, items);
        bindFileRows(listEl, rootEl, rootEl?.querySelector(".materials-msg"));
        syncActions(rootEl, items);
    }

    function showMsg(msgEl, text, isError) {
        if (!msgEl) return;
        msgEl.textContent = text;
        msgEl.style.display = text ? "block" : "none";
        msgEl.className = "materials-msg" + (isError ? " is-error" : "");
    }

    window.LessonMaterialsEditor = {
        mount(lessonId, rootEl) {
            if (!rootEl || !lessonId) return;
            rootEl.innerHTML = `
                <div class="lesson-materials-block">
                    <div class="lesson-materials-head">
                        <span>Материалы урока</span>
                        <span class="lesson-materials-hint">до ${MAX_MB} МБ · ${ALLOWED_EXT.join(", ")}</span>
                    </div>
                    <p class="materials-limits">Файлов: 0/${MAX_FILES} · Ссылок: 0/${MAX_LINKS}</p>
                    <div class="materials-list" data-lesson-id="${lessonId}"></div>
                    <div class="materials-actions">
                        <label class="btn btn-outline materials-upload-btn">
                            <input type="file" class="materials-file-input" accept=".pptx,.docx,.pdf,.xlsx,.txt" hidden />
                            + Файл
                        </label>
                        <button type="button" class="btn btn-outline materials-add-link-btn">+ Ссылка</button>
                    </div>
                    <div class="materials-upload-caption">
                        <label for="materialsFileCaption">Подпись для нового файла</label>
                        <input type="text" id="materialsFileCaption" class="materials-file-caption" placeholder="Появится под материалом у учеников" maxlength="200" />
                    </div>
                    <div class="materials-link-form">
                        <div class="materials-link-field">
                            <label>Название ссылки</label>
                            <input type="text" class="materials-link-title" placeholder="Необязательно" />
                        </div>
                        <div class="materials-link-field">
                            <label>Адрес (URL)</label>
                            <input type="text" class="materials-link-url" placeholder="https://..." inputmode="url" autocomplete="url" />
                        </div>
                        <div class="materials-link-actions">
                            <button type="button" class="btn btn-primary materials-link-save">Добавить</button>
                            <button type="button" class="btn btn-outline materials-link-cancel">Отмена</button>
                        </div>
                    </div>
                    <p class="materials-msg" style="display:none;"></p>
                </div>`;

            const blockEl = rootEl.querySelector(".lesson-materials-block");
            const listEl = rootEl.querySelector(".materials-list");
            const msgEl = rootEl.querySelector(".materials-msg");
            const linkForm = rootEl.querySelector(".materials-link-form");

            refreshMaterials(lessonId, listEl, blockEl);

            rootEl.querySelector(".materials-file-input")?.addEventListener("change", async e => {
                const file = e.target.files?.[0];
                e.target.value = "";
                if (!file) return;

                const { files } = countByKind(await fetchMaterials(lessonId));
                if (files >= MAX_FILES) {
                    showMsg(msgEl, `Максимум ${MAX_FILES} файлов на урок.`, true);
                    return;
                }

                if (file.size > MAX_MB * 1024 * 1024) {
                    showMsg(msgEl, `Файл больше ${MAX_MB} МБ`, true);
                    return;
                }
                if (!ALLOWED_EXT.includes(extOf(file.name))) {
                    showMsg(msgEl, "Недопустимый тип файла", true);
                    return;
                }

                const captionInput = rootEl.querySelector(".materials-file-caption");
                let title = captionInput?.value?.trim() || "";
                if (!title) {
                    title = file.name.replace(/\.[^.]+$/, "") || file.name;
                }

                const fd = new FormData();
                fd.append("file", file);
                fd.append("title", title);
                const res = await fetch(`/CourseConstructor/UploadLessonMaterial?lessonId=${lessonId}`, {
                    method: "POST",
                    body: fd
                });
                const data = await res.json().catch(() => ({}));
                if (!res.ok) {
                    showMsg(msgEl, data.message || "Ошибка загрузки", true);
                    return;
                }
                if (captionInput) captionInput.value = "";
                showMsg(msgEl, "");
                await refreshMaterials(lessonId, listEl, blockEl);
            });

            rootEl.querySelector(".materials-add-link-btn")?.addEventListener("click", () => {
                const btn = rootEl.querySelector(".materials-add-link-btn");
                if (btn?.disabled || btn?.classList.contains("is-disabled")) return;
                showMsg(msgEl, "");
                linkForm.classList.add("is-open");
            });
            rootEl.querySelector(".materials-link-cancel")?.addEventListener("click", () => {
                linkForm.classList.remove("is-open");
                rootEl.querySelector(".materials-link-title").value = "";
                rootEl.querySelector(".materials-link-url").value = "";
            });

            rootEl.querySelector(".materials-link-save")?.addEventListener("click", async () => {
                const title = rootEl.querySelector(".materials-link-title")?.value?.trim() || "";
                const url = rootEl.querySelector(".materials-link-url")?.value?.trim() || "";
                if (!url) {
                    showMsg(msgEl, "Укажите ссылку", true);
                    return;
                }
                const res = await fetch("/CourseConstructor/AddLessonLink", {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ lessonId, title, url })
                });
                const data = await res.json().catch(() => ({}));
                if (!res.ok) {
                    showMsg(msgEl, data.message || "Ошибка", true);
                    return;
                }
                linkForm.classList.remove("is-open");
                rootEl.querySelector(".materials-link-title").value = "";
                rootEl.querySelector(".materials-link-url").value = "";
                showMsg(msgEl, "");
                await refreshMaterials(lessonId, listEl, blockEl);
            });
        }
    };

    async function fetchMaterials(lessonId) {
        const res = await fetch(`/CourseConstructor/GetLessonMaterials?lessonId=${encodeURIComponent(lessonId)}`);
        if (!res.ok) return [];
        return res.json();
    }

    function escapeHtml(s) {
        const d = document.createElement("div");
        d.textContent = s;
        return d.innerHTML;
    }

    function escapeAttr(s) {
        return String(s || "").replace(/&/g, "&amp;").replace(/"/g, "&quot;").replace(/</g, "&lt;");
    }
})();
