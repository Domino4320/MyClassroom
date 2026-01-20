const form = document.getElementById('registerForm');

form.addEventListener('submit', function (e) {
    e.preventDefault();

    // Получаем поля
    const login = document.getElementById("login");
    const password = document.getElementById('password');
    const username = document.getElementById("username");
    const confirm = document.getElementById('confirmPassword');

    // Сброс старых ошибок
    resetErrors([login, password, confirm, username]);

    let hasError = false;

    // Проверка логина
    if (login.value.length < 8) {
        showError(login, 'Длина логина должна быть не менее 8 символов');
        hasError = true;
    }

    // Проверка пароля
    if (password.value.length < 8) {
        showError(password, 'Длина пароля должна быть не менее 8 символов');
        hasError = true;
    }

    // Проверка совпадения паролей
    if (password.value !== confirm.value) {
        showError(confirm, 'Пароли не совпадают!');
        showError(password, 'Пароли не совпадают!');
        hasError = true;
    }

    if (password.value.includes(" ")) {
        showError(password, "Пароль содержит пробелы");
        hasError = true;
    }

    if (login.value.includes(" ")) {
        showError(login, "Логин содержит пробелы");
        hasError = true;
    }

    if (username.value.includes(" ")) {
        showError(username, "Имя пользователя содержит пробелы");
        hasError = true;
    }



    

    if (hasError) {
        // Анимация тряски карточки
        const card = document.querySelector('.register-card');
        card.style.animation = 'shake 0.4s';
        setTimeout(() => card.style.animation = '', 400);
        return;
    }

    else {
        document.getElementById("registerForm").submit();
    }

});

// Функция показа ошибки под полем
function showError(field, message) {
    field.classList.add('input-error');

    // Создаем сообщение, если его нет
    let error = field.parentNode.querySelector('.error-message');
    if (!error) {
        error = document.createElement('div');
        error.className = 'error-message';
        field.parentNode.appendChild(error);
    }
    error.textContent = message;
}

// Функция сброса ошибок
function resetErrors(fields) {
    fields.forEach(field => {
        field.classList.remove('input-error');
        const error = field.parentNode.querySelector('.error-message');
        if (error) error.remove();
    });
}

/* Shake animation */
const style = document.createElement('style');
style.innerHTML = `
@keyframes shake {
    0% { transform: translateX(0); }
    25% { transform: translateX(-8px); }
    50% { transform: translateX(8px); }
    75% { transform: translateX(-8px); }
    100% { transform: translateX(0); }
}

/* Красная рамка для ошибки */
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
