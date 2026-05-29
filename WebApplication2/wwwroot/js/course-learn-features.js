(function () {
    "use strict";

    window.CourseLearnFeatures = {
        initInteractive: initInteractive,
        resetInteractive: resetInteractive,
        getInteractiveAnswer: getInteractiveAnswer,
        validateInteractiveClient: validateInteractiveClient
    };

    function parseConfig(json) {
        if (!json || !json.trim()) return null;
        try {
            const cfg = JSON.parse(json);
            if (!cfg.kind && !cfg.Kind) cfg.kind = "match";
            return normalizeConfig(cfg);
        } catch {
            return null;
        }
    }

    function normalizeConfig(cfg) {
        if (!cfg) return null;

        const kindRaw = (cfg.kind ?? cfg.Kind ?? "match").toLowerCase();
        const normalizedKind = kindRaw === "order" ? "sequence" : kindRaw;

        const DEFAULT_INSTRUCTIONS = {
            match: "Сопоставьте термины с определениями",
            sequence: "Расставьте элементы в правильном порядке",
            truefalse: "Определите, какие утверждения верны, а какие нет"
        };

        const instruction = (cfg.instruction ?? cfg.Instruction ?? "").trim();
        const allDefaults = Object.values(DEFAULT_INSTRUCTIONS);

        const normalized = {
            kind: normalizedKind,
            instruction: !instruction || allDefaults.includes(instruction)
                ? (DEFAULT_INSTRUCTIONS[normalizedKind] || DEFAULT_INSTRUCTIONS.match)
                : instruction,
            pairs: [],
            items: [],
            statements: []
        };

        const rawPairs = cfg.pairs ?? cfg.Pairs ?? [];
        if (Array.isArray(rawPairs)) {
            normalized.pairs = rawPairs
                .map(p => ({
                    left: String(p?.left ?? p?.Left ?? "").trim(),
                    right: String(p?.right ?? p?.Right ?? "").trim()
                }))
                .filter(p => p.left && p.right);
        }

        const rawItems = cfg.items ?? cfg.Items ?? [];
        if (Array.isArray(rawItems)) {
            normalized.items = rawItems
                .map(i => String(i ?? "").trim())
                .filter(Boolean);
        }

        const rawStatements = cfg.statements ?? cfg.Statements ?? [];
        if (Array.isArray(rawStatements)) {
            normalized.statements = rawStatements
                .map(s => ({
                    text: String(s?.text ?? s?.Text ?? "").trim(),
                    isTrue: !!(s?.isTrue ?? s?.IsTrue)
                }))
                .filter(s => s.text);
        }

        return normalized;
    }

    function resetInteractive(container) {
        if (!container?._interactiveConfigJson) return;
        initInteractive(container, container._interactiveConfigJson);
        const fb = document.getElementById("feedback-msg");
        if (fb) {
            fb.style.display = "none";
            fb.className = "";
            fb.innerText = "";
        }
        const btn = document.getElementById("main-btn");
        if (btn) btn.disabled = false;
    }

    function appendResetButton(block, container, label) {
        const actions = document.createElement("div");
        actions.className = "interactive-actions";
        const btn = document.createElement("button");
        btn.type = "button";
        btn.className = "interactive-reset-btn";
        btn.textContent = label || "↻ Начать заново";
        btn.addEventListener("click", () => resetInteractive(container));
        actions.appendChild(btn);
        block.appendChild(actions);
    }

    function initInteractive(container, configJson) {
        if (!container) return;
        if (configJson != null) container._interactiveConfigJson = configJson;
        container.innerHTML = "";
        const config = parseConfig(configJson);
        if (!config) {
            container.innerHTML = "<p class=\"interactive-error-msg\">Неверный формат задания. Обратитесь к преподавателю.</p>";
            return;
        }

        const block = document.createElement("div");
        block.className = "interactive-block";

        const titles = {
            match: "Сопоставление пар",
            sequence: "Расставьте по порядку",
            order: "Расставьте по порядку",
            truefalse: "Верно или неверно?"
        };
        const kind = config.kind;

        const title = document.createElement("h3");
        title.textContent = titles[kind] || "Интерактивное задание";
        block.appendChild(title);

        if (config.instruction) {
            const instr = document.createElement("p");
            instr.className = "interactive-instruction";
            instr.textContent = config.instruction;
            block.appendChild(instr);
        }

        if (kind === "match") {
            if (!config.pairs.length) {
                const err = document.createElement("p");
                err.className = "interactive-error-msg";
                err.textContent = "Задание не настроено: нет пар для сопоставления.";
                block.appendChild(err);
            } else {
                renderMatch(block, config, container);
            }
        } else if (kind === "sequence" || kind === "order") {
            if ((config.items || []).length < 2) {
                const err = document.createElement("p");
                err.className = "interactive-error-msg";
                err.textContent = "Задание не настроено: недостаточно элементов.";
                block.appendChild(err);
            } else {
                renderSequence(block, config, container);
            }
        } else if (kind === "truefalse") {
            if (!config.statements.length) {
                const err = document.createElement("p");
                err.className = "interactive-error-msg";
                err.textContent = "Задание не настроено: нет утверждений.";
                block.appendChild(err);
            } else {
                renderTrueFalse(block, config, container);
            }
        } else {
            block.innerHTML += "<p class=\"interactive-error-msg\">Неизвестный тип задания.</p>";
        }

        container.appendChild(block);
        container._interactiveConfig = config;
    }

    function unmatchPair(left, matches, termElements, rightCol) {
        const right = matches[left];
        if (!right) return;
        delete matches[left];
        const termEl = termElements.get(left);
        if (termEl) {
            termEl.classList.remove("matched", "selected");
        }
        const defEl = [...rightCol.querySelectorAll(".interactive-def")].find(d => d.dataset.right === right);
        if (defEl) defEl.classList.remove("matched");
    }

    function renderMatch(block, config, container) {
        const pairs = config.pairs || [];
        const leftItems = pairs.map(p => p.left);
        const rightItems = shuffle([...pairs.map(p => p.right)]);

        const grid = document.createElement("div");
        grid.className = "interactive-match-grid";

        const leftCol = document.createElement("div");
        leftCol.className = "interactive-col";
        leftCol.innerHTML = "<h4>Термины</h4>";

        const rightCol = document.createElement("div");
        rightCol.className = "interactive-col";
        rightCol.innerHTML = "<h4>Определения</h4>";

        const matches = {};
        let selectedLeft = null;
        const termElements = new Map();

        leftItems.forEach((left, index) => {
            const el = document.createElement("div");
            el.className = "interactive-term";
            el.textContent = left;
            el.dataset.left = left;
            el.dataset.leftIndex = String(index);
            el.title = "Нажмите, чтобы выбрать или отменить сопоставление";
            el.addEventListener("click", () => {
                if (matches[left]) {
                    unmatchPair(left, matches, termElements, rightCol);
                    selectedLeft = null;
                    block._matches = matches;
                    return;
                }
                leftCol.querySelectorAll(".interactive-term.selected").forEach(t => t.classList.remove("selected"));
                if (selectedLeft === left) {
                    selectedLeft = null;
                    return;
                }
                selectedLeft = left;
                el.classList.add("selected");
            });
            termElements.set(left, el);
            leftCol.appendChild(el);
        });

        rightItems.forEach(right => {
            const el = document.createElement("div");
            el.className = "interactive-def";
            el.textContent = right;
            el.dataset.right = right;
            el.title = "Нажмите, чтобы сопоставить или отменить";
            el.addEventListener("click", () => {
                if (el.classList.contains("matched")) {
                    const leftKey = Object.keys(matches).find(k => matches[k] === right);
                    if (leftKey) {
                        unmatchPair(leftKey, matches, termElements, rightCol);
                        selectedLeft = null;
                        block._matches = matches;
                    }
                    return;
                }
                if (!selectedLeft) return;
                matches[selectedLeft] = right;
                el.classList.add("matched");
                const termEl = termElements.get(selectedLeft);
                if (termEl) {
                    termEl.classList.remove("selected");
                    termEl.classList.add("matched");
                }
                selectedLeft = null;
                block._matches = matches;
            });
            rightCol.appendChild(el);
        });

        grid.appendChild(leftCol);
        grid.appendChild(rightCol);
        block.appendChild(grid);
        block._matches = matches;

        const hint = document.createElement("p");
        hint.className = "interactive-hint";
        hint.textContent = "Подсказка: нажмите на уже сопоставленную пару, чтобы отменить выбор.";
        block.appendChild(hint);

        appendResetButton(block, container, "↻ Сбросить все пары");
    }

    function renderSequence(block, config, container) {
        const items = shuffle([...(config.items || [])]);
        const list = document.createElement("ul");
        list.className = "interactive-order-list";
        list.id = "interactive-order-list";

        items.forEach(text => {
            const li = document.createElement("li");
            li.className = "interactive-order-item";
            li.draggable = true;
            li.dataset.text = text;
            li.innerHTML = `<span class="interactive-order-handle">☰</span><span>${escapeHtml(text)}</span>`;
            li.addEventListener("dragstart", () => li.classList.add("dragging"));
            li.addEventListener("dragend", () => li.classList.remove("dragging"));
            list.appendChild(li);
        });

        list.addEventListener("dragover", e => {
            e.preventDefault();
            const dragging = list.querySelector(".dragging");
            const after = getDragAfterElement(list, e.clientY);
            if (dragging && after == null) list.appendChild(dragging);
            else if (dragging && after) list.insertBefore(dragging, after);
        });

        block.appendChild(list);
        appendResetButton(block, container, "↻ Перемешать заново");
    }

    function renderTrueFalse(block, config, container) {
        const statements = config.statements || [];
        const answers = new Array(statements.length).fill(null);
        block._trueFalseAnswers = answers;

        const wrap = document.createElement("div");
        wrap.className = "interactive-tf-list";

        statements.forEach((stmt, index) => {
            const row = document.createElement("div");
            row.className = "interactive-tf-row";
            row.innerHTML = `
                <p class="interactive-tf-text">${escapeHtml(stmt.text)}</p>
                <div class="interactive-tf-btns">
                    <button type="button" class="interactive-tf-btn" data-val="true">Верно</button>
                    <button type="button" class="interactive-tf-btn" data-val="false">Неверно</button>
                </div>`;

            row.querySelectorAll(".interactive-tf-btn").forEach(btn => {
                btn.addEventListener("click", () => {
                    row.querySelectorAll(".interactive-tf-btn").forEach(b => b.classList.remove("selected"));
                    btn.classList.add("selected");
                    answers[index] = btn.dataset.val === "true";
                    block._trueFalseAnswers = answers;
                });
            });

            wrap.appendChild(row);
        });

        block.appendChild(wrap);
        appendResetButton(block, container, "↻ Сбросить ответы");
    }

    function getDragAfterElement(container, y) {
        const els = [...container.querySelectorAll(".interactive-order-item:not(.dragging)")];
        return els.reduce((closest, child) => {
            const box = child.getBoundingClientRect();
            const offset = y - box.top - box.height / 2;
            if (offset < 0 && offset > closest.offset) return { offset, element: child };
            return closest;
        }, { offset: Number.NEGATIVE_INFINITY }).element;
    }

    function getInteractiveAnswer(container) {
        const block = container?.querySelector(".interactive-block");
        if (!block) return null;

        const config = container._interactiveConfig;
        if (!config) return null;

        const kind = config.kind;

        if (kind === "match") {
            const matches = block._matches || {};
            const pairs = config.pairs || [];
            if (Object.keys(matches).length < pairs.length) return { incomplete: true };
            return JSON.stringify({ matches });
        }

        if (kind === "sequence" || kind === "order") {
            const list = block.querySelector("#interactive-order-list");
            if (!list) return { incomplete: true };
            const order = [...list.querySelectorAll(".interactive-order-item")].map(li => li.dataset.text);
            return JSON.stringify({ order });
        }

        if (kind === "truefalse") {
            const answers = block._trueFalseAnswers || [];
            if (answers.some(a => a === null)) return { incomplete: true };
            return JSON.stringify({ answers });
        }

        return null;
    }

    function validateInteractiveClient(container, configJson) {
        const config = parseConfig(configJson);
        if (!config) return "Неверный формат задания.";

        if (config.kind === "match" && !config.pairs.length) {
            return "Задание не настроено. Сообщите преподавателю.";
        }
        if ((config.kind === "sequence" || config.kind === "order") && config.items.length < 2) {
            return "Задание не настроено. Сообщите преподавателю.";
        }
        if (config.kind === "truefalse" && !config.statements.length) {
            return "Задание не настроено. Сообщите преподавателю.";
        }

        const answerStr = getInteractiveAnswer(container);
        if (!answerStr || (typeof answerStr === "object" && answerStr.incomplete)) {
            if (config.kind === "match") return "Сопоставьте все пары.";
            if (config.kind === "truefalse") return "Отметьте все утверждения.";
            return "Расставьте все элементы.";
        }

        let answer;
        try {
            answer = JSON.parse(answerStr);
        } catch {
            return "Ошибка ответа.";
        }

        if (config.kind === "match") {
            for (const p of config.pairs || []) {
                if (answer.matches?.[p.left] !== p.right) {
                    return "Не все пары верны. Попробуйте ещё раз.";
                }
            }
        } else if (config.kind === "sequence" || config.kind === "order") {
            const expected = config.items || [];
            const got = answer.order || [];
            if (got.length !== expected.length || got.some((v, i) => v !== expected[i])) {
                return "Порядок неверный. Попробуйте ещё раз.";
            }
        } else if (config.kind === "truefalse") {
            const stmts = config.statements || [];
            const got = answer.answers || [];
            if (got.length !== stmts.length || got.some((v, i) => v !== stmts[i].isTrue)) {
                return "Не все утверждения верны. Попробуйте ещё раз.";
            }
        }

        return null;
    }

    function shuffle(arr) {
        for (let i = arr.length - 1; i > 0; i--) {
            const j = Math.floor(Math.random() * (i + 1));
            [arr[i], arr[j]] = [arr[j], arr[i]];
        }
        return arr;
    }

    function escapeHtml(s) {
        const d = document.createElement("div");
        d.textContent = s;
        return d.innerHTML;
    }
})();
