(function () {
    "use strict";

    const KIND_LABELS = {
        match: "Сопоставление пар",
        sequence: "Последовательность",
        truefalse: "Верно / Неверно"
    };

    const DEFAULT_INSTRUCTIONS = {
        match: "Сопоставьте термины с определениями",
        sequence: "Расставьте элементы в правильном порядке",
        truefalse: "Определите, какие утверждения верны, а какие нет"
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
                }))
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
        }
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

        const statements = [];
        wrap.querySelectorAll(".interactive-stmt-row").forEach(row => {
            const text = row.querySelector(".interactive-stmt-text")?.value?.trim();
            const isTrue = row.querySelector(".interactive-stmt-truth")?.value === "true";
            if (text) statements.push({ text, isTrue });
        });
        return { kind: "truefalse", instruction, statements };
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
