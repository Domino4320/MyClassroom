document.addEventListener('DOMContentLoaded', () => {
    // Анимация цифр
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

    // Закрытие модалки при клике на темный фон
    const modal = document.getElementById('editModal');
    if (modal) {
        modal.addEventListener('click', (e) => {
            if (e.target === modal) {
                closeModal();
            }
        });
    }
});

function editField(fieldName) {
    const modal = document.getElementById('editModal');
    const container = document.getElementById('inputContainer');
    const fieldInput = document.getElementById('editFieldName');
    const errorDiv = document.getElementById('fieldError');

    if (!modal || !container) return;

    errorDiv.style.display = 'none';
    errorDiv.innerText = '';
    fieldInput.value = fieldName;
    container.innerHTML = '';

    let currentValue = "";

    if (fieldName === 'About') {
        currentValue = document.getElementById('aboutText')?.innerText || "";
        container.innerHTML = `<textarea id="editValue" rows="5">${currentValue}</textarea>`;
    } else if (fieldName === 'ExtraInfo') {
        currentValue = document.getElementById('extraInfoText')?.innerText.trim() || "";
        container.innerHTML = `<textarea id="editValue" rows="5">${currentValue}</textarea>`;
    } else if (fieldName === 'CurrentJob') {
        currentValue = document.getElementById('currentJobText')?.innerText || "";
        container.innerHTML = `<input type="text" id="editValue" value="${currentValue}">`;
    } else if (fieldName === 'Experience') {
        currentValue = document.getElementById('experienceText')?.innerText || "0";
        container.innerHTML = `<input type="number" id="editValue" value="${currentValue}">`;
    } else if (fieldName === 'TeacherTags') {
        const tags = Array.from(document.querySelectorAll('#tagsContainer .tag')).map(t => t.innerText);
        currentValue = tags.join(', ');
        container.innerHTML = `<input type="text" id="editValue" placeholder="Напр: C#, SQL" value="${currentValue}">`;
    } else if (fieldName === 'PortfolioUrl') {
        const link = document.getElementById('portfolioUrl');
        currentValue = link ? link.getAttribute('href') : "";
        container.innerHTML = `<input type="text" id="editValue" value="${currentValue}">`;
    }

    modal.classList.add('active');
}

function closeModal() {
    const modal = document.getElementById('editModal');
    if (modal) modal.classList.remove('active');
}

async function saveChanges() {
    const fieldName = document.getElementById('editFieldName').value;
    const inputElement = document.getElementById('editValue');
    const newValue = inputElement.value;
    const errorDiv = document.getElementById('fieldError');
    const saveBtn = document.getElementById('saveBtn');

    errorDiv.style.display = 'none';
    inputElement.style.borderColor = '';
    saveBtn.disabled = true;
    saveBtn.innerText = 'Сохранение...';

    try {
        const response = await fetch('/TeacherAccount/UpdateProfile', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ fieldName: fieldName, value: newValue })
        });

        if (response.ok) {
            location.reload();
        } else {
            const errorMessage = await response.text();
            errorDiv.innerText = errorMessage || "Ошибка при сохранении";
            errorDiv.style.display = 'block';
            inputElement.style.borderColor = '#ff4d4d';
        }
    } catch (err) {
        errorDiv.innerText = "Ошибка сети.";
        errorDiv.style.display = 'block';
    } finally {
        saveBtn.disabled = false;
        saveBtn.innerText = 'Сохранить';
    }
} 