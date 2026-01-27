const form = document.getElementById('registerForm');

// Проверка ошибок от сервера при загрузке
window.addEventListener('DOMContentLoaded', () => {
    const fields = document.querySelectorAll('.form-group input');
    let hasServerError = false;

    fields.forEach(field => {
        const error = field.parentNode.querySelector('.error-message, .field-error');
        if (error && error.textContent.trim() !== '') {
            field.classList.add('input-error');
            hasServerError = true;

            // Преобразуем field-error в error-message
            if (error.classList.contains('field-error')) {
                error.classList.remove('field-error');
                error.classList.add('error-message');
            }
        }
    });

    if (hasServerError) {
        const card = document.querySelector('.register-card');
        card.style.animation = 'shake 0.4s';
        setTimeout(() => card.style.animation = '', 400);
    }
});

// Клиентская проверка формы перед отправкой
form.addEventListener('submit', function (e) {
    e.preventDefault();

    const inputs = form.querySelectorAll('input');
    resetErrors(inputs);

    let hasError = false;

    inputs.forEach(field => {
        if (field.value.trim() === "") {
            showError(field, 'Поле не должно быть пустым');
            hasError = true;
        }
        if (field.value.includes(" ")) {
            showError(field, 'Поле не должно содержать пробелы');
            hasError = true;
        }
        if (field.value.length < 8) {
            showError(field, "Длина содержимого поля не может быть менее 8 символов");
            hasError = true;
        }
    });

    if (hasError) {
        const card = document.querySelector('.register-card');
        card.style.animation = 'shake 0.4s';
        setTimeout(() => card.style.animation = '', 400);
        return;
    }

    form.submit();
});

// Функции работы с ошибками
function showError(field, message) {
    field.classList.add('input-error');
    let error = field.parentNode.querySelector('.error-message');
    if (!error) {
        error = document.createElement('div');
        error.className = 'error-message';
        field.parentNode.appendChild(error);
    }
    error.textContent = message;
}

function resetErrors(fields) {
    fields.forEach(field => {
        field.classList.remove('input-error');
        const error = field.parentNode.querySelector('.error-message');
        if (error) error.remove();
    });
}

// CSS для тряски и подсветки ошибок
const style = document.createElement('style');
style.innerHTML = `
@keyframes shake {
    0% { transform: translateX(0); }
    25% { transform: translateX(-8px); }
    50% { transform: translateX(8px); }
    75% { transform: translateX(-8px); }
    100% { transform: translateX(0); }
}

.input-error {
    border-color: #f44336 !important;
    box-shadow: 0 0 8px rgba(244,67,54,0.6);
}

.error-message {
    color: #f44336;
    font-size: 0.8rem;
    margin-top: 0.3rem;
    font-weight: 500;
}
`;
document.head.appendChild(style);
