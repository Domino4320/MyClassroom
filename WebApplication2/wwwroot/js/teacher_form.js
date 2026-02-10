document.addEventListener('DOMContentLoaded', function () {
    const form = document.querySelector('.teacher-form');
    if (!form) return;

    const inputs = form.querySelectorAll('[required]');
    const tagInput = document.getElementById('tagInput');
    const tagsContainer = document.getElementById('tagsContainer');
    const hiddenTagsInput = document.getElementById('hiddenTagsInput');
    let tags = [];

    // --- ЛОГИКА ТЕГОВ ---

    if (tagInput && tagsContainer) {
        tagInput.addEventListener('keydown', function (e) {
            if (e.key === 'Enter' || e.key === ',') {
                e.preventDefault();
                let val = tagInput.value.trim().replace(/[^a-zA-Zа-яА-Я0-9\s-]/g, '');

                if (val && !tags.includes(val) && tags.length < 10) {
                    tags.push(val);
                    renderTags();
                    tagInput.value = '';
                }
            }

            if (e.key === 'Backspace' && !tagInput.value && tags.length > 0) {
                tags.pop();
                renderTags();
            }
        });
    }

    function renderTags() {
        tagsContainer.querySelectorAll('.tag-item').forEach(el => el.remove());
        tags.forEach((tag, index) => {
            const tagEl = document.createElement('div');
            tagEl.className = 'tag-item';
            tagEl.innerHTML = `${tag} <span data-index="${index}">&times;</span>`;
            tagsContainer.insertBefore(tagEl, tagInput);
        });
        hiddenTagsInput.value = tags.join(',');
    }

    tagsContainer?.addEventListener('click', (e) => {
        if (e.target.tagName === 'SPAN') {
            const index = e.target.getAttribute('data-index');
            tags.splice(index, 1);
            renderTags();
        }
    });

    // --- ВАЛИДАЦИЯ ССЫЛОК ---

    function isValidProfessionalLink(url) {
        try {
            const parsedUrl = new URL(url);
            const host = parsedUrl.hostname.toLowerCase();
            return host.includes('github.com') || host.includes('linkedin.com');
        } catch (e) {
            return false;
        }
    }

    // --- ОБЩАЯ ВАЛИДАЦИЯ ФОРМЫ ---

    // 1. Подготовка: добавляем звездочки и контейнеры ошибок
    const allFormInputs = form.querySelectorAll('input, textarea, select');
    allFormInputs.forEach(input => {
        const group = input.closest('.input-group');
        if (!group) return;

        const label = group.querySelector('label');

        if (input.hasAttribute('required') && label && !label.querySelector('.star')) {
            label.innerHTML += ' <span class="star" style="color: #ff4d4f;">*</span>';
        }

        if (!group.querySelector('.error-message')) {
            const errorDiv = document.createElement('div');
            errorDiv.className = 'error-message';
            errorDiv.style.cssText = 'color: #ff4d4f; font-size: 0.8rem; margin-top: 5px; display: none;';
            group.appendChild(errorDiv);
        }
    });

    // 2. Валидация при отправке
    form.addEventListener('submit', function (e) {
        let hasError = false;

        allFormInputs.forEach(input => {
            const group = input.closest('.input-group');
            if (!group) return;

            const errorMessage = group.querySelector('.error-message');
            let isInvalid = false;
            let errorText = 'Это поле обязательно для заполнения';

            // Проверка обязательных полей
            if (input.hasAttribute('required') && !input.value.trim()) {
                isInvalid = true;
            }
            // Проверка формата ссылки (даже если поле не обязательное, но заполнено)
            else if (input.type === 'url' && input.value.trim() !== '') {
                if (!isValidProfessionalLink(input.value.trim())) {
                    isInvalid = true;
                    errorText = 'Разрешены только ссылки на GitHub или LinkedIn';
                }
            }

            if (isInvalid) {
                hasError = true;
                input.style.borderColor = '#ff4d4f';
                if (errorMessage) {
                    errorMessage.innerText = errorText;
                    errorMessage.style.display = 'block';
                }
            } else {
                input.style.borderColor = '';
                if (errorMessage) errorMessage.style.display = 'none';
            }
        });

        if (hasError) {
            e.preventDefault();
            const firstError = form.querySelector('.error-message[style*="display: block"]');
            if (firstError) firstError.parentElement.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }
    });

    // Убираем ошибку при вводе
    allFormInputs.forEach(input => {
        input.addEventListener('input', function () {
            this.style.borderColor = '';
            const err = this.closest('.input-group')?.querySelector('.error-message');
            if (err) err.style.display = 'none';
        });
    });
});