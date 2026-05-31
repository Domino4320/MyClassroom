(function () {
    "use strict";

    const KIND_LABELS = {
        match: "Сопоставление пар",
        sequence: "Последовательность",
        truefalse: "Верно / Неверно",
        fillblanks: "Заполнение пропусков",
        imagechoice: "Выбор картинки"
    };

    const MAX_INTERACTIVE_IMAGES = 6;
    const MAX_IMAGE_MB = 5;
    const IMAGE_EXT = [".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp"];

    const DEFAULT_INSTRUCTIONS = {
        match: "Сопоставьте термины с определениями",
        sequence: "Расставьте элементы в правильном порядке",
        truefalse: "Определите, какие утверждения верны, а какие нет",
        fillblanks: "Заполните пропуски в тексте",
        imagechoice: "Выберите правильный вариант по картинке"
    };

    window.InteractiveStepEditor = {
        parse: parseConfig,
        render: renderEditor,
        serialize: serializeFromCard,
        serializeFromCard: serializeFromCard,
        validate: validateConfig,
        validateFromCard: validateFromCard,
        defaultConfig: defaultConfig,
        normalizeConfig: normalizeConfig,
        getDefaultInstruction: kind => DEFAULT_INSTRUCTIONS[normalizeKind(kind)] || DEFAULT_INSTRUCTIONS.match
    };

    function normalizeKind(kind) {
        const k = (kind || "match").toLowerCase();
        return k === "order" ? "sequence" : k;
    }

    function defaultConfig(kind) {
        const k = normalizeKind(kind);
        if (k === "sequence") {
            return {
                kind: "sequence",
                instruction: DEFAULT_INSTRUCTIONS.sequence,
                items: ["Шаг 1", "Шаг 2", "Шаг 3"]
            };
        }
        if (k === "truefalse") {
            return {
                kind: "truefalse",
                instruction: DEFAULT_INSTRUCTIONS.truefalse,
                statements: [
                    { text: "HTML описывает структуру страницы", isTrue: true },
                    { text: "CSS выполняет вычисления на сервере", isTrue: false }
                ]
            };
        }
        if (k === "fillblanks") {
            return {
                kind: "fillblanks",
                instruction: DEFAULT_INSTRUCTIONS.fillblanks,
                template: "HTTP — это ___ протокол. Код 404 означает ___.",
                blanks: ["прикладной", "не найдено"]
            };
        }
        if (k === "imagechoice") {
            return {
                kind: "imagechoice",
                instruction: DEFAULT_INSTRUCTIONS.imagechoice,
                question: "Какой вариант верный?",
                options: [
                    { id: "a", label: "Вариант A", imageUrl: "" },
                    { id: "b", label: "Вариант B", imageUrl: "" }
                ],
                correctOptionId: "a"
            };
        }
        return {
            kind: "match",
            instruction: DEFAULT_INSTRUCTIONS.match,
            pairs: [
                { left: "HTML", right: "Разметка" },
                { left: "CSS", right: "Стили" }
            ]
        };
    }

    function normalizeConfig(cfg) {
        if (!cfg) return defaultConfig("match");

        const kind = normalizeKind(cfg.kind);
        cfg.kind = kind;

        const instruction = (cfg.instruction || "").trim();
        const allDefaults = Object.values(DEFAULT_INSTRUCTIONS);
        if (!instruction || allDefaults.includes(instruction)) {
            cfg.instruction = DEFAULT_INSTRUCTIONS[kind] || DEFAULT_INSTRUCTIONS.match;
        }

        return cfg;
    }

    function parseConfig(json) {
        if (!json || !json.trim()) return defaultConfig("match");
        try {
            const raw = JSON.parse(json);
            const cfg = {
                kind: raw.kind ?? raw.Kind ?? "match",
                instruction: raw.instruction ?? raw.Instruction ?? "",
                pairs: (raw.pairs ?? raw.Pairs ?? []).map(p => ({
                    left: p?.left ?? p?.Left ?? "",
                    right: p?.right ?? p?.Right ?? ""
                })),
                items: raw.items ?? raw.Items ?? [],
                statements: (raw.statements ?? raw.Statements ?? []).map(s => ({
                    text: s?.text ?? s?.Text ?? "",
                    isTrue: !!(s?.isTrue ?? s?.IsTrue)
                })),
                template: raw.template ?? raw.Template ?? "",
                blanks: raw.blanks ?? raw.Blanks ?? [],
                question: raw.question ?? raw.Question ?? "",
                options: (raw.options ?? raw.Options ?? []).map(o => ({
                    id: o?.id ?? o?.Id ?? "",
                    label: o?.label ?? o?.Label ?? "",
                    imageUrl: o?.imageUrl ?? o?.ImageUrl ?? ""
                })),
                correctOptionId: raw.correctOptionId ?? raw.CorrectOptionId ?? ""
            };
            if (!cfg.kind) cfg.kind = "match";
            return normalizeConfig(cfg);
        } catch {
            return defaultConfig("match");
        }
    }

    function renderEditor(container, stepId, configJson) {
        const config = parseConfig(configJson);
        container.innerHTML = "";

        const wrap = document.createElement("div");
        wrap.className = "interactive-constructor";
        wrap.dataset.stepId = String(stepId);

        wrap.innerHTML = `
            <label>Тип задания</label>
            <select class="interactive-kind-select" data-step-id="${stepId}">
                ${Object.entries(KIND_LABELS).map(([k, label]) =>
                    `<option value="${k}" ${config.kind === k ? "selected" : ""}>${label}</option>`
                ).join("")}
            </select>
            <label style="margin-top:12px;display:block;">Инструкция для студента</label>
            <textarea class="interactive-instruction" rows="2" placeholder="Что нужно сделать...">${escapeAttr(config.instruction || "")}</textarea>
            <span id="interactive-instruction-error-${stepId}" class="step-field-error" style="display:none;"></span>
            <div class="interactive-fields" id="interactive-fields-${stepId}"></div>
            <input type="hidden" class="step-interactive-json" data-id="${stepId}" value="">`;

        container.appendChild(wrap);

        const kindSelect = wrap.querySelector(".interactive-kind-select");
        kindSelect.addEventListener("change", () => {
            const kind = kindSelect.value;
            const fresh = defaultConfig(kind);
            wrap.querySelector(".interactive-instruction").value = fresh.instruction;
            renderFields(wrap.querySelector(`#interactive-fields-${stepId}`), fresh);
            syncHidden(wrap);
            clearInteractiveFieldErrors(wrap);
        });

        wrap.querySelector(".interactive-instruction").addEventListener("input", () => {
            wrap.querySelector(".interactive-instruction")?.classList.remove("input-invalid");
            const err = document.getElementById(`interactive-instruction-error-${stepId}`);
            if (err) err.style.display = "none";
            syncHidden(wrap);
        });

        renderFields(wrap.querySelector(`#interactive-fields-${stepId}`), config);
        syncHidden(wrap);
    }

    function renderFields(container, config) {
        container.innerHTML = "";
        const kind = normalizeKind(config.kind);

        if (kind === "match") {
            container.innerHTML = `<label>Пары «термин → определение»</label><div class="interactive-pairs-list"></div>
                <button type="button" class="btn btn-outline interactive-add-pair" style="margin-top:8px;width:100%;">+ Добавить пару</button>`;
            const list = container.querySelector(".interactive-pairs-list");
            (config.pairs || []).forEach(p => addPairRow(list, p.left, p.right));
            if (!(config.pairs || []).length) addPairRow(list, "", "");
            container.querySelector(".interactive-add-pair").addEventListener("click", () => {
                addPairRow(list, "", "");
                syncHidden(container.closest(".interactive-constructor"));
            });
            list.addEventListener("input", () => syncHidden(container.closest(".interactive-constructor")));
        } else if (kind === "sequence") {
            container.innerHTML = `<label>Элементы в правильном порядке (сверху вниз)</label><div class="interactive-items-list"></div>
                <button type="button" class="btn btn-outline interactive-add-item" style="margin-top:8px;width:100%;">+ Добавить элемент</button>`;
            const list = container.querySelector(".interactive-items-list");
            (config.items || []).forEach(item => addItemRow(list, item));
            if (!(config.items || []).length) addItemRow(list, "");
            container.querySelector(".interactive-add-item").addEventListener("click", () => {
                addItemRow(list, "");
                syncHidden(container.closest(".interactive-constructor"));
            });
            list.addEventListener("input", () => syncHidden(container.closest(".interactive-constructor")));
        } else if (kind === "truefalse") {
            container.innerHTML = `<label>Утверждения</label><div class="interactive-statements-list"></div>
                <button type="button" class="btn btn-outline interactive-add-stmt" style="margin-top:8px;width:100%;">+ Добавить утверждение</button>`;
            const list = container.querySelector(".interactive-statements-list");
            (config.statements || []).forEach(s => addStatementRow(list, s.text, s.isTrue));
            if (!(config.statements || []).length) addStatementRow(list, "", true);
            container.querySelector(".interactive-add-stmt").addEventListener("click", () => {
                addStatementRow(list, "", true);
                syncHidden(container.closest(".interactive-constructor"));
            });
            list.addEventListener("input", () => syncHidden(container.closest(".interactive-constructor")));
            list.addEventListener("change", () => syncHidden(container.closest(".interactive-constructor")));
        } else if (kind === "fillblanks") {
            container.innerHTML = `
                <label>Текст с пропусками (используйте «___»)</label>
                <textarea class="interactive-fill-template" rows="4" placeholder="Пример: HTTP — это ___ протокол.">${escapeAttr(config.template || "")}</textarea>
                <label style="margin-top:10px;display:block;">Ответы по порядку (через строку)</label>
                <textarea class="interactive-fill-blanks" rows="3" placeholder="Каждый ответ с новой строки">${escapeAttr((config.blanks || []).join("\n"))}</textarea>`;
            container.querySelectorAll("textarea").forEach(ta => {
                ta.addEventListener("input", () => syncHidden(container.closest(".interactive-constructor")));
            });
        } else if (kind === "imagechoice") {
            container.innerHTML = `
                <label>Вопрос (необязательно)</label>
                <input type="text" class="interactive-img-question" value="${escapeAttr(config.question || "")}" />
                <p class="interactive-img-hint">До ${MAX_INTERACTIVE_IMAGES} картинок · ${MAX_IMAGE_MB} МБ · ${IMAGE_EXT.join(", ")}</p>
                <label style="margin-top:8px;display:block;">Варианты с картинками</label>
                <div class="interactive-img-options-list"></div>
                <button type="button" class="btn btn-outline interactive-add-img-opt" style="margin-top:8px;width:100%;">+ Вариант</button>`;
            const list = container.querySelector(".interactive-img-options-list");
            const wrap = container.closest(".interactive-constructor");
            (config.options || []).forEach(o => addImageOptionRow(list, o, config.correctOptionId));
            if (!(config.options || []).length) {
                addImageOptionRow(list, { id: "a", label: "Вариант A", imageUrl: "" }, config.correctOptionId);
                addImageOptionRow(list, { id: "b", label: "Вариант B", imageUrl: "" }, config.correctOptionId);
            }
            container.querySelector(".interactive-add-img-opt").addEventListener("click", () => {
                if (list.children.length >= MAX_INTERACTIVE_IMAGES) return;
                const id = "opt" + Date.now();
                addImageOptionRow(list, { id, label: "", imageUrl: "" }, "");
                syncHidden(wrap);
                updateImageAddButton(wrap);
            });
            list.addEventListener("input", () => syncHidden(wrap));
            list.addEventListener("change", () => syncHidden(wrap));
            updateImageAddButton(wrap);
        }
    }

    function updateImageAddButton(wrap) {
        const btn = wrap?.querySelector(".interactive-add-img-opt");
        if (!btn) return;
        const count = wrap.querySelectorAll(".interactive-img-opt-row").length;
        const atLimit = count >= MAX_INTERACTIVE_IMAGES;
        btn.disabled = atLimit;
        btn.classList.toggle("is-disabled", atLimit);
        btn.textContent = atLimit ? `Лимит: ${MAX_INTERACTIVE_IMAGES} картинок` : "+ Вариант";
    }

    function imageExtOk(name) {
        const i = (name || "").lastIndexOf(".");
        const ext = i >= 0 ? name.slice(i).toLowerCase() : "";
        return IMAGE_EXT.includes(ext);
    }

    function updateImagePreview(row) {
        const url = row.querySelector(".interactive-img-url")?.value?.trim() || "";
        const img = row.querySelector(".interactive-img-preview");
        const ph = row.querySelector(".interactive-img-placeholder");
        if (!img || !ph) return;
        if (url) {
            img.src = url;
            img.style.display = "block";
            ph.style.display = "none";
        } else {
            img.removeAttribute("src");
            img.style.display = "none";
            ph.style.display = "block";
        }
    }

    async function uploadInteractiveImage(stepId, file, replacePath) {
        const fd = new FormData();
        fd.append("file", file);
        let url = `/CourseConstructor/UploadInteractiveImage?stepId=${encodeURIComponent(stepId)}`;
        if (replacePath) url += `&replacePath=${encodeURIComponent(replacePath)}`;
        const res = await fetch(url, { method: "POST", body: fd });
        const data = await res.json().catch(() => ({}));
        if (!res.ok) throw new Error(data.message || "Ошибка загрузки");
        return data.imageUrl;
    }

    async function deleteInteractiveImage(stepId, path) {
        if (!path || !path.includes("/uploads/interactive/")) return;
        await fetch(
            `/CourseConstructor/DeleteInteractiveImage?stepId=${encodeURIComponent(stepId)}&path=${encodeURIComponent(path)}`,
            { method: "POST" }
        );
    }

    function addImageOptionRow(list, opt, correctId) {
        const wrap = list.closest(".interactive-constructor");
        const stepId = wrap?.dataset?.stepId;
        const row = document.createElement("div");
        row.className = "interactive-img-opt-row";
        const hasImage = !!(opt.imageUrl || "").trim();
        row.innerHTML = `
            <div class="interactive-img-opt-main">
                <label class="interactive-img-correct-wrap" title="Правильный ответ">
                    <input type="radio" name="interactive-img-correct-${stepId}" class="interactive-img-correct" value="${escapeAttr(opt.id)}" ${opt.id === correctId ? "checked" : ""}>
                </label>
                <input type="text" class="interactive-img-label" placeholder="Подпись" value="${escapeAttr(opt.label || "")}">
                <input type="hidden" class="interactive-img-url" value="${escapeAttr(opt.imageUrl || "")}">
                <input type="hidden" class="interactive-img-id" value="${escapeAttr(opt.id || "")}">
                <div class="interactive-img-preview-box">
                    <img class="interactive-img-preview" src="${hasImage ? escapeAttr(opt.imageUrl) : ""}" alt="" style="display:${hasImage ? "block" : "none"};" />
                    <span class="interactive-img-placeholder" style="display:${hasImage ? "none" : "block"};">Нет картинки</span>
                </div>
                <label class="btn btn-outline interactive-img-upload-btn">
                    <input type="file" class="interactive-img-file" accept=".jpg,.jpeg,.png,.gif,.webp,.bmp" hidden />
                    <span class="interactive-img-upload-text">${hasImage ? "Заменить" : "Загрузить"}</span>
                </label>
                <button type="button" class="interactive-row-remove" title="Удалить">×</button>
            </div>`;

        row.querySelector(".interactive-row-remove").addEventListener("click", async () => {
            const path = row.querySelector(".interactive-img-url")?.value?.trim();
            if (path && stepId) await deleteInteractiveImage(stepId, path);
            row.remove();
            syncHidden(wrap);
            updateImageAddButton(wrap);
        });

        row.querySelector(".interactive-img-label")?.addEventListener("input", () => syncHidden(wrap));
        row.querySelectorAll(".interactive-img-correct").forEach(r => {
            r.addEventListener("change", () => syncHidden(wrap));
        });

        row.querySelector(".interactive-img-file")?.addEventListener("change", async e => {
            const file = e.target.files?.[0];
            e.target.value = "";
            if (!file || !stepId) return;

            if (file.size > MAX_IMAGE_MB * 1024 * 1024) {
                alert(`Изображение больше ${MAX_IMAGE_MB} МБ`);
                return;
            }
            if (!imageExtOk(file.name)) {
                alert(`Разрешены: ${IMAGE_EXT.join(", ")}`);
                return;
            }

            const oldPath = row.querySelector(".interactive-img-url")?.value?.trim() || "";
            try {
                const imageUrl = await uploadInteractiveImage(stepId, file, oldPath || undefined);
                row.querySelector(".interactive-img-url").value = imageUrl;
                const uploadText = row.querySelector(".interactive-img-upload-text");
                if (uploadText) uploadText.textContent = "Заменить";
                updateImagePreview(row);
                syncHidden(wrap);
            } catch (err) {
                alert(err.message || "Не удалось загрузить");
            }
        });

        list.appendChild(row);
    }

    function countBlanks(template) {
        let n = 0, i = 0;
        while (i < template.length) {
            if (template.slice(i, i + 3) === "___") { n++; i += 3; } else i++;
        }
        return n;
    }

    function wireInteractiveInput(input, wrap) {
        if (!input) return;
        input.addEventListener("input", () => {
            input.classList.remove("input-invalid");
            syncHidden(wrap);
        });
    }

    function addPairRow(list, left, right) {
        const wrap = list.closest(".interactive-constructor");
        const row = document.createElement("div");
        row.className = "interactive-pair-row";
        row.innerHTML = `
            <input type="text" class="interactive-pair-left" placeholder="Термин" value="${escapeAttr(left)}">
            <span>→</span>
            <input type="text" class="interactive-pair-right" placeholder="Определение" value="${escapeAttr(right)}">
            <button type="button" class="interactive-row-remove" title="Удалить">×</button>`;
        row.querySelector(".interactive-row-remove").addEventListener("click", () => {
            row.remove();
            syncHidden(wrap);
        });
        wireInteractiveInput(row.querySelector(".interactive-pair-left"), wrap);
        wireInteractiveInput(row.querySelector(".interactive-pair-right"), wrap);
        list.appendChild(row);
    }

    function addItemRow(list, text) {
        const wrap = list.closest(".interactive-constructor");
        const row = document.createElement("div");
        row.className = "interactive-item-row";
        row.innerHTML = `
            <span class="interactive-item-num">${list.children.length + 1}.</span>
            <input type="text" class="interactive-item-text" placeholder="Элемент" value="${escapeAttr(text)}">
            <button type="button" class="interactive-row-remove" title="Удалить">×</button>`;
        row.querySelector(".interactive-row-remove").addEventListener("click", () => {
            row.remove();
            renumberItems(list);
            syncHidden(wrap);
        });
        wireInteractiveInput(row.querySelector(".interactive-item-text"), wrap);
        list.appendChild(row);
    }

    function renumberItems(list) {
        list.querySelectorAll(".interactive-item-row").forEach((row, i) => {
            const num = row.querySelector(".interactive-item-num");
            if (num) num.textContent = `${i + 1}.`;
        });
    }

    function addStatementRow(list, text, isTrue) {
        const wrap = list.closest(".interactive-constructor");
        const row = document.createElement("div");
        row.className = "interactive-stmt-row";
        row.innerHTML = `
            <input type="text" class="interactive-stmt-text" placeholder="Утверждение" value="${escapeAttr(text)}">
            <select class="interactive-stmt-truth">
                <option value="true" ${isTrue ? "selected" : ""}>Верно</option>
                <option value="false" ${!isTrue ? "selected" : ""}>Неверно</option>
            </select>
            <button type="button" class="interactive-row-remove" title="Удалить">×</button>`;
        row.querySelector(".interactive-row-remove").addEventListener("click", () => {
            row.remove();
            syncHidden(wrap);
        });
        wireInteractiveInput(row.querySelector(".interactive-stmt-text"), wrap);
        list.appendChild(row);
    }

    function clearInteractiveFieldErrors(wrap) {
        if (!wrap) return;
        wrap.querySelectorAll(".input-invalid").forEach(el => el.classList.remove("input-invalid"));
    }

    function validateDom(wrap) {
        if (!wrap) return "Интерактивное задание не настроено.";

        clearInteractiveFieldErrors(wrap);

        const kind = normalizeKind(wrap.querySelector(".interactive-kind-select")?.value);
        const instructionEl = wrap.querySelector(".interactive-instruction");
        const instruction = (instructionEl?.value || "").trim();

        if (!instruction) {
            instructionEl?.classList.add("input-invalid");
            return "Введите инструкцию для студента.";
        }

        if (kind === "match") {
            const rows = [...wrap.querySelectorAll(".interactive-pair-row")];
            let completeCount = 0;

            for (const row of rows) {
                const leftEl = row.querySelector(".interactive-pair-left");
                const rightEl = row.querySelector(".interactive-pair-right");
                const left = (leftEl?.value || "").trim();
                const right = (rightEl?.value || "").trim();

                if (!left && !right) continue;

                if (!left || !right) {
                    if (!left) leftEl?.classList.add("input-invalid");
                    if (!right) rightEl?.classList.add("input-invalid");
                    return "Заполните обе части пары или очистите строку.";
                }
                completeCount++;
            }

            if (completeCount === 0) {
                return "Добавьте хотя бы одну заполненную пару.";
            }
            return null;
        }

        if (kind === "sequence") {
            const rows = [...wrap.querySelectorAll(".interactive-item-row")];
            let filled = 0;

            for (const row of rows) {
                const input = row.querySelector(".interactive-item-text");
                const value = (input?.value || "").trim();
                if (!value) continue;
                filled++;
            }

            if (filled < 2) {
                return "Добавьте минимум 2 элемента для упорядочивания.";
            }
            return null;
        }

        if (kind === "truefalse") {
            const rows = [...wrap.querySelectorAll(".interactive-stmt-row")];
            let filled = 0;

            for (const row of rows) {
                const input = row.querySelector(".interactive-stmt-text");
                const value = (input?.value || "").trim();
                if (!value) continue;
                filled++;
            }

            if (filled === 0) {
                return "Добавьте хотя бы одно утверждение.";
            }
            return null;
        }

        if (kind === "fillblanks") {
            const template = (wrap.querySelector(".interactive-fill-template")?.value || "").trim();
            const blanks = (wrap.querySelector(".interactive-fill-blanks")?.value || "").split("\n").map(s => s.trim()).filter(Boolean);
            if (!template || !template.includes("___")) return "В тексте должен быть хотя бы один пропуск «___».";
            const gaps = countBlanks(template);
            if (blanks.length !== gaps) return `Ответов (${blanks.length}) должно быть столько же, сколько пропусков (${gaps}).`;
            return null;
        }

        if (kind === "imagechoice") {
            const rows = [...wrap.querySelectorAll(".interactive-img-opt-row")];
            const filled = rows.filter(r => (r.querySelector(".interactive-img-url")?.value || "").trim());
            if (filled.length < 2) return "Загрузите минимум 2 картинки.";
            if (filled.length > MAX_INTERACTIVE_IMAGES) return `Не более ${MAX_INTERACTIVE_IMAGES} картинок.`;
            if (!wrap.querySelector(".interactive-img-correct:checked")) return "Отметьте правильный вариант.";
            return null;
        }

        return "Неизвестный тип интерактивного задания.";
    }

    function syncHidden(wrap) {
        if (!wrap) return;
        const hidden = wrap.querySelector(".step-interactive-json");
        if (hidden) hidden.value = JSON.stringify(buildConfigFromWrap(wrap));
    }

    function buildConfigFromWrap(wrap) {
        const kind = normalizeKind(wrap.querySelector(".interactive-kind-select")?.value);
        const instruction = wrap.querySelector(".interactive-instruction")?.value?.trim() || "";

        if (kind === "match") {
            const pairs = [];
            wrap.querySelectorAll(".interactive-pair-row").forEach(row => {
                const left = row.querySelector(".interactive-pair-left")?.value?.trim();
                const right = row.querySelector(".interactive-pair-right")?.value?.trim();
                if (left && right) pairs.push({ left, right });
            });
            return { kind: "match", instruction, pairs };
        }

        if (kind === "sequence") {
            const items = [];
            wrap.querySelectorAll(".interactive-item-text").forEach(inp => {
                const v = inp.value?.trim();
                if (v) items.push(v);
            });
            return { kind: "sequence", instruction, items };
        }

        if (kind === "truefalse") {
            const statements = [];
            wrap.querySelectorAll(".interactive-stmt-row").forEach(row => {
                const text = row.querySelector(".interactive-stmt-text")?.value?.trim();
                const isTrue = row.querySelector(".interactive-stmt-truth")?.value === "true";
                if (text) statements.push({ text, isTrue });
            });
            return { kind: "truefalse", instruction, statements };
        }

        if (kind === "fillblanks") {
            const template = wrap.querySelector(".interactive-fill-template")?.value?.trim() || "";
            const blanks = (wrap.querySelector(".interactive-fill-blanks")?.value || "")
                .split("\n").map(s => s.trim()).filter(Boolean);
            return { kind: "fillblanks", instruction, template, blanks };
        }

        const options = [];
        let correctOptionId = wrap.querySelector(".interactive-img-correct:checked")?.value || "";
        wrap.querySelectorAll(".interactive-img-opt-row").forEach((row, idx) => {
            let id = row.querySelector(".interactive-img-id")?.value?.trim();
            const imageUrl = row.querySelector(".interactive-img-url")?.value?.trim();
            const label = row.querySelector(".interactive-img-label")?.value?.trim();
            if (!id) id = "opt" + idx;
            if (imageUrl) options.push({ id, label: label || ("Вариант " + (idx + 1)), imageUrl });
        });
        const question = wrap.querySelector(".interactive-img-question")?.value?.trim() || "";
        if (!correctOptionId && options.length) correctOptionId = options[0].id;
        return { kind: "imagechoice", instruction, question, options, correctOptionId };
    }

    function validateConfig(config) {
        if (!config || !config.kind) {
            return "Выберите тип интерактивного задания.";
        }

        const kind = normalizeKind(config.kind);

        if (!(config.instruction || "").trim()) {
            return "Введите инструкцию для студента.";
        }

        if (kind === "match") {
            const pairs = config.pairs || [];
            if (pairs.length === 0) return "Добавьте хотя бы одну заполненную пару.";
            return null;
        }

        if (kind === "sequence") {
            const items = (config.items || []).map(i => (i || "").trim()).filter(Boolean);
            if (items.length < 2) return "Добавьте минимум 2 элемента для упорядочивания.";
            return null;
        }

        if (kind === "truefalse") {
            const statements = (config.statements || []).filter(s => (s.text || "").trim());
            if (statements.length === 0) return "Добавьте хотя бы одно утверждение.";
            return null;
        }

        if (kind === "fillblanks") {
            const template = (config.template || "").trim();
            const blanks = (config.blanks || []).map(b => (b || "").trim()).filter(Boolean);
            if (!template.includes("___")) return "В тексте должен быть пропуск «___».";
            const gaps = countBlanks(template);
            if (blanks.length !== gaps) return "Число ответов должно совпадать с числом пропусков.";
            return null;
        }

        if (kind === "imagechoice") {
            const options = (config.options || []).filter(o => (o.imageUrl || "").trim());
            if (options.length < 2) return "Загрузите минимум 2 картинки.";
            if (options.length > MAX_INTERACTIVE_IMAGES) return `Не более ${MAX_INTERACTIVE_IMAGES} картинок.`;
            if (!config.correctOptionId) return "Отметьте правильный вариант.";
            return null;
        }

        return "Неизвестный тип интерактивного задания.";
    }

    function validateFromCard(card) {
        const wrap = card?.querySelector(".interactive-constructor");
        const domError = validateDom(wrap);
        if (domError) return domError;

        syncHidden(wrap);
        let config;
        try {
            config = JSON.parse(wrap.querySelector(".step-interactive-json")?.value || "{}");
        } catch {
            return "Ошибка конфигурации задания.";
        }

        return validateConfig(normalizeConfig(config));
    }

    function serializeFromCard(card) {
        const wrap = card?.querySelector(".interactive-constructor");
        if (!wrap) return card?.querySelector(".step-interactive-json")?.value || "";
        syncHidden(wrap);
        const raw = wrap.querySelector(".step-interactive-json")?.value || "{}";
        try {
            return JSON.stringify(normalizeConfig(JSON.parse(raw)));
        } catch {
            return raw;
        }
    }

    function escapeAttr(s) {
        return String(s || "")
            .replace(/&/g, "&amp;")
            .replace(/"/g, "&quot;")
            .replace(/</g, "&lt;");
    }
})();
