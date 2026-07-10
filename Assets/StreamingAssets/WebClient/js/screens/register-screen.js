import { apiPost } from '../api.js';
import { saveSession } from '../session.js';

function readImageAsDataUrl(file) {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = event => resolve(event.target.result);
        reader.onerror = reject;
        reader.readAsDataURL(file);
    });
}

export function initRegisterScreen(onRegistered) {
    const form = document.getElementById('register-form');
    const nameInput = document.getElementById('player-name');
    const photoInput = document.getElementById('player-photo');
    const preview = document.getElementById('photo-preview');
    const button = document.getElementById('register-button');
    let photoBase64 = null;

    photoInput.addEventListener('change', async () => {
        const file = photoInput.files[0];
        if (!file) {
            photoBase64 = null;
            preview.classList.add('hidden');
            return;
        }

        photoBase64 = await readImageAsDataUrl(file);
        preview.style.backgroundImage = `url('${photoBase64}')`;
        preview.classList.remove('hidden');
    });

    form.addEventListener('submit', async event => {
        event.preventDefault();
        if (!nameInput.value.trim() || !photoBase64) {
            alert('Введите имя и выберите фото.');
            return;
        }

        button.disabled = true;
        button.textContent = 'Подключение...';

        try {
            const session = await apiPost('/api/session/register', {
                name: nameInput.value.trim(),
                photoBase64
            });
            saveSession(session);
            onRegistered(session);
        } catch (error) {
            console.error(error);
            alert('Не удалось подключиться к столу.');
        } finally {
            button.disabled = false;
            button.textContent = 'Войти';
        }
    });
}
