import { apiGet, apiPost } from '../api.js';
import { showDiceWidget } from '../components/dice-widget.js';
import { saveSession } from '../session.js';

let gameTimer = null;
let currentTargetId = null;
let currentSessionRef = null;
let isGamePollingActive = false;

export function startGamePolling(session) {
    stopGamePolling();
    isGamePollingActive = true;
    document.getElementById('game-name').textContent = session.name || 'Игрок';
    document.getElementById('game-photo').style.backgroundImage = session.portraitUrl ? `url('${session.portraitUrl}')` : '';

    const tick = async () => {
        if (!isGamePollingActive) return;
        try {
            if (currentSessionRef && currentSessionRef.current) {
                const state = await apiGet(`/api/game/state?playerId=${encodeURIComponent(currentSessionRef.current.playerId)}`);
                if (!isGamePollingActive) return;
                renderGameState(state);
            }
        } catch (error) {
            console.error(error);
        }

        if (isGamePollingActive) {
            gameTimer = setTimeout(tick, 2000);
        }
    };

    tick();
}

export function stopGamePolling() {
    isGamePollingActive = false;
    if (gameTimer) {
        clearTimeout(gameTimer);
        gameTimer = null;
    }
}

export function initCombatControls(sessionRef) {
    currentSessionRef = sessionRef;
    ensureLevelProgressButton();

    document.querySelectorAll('[data-move]').forEach(button => {
        let intervalId = null;
        const [dx, dy] = button.dataset.move.split(',').map(Number);

        const send = () => {
            if (!sessionRef.current?.playerId) return;
            apiPost('/api/action/move', {
                playerId: sessionRef.current.playerId,
                dirX: dx,
                dirY: dy
            }).catch(console.error);
        };

        const start = event => {
            event.preventDefault();
            if (intervalId !== null) return;
            send();
            intervalId = setInterval(send, 300);
        };

        const stop = () => {
            if (intervalId !== null) {
                clearInterval(intervalId);
                intervalId = null;
            }
        };

        button.addEventListener('mousedown', start);
        button.addEventListener('touchstart', start, { passive: false });
        button.addEventListener('mouseup', stop);
        button.addEventListener('mouseleave', stop);
        button.addEventListener('touchend', stop);
        button.addEventListener('touchcancel', stop);
    });

    document.getElementById('transition-confirm').addEventListener('click', () => sendTransition(sessionRef, 'confirm'));
    document.getElementById('transition-cancel').addEventListener('click', () => sendTransition(sessionRef, 'cancel'));
    document.getElementById('attack-confirm').addEventListener('click', () => sendAttack(sessionRef));
    document.getElementById('attack-cancel').addEventListener('click', () => toggleModal('attack-modal', false));
    document.getElementById('focus-camera-button').addEventListener('click', () => focusCamera(sessionRef));
    document.getElementById('inventory-button').addEventListener('click', () => window.dispatchEvent(new CustomEvent('route', { detail: 'inventory' })));
    document.getElementById('settings-button').addEventListener('click', () => window.dispatchEvent(new CustomEvent('route', { detail: 'character-editor' })));
    
    // Switch weapon and End Turn buttons
    document.getElementById('weapon-button').addEventListener('click', () => switchWeapon(sessionRef));
    document.getElementById('end-turn-button').addEventListener('click', () => endTurn(sessionRef));
}

async function switchWeapon(sessionRef) {
    if (!sessionRef.current?.playerId) return;
    try {
        const response = await apiPost('/api/action/switch-weapon', {
            playerId: sessionRef.current.playerId
        });
        if (response.status === 'success') {
            const state = await apiGet(`/api/game/state?playerId=${encodeURIComponent(sessionRef.current.playerId)}`);
            renderGameState(state);
        }
    } catch (error) {
        console.error(error);
    }
}

async function endTurn(sessionRef) {
    if (!sessionRef.current?.playerId) return;
    try {
        const response = await apiPost('/api/action/end-turn', {
            playerId: sessionRef.current.playerId
        });
        if (response.status === 'success') {
            const state = await apiGet(`/api/game/state?playerId=${encodeURIComponent(sessionRef.current.playerId)}`);
            renderGameState(state);
        }
    } catch (error) {
        console.error(error);
    }
}

function ensureLevelProgressButton() {
    if (document.getElementById('level-progress-button')) return;

    const focusButton = document.getElementById('focus-camera-button');
    if (!focusButton || !focusButton.parentNode) return;

    const button = document.createElement('button');
    button.id = 'level-progress-button';
    button.className = 'icon-button level-progress-button hidden';
    button.type = 'button';
    button.title = 'Распределить очки';
    button.textContent = '↑';
    button.addEventListener('click', () => {
        window.dispatchEvent(new CustomEvent('route', { detail: 'character-editor' }));
    });

    focusButton.parentNode.insertBefore(button, focusButton);
}

