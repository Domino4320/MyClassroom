document.addEventListener('DOMContentLoaded', function () {
    const form = document.querySelector('.teacher-form');
    if (!form) return;

    const inputs = form.querySelectorAll('[required]');

    // 1. Добавляем звездочки и контейнеры для ошибок
    inputs.forEach(input => {
        const group = input.closest('.input-group');
        const label = group.querySelector('label');

        // Звездочка
        if (label && !label.querySelector('.star')) {
            label.innerHTML += ' <span class="star" style="color: #ff4d4f;">*</span>';
        }

        // Контейнер для текста ошибки (если его еще нет)
        if (!group.querySelector('.error-message')) {
            const errorDiv = document.createElement('div');
            errorDiv.className = 'error-message';
            errorDiv.style.cssText = 'color: #ff4d4f; font-size: 0.8rem; margin-top: 5px; display: none;';
            errorDiv.innerText = 'Это поле обязательно для заполнения';
            group.appendChild(errorDiv);
        }
    });

    // 2. Валидация при отправке
    form.addEventListener('submit', function (e) {
        let hasError = false;

        inputs.forEach(input => {
            const group = input.closest('.input-group');
            const errorMessage = group.querySelector('.error-message');

            if (!input.value.trim()) {
                hasError = true;
                input.style.borderColor = '#ff4d4f';
                if (errorMessage) errorMessage.style.display = 'block';
            } else {
                input.style.borderColor = '';
                if (errorMessage) errorMessage.style.display = 'none';
            }
        });

        if (hasError) {
            e.preventDefault();
            // Скролл к первой ошибке
            const firstError = form.querySelector('.error-message[style*="display: block"]');
            if (firstError) firstError.parentElement.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }
    });

    // Убираем ошибку в процессе набора текста
    inputs.forEach(input => {
        input.addEventListener('input', function () {
            if (this.value.trim()) {
                this.style.borderColor = '';
                const err = this.closest('.input-group').querySelector('.error-message');
                if (err) err.style.display = 'none';
            }
        });
    });
});