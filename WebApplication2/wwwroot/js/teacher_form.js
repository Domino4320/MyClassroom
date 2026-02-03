document.addEventListener('DOMContentLoaded', function () {
    const specSelect = document.getElementById('specializationSelect');
const otherGroup = document.getElementById('otherSpecializationGroup');
const customInput = document.getElementById('customSpecialization');

specSelect.addEventListener('change', function () {
        if (this.value === 'Other') {
    otherGroup.style.display = 'block';
customInput.setAttribute('required', 'required');
        } else {
    otherGroup.style.display = 'none';
customInput.removeAttribute('required');
customInput.value = ''; // очищаем при переключении
        }
    });
});