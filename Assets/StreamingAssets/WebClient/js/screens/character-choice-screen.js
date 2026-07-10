import { apiGet, apiPost } from '../api.js';
import { saveSession } from '../session.js';

export function initCharacterChoiceScreen(sessionRef, onImported, showScreen) {
    document.getElementById('open-import-button').addEventListener('click', async () => {
        await loadCharacters(sessionRef, onImported);
        showScreen('import');
    });

    document.getElementById('open-create-button').addEventListener('click', () => {
        showScreen('character-editor');
    });
}

async function loadCharacters(sessionRef, onImported) {
    const list = document.getElementById('character-list');
    list.innerHTML = '<div class="panel">Загрузка...</div>';

    try {
        const characters = await apiGet('/api/characters');
        list.innerHTML = '';

        if (characters.length === 0) {
            list.innerHTML = '<div class="panel">Сохранённых персонажей пока нет.</div>';
            return;
        }

        characters.forEach(character => {
            const button = document.createElement('button');
            button.className = 'character-card';
            button.type = 'button';

            const avatar = document.createElement('div');
            avatar.className = 'avatar';
            if (character.portraitUrl) {
                avatar.style.backgroundImage = `url('${character.portraitUrl}')`;
            }

            const body = document.createElement('div');
            const name = document.createElement('strong');
            name.textContent = character.name;
            const level = document.createElement('span');
            level.className = 'muted';
            level.textContent = `Уровень ${character.level || 1}`;
            body.append(name, level);

            button.append(avatar, body);
            button.addEventListener('click', async () => {
                const usePlayerPhoto = document.getElementById('use-player-photo').checked;
                await apiPost('/api/character/import', {
                    playerId: sessionRef.current.playerId,
                    characterPath: character.id,
                    usePlayerPhoto
                });
                const restored = await apiGet(`/api/session/restore?playerId=${encodeURIComponent(sessionRef.current.playerId)}`);
                sessionRef.current = restored;
                saveSession(restored);
                onImported(restored);
            });

            list.appendChild(button);
        });
    } catch (error) {
        console.error(error);
        list.innerHTML = '<div class="panel">Не удалось загрузить персонажей.</div>';
    }
}
