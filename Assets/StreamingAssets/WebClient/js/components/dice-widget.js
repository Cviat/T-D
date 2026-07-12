import { apiPost } from '../api.js';

let isRolling = false;
let currentRollResult = null;

export function showDiceWidget(pendingRoll, session, onRollSubmitted, onSessionUpdated) {
    const modal = document.getElementById('dice-modal');
    const title = document.getElementById('dice-title');
    const prompt = document.getElementById('dice-prompt');
    const diceVal = document.getElementById('dice-value');
    
    const rollBtn = document.getElementById('dice-roll-btn');
    const rerollBtn = document.getElementById('dice-reroll-btn');
    const submitBtn = document.getElementById('dice-submit-btn');
    const coinsSpan = document.getElementById('dice-coins');

    // Reset state
    isRolling = false;
    currentRollResult = null;
    diceVal.textContent = '?';
    diceVal.classList.remove('rolling');
    
    rollBtn.classList.remove('hidden');
    rollBtn.disabled = false;
    rerollBtn.classList.add('hidden');
    rerollBtn.disabled = false;
    submitBtn.classList.add('hidden');
    submitBtn.disabled = false;

    // Set title and prompt text depending on type
    if (pendingRoll.type === 'attack') {
        title.textContent = 'Бросок Атаки';
        prompt.textContent = 'Бросьте D6, чтобы выбрать способность оружия для атаки.';
    } else if (pendingRoll.type === 'defense') {
        title.textContent = 'Бросок Защиты';
        const attacker = pendingRoll.attackerName || 'Враг';
        const ability = pendingRoll.attackerAbilityName || 'атака';
        prompt.textContent = `Вас атакует ${attacker} способностью "${ability}" (Урон: ${pendingRoll.baseDamage}). Бросьте D6 для защиты!`;
    }

    // Setup modal show
    modal.classList.add('active');

    // Clear event listeners
    const newRollBtn = rollBtn.cloneNode(true);
    rollBtn.parentNode.replaceChild(newRollBtn, rollBtn);

    const newRerollBtn = rerollBtn.cloneNode(true);
    rerollBtn.parentNode.replaceChild(newRerollBtn, rerollBtn);

    const newSubmitBtn = submitBtn.cloneNode(true);
    submitBtn.parentNode.replaceChild(newSubmitBtn, submitBtn);

    // Setup Roll button
    newRollBtn.addEventListener('click', () => {
        if (isRolling) return;
        runDiceAnimation(diceVal, 800, (result) => {
            currentRollResult = result;
            newRollBtn.classList.add('hidden');
            newSubmitBtn.classList.remove('hidden');
            
            // Setup reroll button if coins are available
            if (session.rerollCoins > 0) {
                coinsSpan.textContent = session.rerollCoins;
                newRerollBtn.classList.remove('hidden');
            } else {
                newRerollBtn.classList.add('hidden');
            }
        });
    });

    // Setup Reroll button
    newRerollBtn.addEventListener('click', async () => {
        if (isRolling || session.rerollCoins <= 0) return;
        newRerollBtn.disabled = true;
        newSubmitBtn.classList.add('hidden');
        
        runDiceAnimation(diceVal, 800, async () => {
            try {
                const response = await apiPost('/api/roll/reroll', {
                    playerId: session.playerId
                });
                
                if (response.status === 'success') {
                    currentRollResult = response.rollResult;
                    diceVal.textContent = currentRollResult;
                    
                    // Update session coins
                    session.rerollCoins = Math.max(0, session.rerollCoins - 1);
                    if (onSessionUpdated) onSessionUpdated(session);
                } else {
                    alert('Переброс не удался: ' + (response.reason || 'неизвестная ошибка'));
                }
            } catch (err) {
                console.error(err);
                alert('Ошибка связи с сервером при перебросе.');
            } finally {
                newRerollBtn.disabled = false;
                newSubmitBtn.classList.remove('hidden');
                
                if (session.rerollCoins > 0) {
                    coinsSpan.textContent = session.rerollCoins;
                    newRerollBtn.classList.remove('hidden');
                } else {
                    newRerollBtn.classList.add('hidden');
                }
            }
        });
    });

    // Setup Submit button
    newSubmitBtn.addEventListener('click', async () => {
        if (isRolling || currentRollResult === null) return;
        newSubmitBtn.disabled = true;
        
        try {
            await onRollSubmitted(currentRollResult);
            modal.classList.remove('active');
        } catch (err) {
            console.error(err);
            alert('Ошибка отправки броска.');
            newSubmitBtn.disabled = false;
        }
    });
}

function runDiceAnimation(element, durationMs, onFinished) {
    isRolling = true;
    element.classList.add('rolling');
    
    const interval = setInterval(() => {
        element.textContent = Math.floor(Math.random() * 6) + 1;
    }, 80);

    setTimeout(() => {
        clearInterval(interval);
        element.classList.remove('rolling');
        isRolling = false;
        
        const finalValue = Math.floor(Math.random() * 6) + 1;
        element.textContent = finalValue;
        
        if (onFinished) {
            onFinished(finalValue);
        }
    }, durationMs);
}