function renderGameState(state) {
    console.log("[CombatScreen] renderGameState state:", state);
    document.getElementById('stat-hp').textContent = valuePair(state.hp, state.maxHp);
    document.getElementById('stat-armor').textContent = valuePair(state.armor, state.maxArmor);
    document.getElementById('stat-move').textContent = valuePair(state.movement, state.maxMovement);
    document.getElementById('stat-rolls').textContent = valuePair(state.rolls, state.maxRolls);
    document.getElementById('game-state').textContent = `Уровень ${state.level || 1}`;

    const levelProgressButton = document.getElementById('level-progress-button');
    if (levelProgressButton) {
        levelProgressButton.classList.toggle('hidden', !state.hasUnspentProgress);
    }

    document.getElementById('turn-state').textContent = state.isCombatActive
        ? state.isMyTurn ? 'Ваш ход' : 'Ход другого участника'
        : 'Свободное перемещение';
    document.getElementById('active-weapon').textContent = `Оружие: ${state.activeWeapon || '-'}`;

    const endTurnBtn = document.getElementById('end-turn-button');
    if (state.isCombatActive && state.isMyTurn) {
        endTurnBtn.classList.remove('hidden');
    } else {
        endTurnBtn.classList.add('hidden');
    }

    const transitionModal = document.getElementById('transition-modal');
    if (state.prompt) {
        document.getElementById('transition-text').textContent = state.prompt;
        transitionModal.classList.add('active');
    } else {
        transitionModal.classList.remove('active');
    }

    // Check for pending roll
    const diceModal = document.getElementById('dice-modal');
    if (state.pendingRoll && currentSessionRef && currentSessionRef.current) {
        if (state.pendingRoll.playerId === currentSessionRef.current.playerId) {
            if (!diceModal.classList.contains('active')) {
                showDiceWidget(state.pendingRoll, currentSessionRef.current, async (rollResult) => {
                    await apiPost('/api/roll/submit', {
                        playerId: currentSessionRef.current.playerId,
                        rollResult
                    });
                    await forceRefreshState();
                }, (updatedSession) => {
                    currentSessionRef.current = updatedSession;
                    saveSession(updatedSession);
                });
            }
        }
    } else {
        diceModal.classList.remove('active');
    }

    const targets = document.getElementById('targets-container');
    targets.innerHTML = '';
    (state.enemies || []).forEach(enemy => {
        const target = document.createElement('button');
        target.className = 'target-avatar';
        if (enemy.team) {
            target.classList.add('team-' + enemy.team.toLowerCase());
        } else {
            target.classList.add('team-enemy');
        }
        target.type = 'button';
        target.title = enemy.name;
        if (enemy.portraitUrl) {
            target.style.backgroundImage = `url('${enemy.portraitUrl}')`;
        }
        target.addEventListener('click', () => {
            if (state.isCombatActive && !state.isMyTurn) {
                alert("Сейчас не ваш ход!");
                return;
            }
            currentTargetId = enemy.id;
            document.getElementById('attack-target-name').textContent = `Атаковать: ${enemy.name}?`;
            toggleModal('attack-modal', true);
        });
        targets.appendChild(target);
    });
}

async function forceRefreshState() {
    if (currentSessionRef && currentSessionRef.current) {
        try {
            const state = await apiGet(`/api/game/state?playerId=${encodeURIComponent(currentSessionRef.current.playerId)}`);
            renderGameState(state);
        } catch (error) {
            console.error(error);
        }
    }
}

async function sendTransition(sessionRef, action) {
    if (!sessionRef.current?.playerId) return;
    await apiPost('/api/action/transition', {
        playerId: sessionRef.current.playerId,
        action
    });
    toggleModal('transition-modal', false);
}

async function sendAttack(sessionRef) {
    if (!sessionRef.current?.playerId || !currentTargetId) return;
    await apiPost('/api/action/request-attack', {
        playerId: sessionRef.current.playerId,
        targetId: currentTargetId
    });
    toggleModal('attack-modal', false);
    await forceRefreshState();
}

async function focusCamera(sessionRef) {
    if (!sessionRef.current?.playerId) return;
    const button = document.getElementById('focus-camera-button');
    button.classList.add('active');
    try {
        await apiPost('/api/camera/focus', {
            playerId: sessionRef.current.playerId
        });
    } catch (error) {
        console.error(error);
        button.classList.remove('active');
    }
}

function valuePair(current, max) {
    if (current === undefined || current === null) return '-';
    if (max === undefined || max === null) return String(current);
    return `${current}/${max}`;
}

function toggleModal(id, active) {
    document.getElementById(id).classList.toggle('active', active);
}
