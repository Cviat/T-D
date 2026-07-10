import { apiGet, apiPost } from '../api.js';

let gameTimer = null;
let currentTargetId = null;

export function startGamePolling(session) {
    stopGamePolling();
    document.getElementById('game-name').textContent = session.name || 'Игрок';
    document.getElementById('game-photo').style.backgroundImage = session.portraitUrl ? `url('${session.portraitUrl}')` : '';

    const tick = async () => {
        try {
            const state = await apiGet(`/api/game/state?playerId=${encodeURIComponent(session.playerId)}`);
            renderGameState(state);
        } catch (error) {
            console.error(error);
        }

        gameTimer = setTimeout(tick, 2000);
    };

    tick();
}

export function stopGamePolling() {
    if (gameTimer) {
        clearTimeout(gameTimer);
        gameTimer = null;
    }
}

export function initCombatControls(sessionRef) {
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
}

function renderGameState(state) {
    document.getElementById('stat-hp').textContent = valuePair(state.hp, state.maxHp);
    document.getElementById('stat-armor').textContent = valuePair(state.armor, state.maxArmor);
    document.getElementById('stat-move').textContent = valuePair(state.movement, state.maxMovement);
    document.getElementById('stat-rolls').textContent = valuePair(state.rolls, state.maxRolls);
    document.getElementById('turn-state').textContent = state.isCombatActive
        ? state.isMyTurn ? 'Ваш ход' : 'Ход другого участника'
        : 'Свободное перемещение';
    document.getElementById('active-weapon').textContent = `Оружие: ${state.activeWeapon || '-'}`;

    const transitionModal = document.getElementById('transition-modal');
    if (state.prompt) {
        document.getElementById('transition-text').textContent = state.prompt;
        transitionModal.classList.add('active');
    } else {
        transitionModal.classList.remove('active');
    }

    const targets = document.getElementById('targets-container');
    targets.innerHTML = '';
    (state.enemies || []).forEach(enemy => {
        const target = document.createElement('button');
        target.className = 'target-avatar';
        target.type = 'button';
        target.title = enemy.name;
        if (enemy.portraitUrl) {
            target.style.backgroundImage = `url('${enemy.portraitUrl}')`;
        }
        target.addEventListener('click', () => {
            currentTargetId = enemy.id;
            document.getElementById('attack-target-name').textContent = `Атаковать: ${enemy.name}?`;
            toggleModal('attack-modal', true);
        });
        targets.appendChild(target);
    });
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
    await apiPost('/api/action/attack', {
        playerId: sessionRef.current.playerId,
        targetId: currentTargetId
    });
    toggleModal('attack-modal', false);
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
