import { apiGet } from './api.js';
import { loadSession, saveSession } from './session.js';
import { initRouter, showScreen } from './router.js';
import { initRegisterScreen } from './screens/register-screen.js';
import { initCharacterChoiceScreen } from './screens/character-choice-screen.js';
import { renderSelf, startLobbyPolling, stopLobbyPolling } from './screens/lobby-screen.js';
import { initCombatControls, startGamePolling, stopGamePolling } from './screens/combat-screen.js';
import { initInventoryScreen } from './screens/inventory-screen.js';
import { initCharacterEditorScreen } from './screens/character-editor-screen.js';
import { initActionCardsScreen, initActionCardsControls } from './screens/action-cards-screen.js';

const sessionRef = { current: null };

document.addEventListener('DOMContentLoaded', async () => {
    initRouter();

    window.addEventListener('route', async event => {
        const routeName = event.detail;
        showScreen(routeName);
        if (routeName === 'inventory') {
            await initInventoryScreen(sessionRef.current);
        } else if (routeName === 'character-editor') {
            await initCharacterEditorScreen(sessionRef.current);
        } else if (routeName === 'action-cards') {
            await initActionCardsScreen(sessionRef.current);
        }
    });

    initRegisterScreen(session => {
        sessionRef.current = session;
        enterLobby();
    });

    initCharacterChoiceScreen(sessionRef, session => {
        sessionRef.current = session;
        enterLobby();
    }, showScreen);

    initCombatControls(sessionRef);
    initActionCardsControls(sessionRef);

    const restored = await restoreSession();
    if (restored) {
        sessionRef.current = restored;
        if (restored.gameStarted && restored.hasCharacter) {
            enterGame();
        } else {
            enterLobby();
        }
    } else {
        showScreen('register');
    }
});

async function restoreSession() {
    const saved = loadSession();
    if (!saved?.playerId) {
        return null;
    }

    try {
        const restored = await apiGet(`/api/session/restore?playerId=${encodeURIComponent(saved.playerId)}`);
        if (restored.status !== 'success') {
            return null;
        }

        saveSession(restored);
        return restored;
    } catch (error) {
        console.error(error);
        return null;
    }
}

function enterLobby() {
    stopGamePolling();
    renderSelf(sessionRef.current);
    showScreen('lobby');
    startLobbyPolling(sessionRef.current, state => {
        const myState = state.players.find(player => player.id === sessionRef.current.playerId);
        if (myState) {
            sessionRef.current = { ...sessionRef.current, ...myState, gameStarted: state.gameStarted };
            saveSession(sessionRef.current);
            renderSelf(sessionRef.current);
        }

        document.getElementById('lobby-title').textContent = state.selectedCampaign
            ? 'Кампания выбрана'
            : 'Ожидание выбора кампании';
        document.getElementById('lobby-subtitle').textContent = state.selectedCampaign
            ? 'Подготовьте персонажа и ждите старта.'
            : 'GM выбирает кампанию. Игроки пока могут подготовить персонажей.';

        const actions = document.getElementById('character-actions');
        actions.classList.toggle('hidden', Boolean(sessionRef.current.hasCharacter));

        if (state.gameStarted && sessionRef.current.hasCharacter) {
            enterGame();
        }
    });
}

function enterGame() {
    stopLobbyPolling();
    showScreen('game');
    startGamePolling(sessionRef.current);
}
