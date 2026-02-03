const form = document.getElementById('registerForm');

// === При загрузке страницы проверяем ошибки от сервера ===
window.addEventListener('DOMContentLoaded', () => {
    const fields = document.querySelectorAll('.form-group input');

    let hasServerError = false;

    fields.forEach(field => {
        const error = field.parentNode.querySelector('.error-message, .field-error');
        if (error && error.textContent.trim() !== '') {
            field.classList.add('input-error');
            hasServerError = true;

            // Если это серверная ошибка (field-error), превращаем её в .error-message для единообразия
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

// === Обработка сабмита формы на клиенте ===
// === Обработка сабмита формы на клиенте ===
form.addEventListener('submit', function (e) {
    e.preventDefault();

    const login = document.getElementById("Login");
    const password = document.getElementById('Password');
    const username = document.getElementById("Username");
    const confirm = document.getElementById('confirmPassword');

    // Сброс старых ошибок
    resetErrors([login, password, confirm, username]);

    let hasError = false;

    // Регулярное выражение для поиска кириллицы
    const cyrillicPattern = /[а-яё]/i;

    // === Проверки на длину ===
    if (username.value.length < 8) {
        showError(username, 'Длина имени пользователя должна быть не менее 8 символов');
        hasError = true;
    }

    if (login.value.length < 8) {
        showError(login, 'Длина логина должна быть не менее 8 символов');
        hasError = true;
    }

    if (password.value.length < 8) {
        showError(password, 'Длина пароля должна быть не менее 8 символов');
        hasError = true;
    }

    // === Проверка на кириллицу (только для логина и пароля) ===
    if (cyrillicPattern.test(login.value)) {
        showError(login, 'Логин может содержать только латинские символы');
        hasError = true;
    }

    if (cyrillicPattern.test(password.value)) {
        showError(password, 'Пароль не может содержать кириллицу');
        hasError = true;
    }

    // === Проверка на совпадение паролей ===
    if (password.value !== confirm.value) {
        showError(confirm, 'Пароли не совпадают!');
        showError(password, 'Пароли не совпадают!');
        hasError = true;
    }

    // === Проверка на пробелы ===
    if (login.value.includes(" ")) {
        showError(login, "Логин содержит пробелы");
        hasError = true;
    }

    if (password.value.includes(" ")) {
        showError(password, "Пароль содержит пробелы");
        hasError = true;
    }

    if (username.value.includes(" ")) {
        showError(username, "Имя пользователя содержит пробелы");
        hasError = true;
    }

    if (hasError) {
        const card = document.querySelector('.register-card');
        card.style.animation = 'shake 0.4s';
        setTimeout(() => card.style.animation = '', 400);
        return;
    }

    // Если ошибок нет — отправляем форму
    form.submit();
});

// === Функции ===
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

// === CSS для тряски и ошибок ===
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
