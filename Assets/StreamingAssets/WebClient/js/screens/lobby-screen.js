import { apiGet } from '../api.js';
import { renderPlayerCard } from '../components/player-card.js';

let lobbyTimer = null;

export function stopLobbyPolling() {
    if (lobbyTimer) {
        clearTimeout(lobbyTimer);
        lobbyTimer = null;
    }
}

export function renderSelf(session) {
    document.getElementById('self-name').textContent = session.name || 'Игрок';
    document.getElementById('self-status').textContent = session.hasCharacter
        ? session.isReady ? 'Готов' : 'Персонаж выбран'
        : 'Без персонажа';

    const photo = document.getElementById('self-photo');
    photo.style.backgroundImage = session.portraitUrl ? `url('${session.portraitUrl}')` : '';
}

export function startLobbyPolling(session, onState) {
    stopLobbyPolling();

    const tick = async () => {
        try {
            const state = await apiGet('/api/lobby/state');
            const list = document.getElementById('lobby-players');
            list.innerHTML = '';
            state.players.forEach(player => list.appendChild(renderPlayerCard(player)));
            onState(state);
        } catch (error) {
            console.error(error);
        }

        lobbyTimer = setTimeout(tick, 2000);
    };

    renderSelf(session);
    tick();
}
