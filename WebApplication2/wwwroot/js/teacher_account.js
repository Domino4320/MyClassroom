document.addEventListener('DOMContentLoaded', () => {
    const stats = document.querySelectorAll('.stat-value');
    stats.forEach(stat => {
        const target = +stat.getAttribute('data-target');
        const count = () => {
            const current = +stat.innerText;
            const increment = target / 50;
            if (current < target) {
                stat.innerText = Math.ceil(current + increment);
                setTimeout(count, 30);
            } else {
                stat.innerText = target + "+";
            }
        };
        count();
    });
});

function editField(fieldName) {
    const modal = document.getElementById('editModal');
    const container = document.getElementById('inputContainer');
    const fieldInput = document.getElementById('editFieldName');
    const errorDiv = document.getElementById('fieldError');

    // Сброс ошибок при открытии
    errorDiv.style.display = 'none';
    errorDiv.innerText = '';

    fieldInput.value = fieldName;
    container.innerHTML = '';

    let currentValue = "";

    // Логика получения текущего значения из DOM
    if (fieldName === 'About') {
        currentValue = document.getElementById('aboutText').innerText;
        container.innerHTML = `<textarea id="editValue" rows="5">${currentValue}</textarea>`;
    } else if (fieldName === 'ExtraInfo') {
        currentValue = document.getElementById('extraInfoText').innerText.trim();
        container.innerHTML = `<textarea id="editValue" rows="5">${currentValue}</textarea>`;
    } else if (fieldName === 'CurrentJob') {
        currentValue = document.getElementById('currentJobText').innerText;
        container.innerHTML = `<input type="text" id="editValue" value="${currentValue}">`;
    } else if (fieldName === 'Experience') {
        currentValue = document.getElementById('experienceText').innerText;
        container.innerHTML = `<input type="number" id="editValue" value="${currentValue}">`;
    } else if (fieldName === 'TeacherTags') {
        // Собираем теги обратно в строку через запятую
        const tags = Array.from(document.querySelectorAll('#tagsContainer .tag')).map(t => t.innerText);
        currentValue = tags.join(', ');
        container.innerHTML = `<input type="text" id="editValue" value="${currentValue}">`;
    } else if (fieldName === 'PortfolioUrl') {
        currentValue = document.getElementById('portfolioUrl').getAttribute('href');
        container.innerHTML = `<input type="text" id="editValue" value="${currentValue}">`;
    }

    modal.style.display = 'block';
}

function closeModal() {
    document.getElementById('editModal').style.display = 'none';
}

async function saveChanges() {
    const fieldName = document.getElementById('editFieldName').value;
    const newValue = document.getElementById('editValue').value;
    const errorDiv = document.getElementById('fieldError');
    const saveBtn = document.getElementById('saveBtn');

    errorDiv.style.display = 'none';
    saveBtn.disabled = true;
    saveBtn.innerText = 'Сохранение...';

    try {
        // ИСПРАВЛЕННЫЙ ПУТЬ: TeacherProfile вместо Teacher
        const response = await fetch('/TeacherAccount/UpdateProfile', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ fieldName: fieldName, value: newValue })
        });

        if (response.ok) {
            location.reload();
        } else {
            const errorMessage = await response.text();
            errorDiv.innerText = errorMessage || "Ошибка валидации";
            errorDiv.style.display = 'block';
            document.getElementById('editValue').style.borderColor = '#ff4d4d';
        }
    } catch (err) {
        errorDiv.innerText = "Ошибка сети: проверьте соединение или адрес контроллера";
        errorDiv.style.display = 'block';
    } finally {
        saveBtn.disabled = false;
        saveBtn.innerText = 'Сохранить';
    }
}