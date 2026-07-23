import { apiGet, apiPost } from '../api.js';

let currentSession = null;
let deckData = [];
let selectedCard = null;

export async function initActionCardsScreen(session) {
    currentSession = session;
    selectedCard = null;
    hideCardDetails();
    await refreshActionCards();
}

export async function refreshActionCards() {
    if (!currentSession?.playerId) return;
    try {
        const data = await apiGet(`/api/character/details?playerId=${encodeURIComponent(currentSession.playerId)}`);
        deckData = data.actionCards || [];
        renderDeck();
    } catch (err) {
        console.error("Failed to load character details for deck:", err);
    }
}

function renderDeck() {
    const container = document.getElementById('deck-container');
    container.innerHTML = '';

    if (deckData.length === 0) {
        container.innerHTML = '<div style="grid-column: span 2; text-align: center; color: var(--muted); padding: 20px;">Нет доступных карт</div>';
        return;
    }

    deckData.forEach(card => {
        const cardEl = document.createElement('div');
        cardEl.style.cssText = `
            border: 2px solid var(--line);
            border-radius: 6px;
            padding: 8px;
            background: linear-gradient(180deg, #1b1613 0%, #110e0c 100%);
            box-shadow: 0 4px 8px rgba(0, 0, 0, 0.4);
            cursor: pointer;
            text-align: center;
            transition: transform 0.2s, border-color 0.2s;
        `;
        
        // Cost tag
        const costHtml = `<div style="
            display: inline-block;
            background: #4dabf7;
            color: #fff;
            font-size: 10px;
            font-weight: bold;
            padding: 2px 6px;
            border-radius: 10px;
            margin-bottom: 6px;
        ">${card.manaCost} MP</div>`;

        // Card Icon
        const iconHtml = `<div style="
            width: 50px;
            height: 50px;
            margin: 0 auto 8px auto;
            border: 1px solid var(--accent);
            border-radius: 4px;
            background: #0f0b09 url('/api/icon/card?id=${encodeURIComponent(card.id)}') center / cover no-repeat;
        "></div>`;

        // Card Title
        const titleHtml = `<div style="
            font-family: 'Cinzel', serif;
            font-size: 13px;
            color: #fff;
            font-weight: bold;
            margin-bottom: 4px;
            white-space: nowrap;
            overflow: hidden;
            text-overflow: ellipsis;
        ">${card.title}</div>`;

        cardEl.innerHTML = costHtml + iconHtml + titleHtml;

        // Hover scale effect
        cardEl.addEventListener('mouseenter', () => {
            cardEl.style.transform = 'scale(1.05)';
            cardEl.style.borderColor = 'var(--accent)';
        });
        cardEl.addEventListener('mouseleave', () => {
            cardEl.style.transform = 'scale(1)';
            cardEl.style.borderColor = 'var(--line)';
        });

        cardEl.addEventListener('click', () => {
            selectCard(card);
        });

        container.appendChild(cardEl);
    });
}

function selectCard(card) {
    selectedCard = card;
    const panel = document.getElementById('selected-card-panel');
    panel.style.display = 'block';
    
    document.getElementById('selected-card-title').textContent = card.title;
    document.getElementById('selected-card-desc').textContent = card.description;

    // Set card detail icon dynamically if exists
    let detailIcon = panel.querySelector('.detail-icon');
    if (!detailIcon) {
        detailIcon = document.createElement('div');
        detailIcon.className = 'detail-icon';
        detailIcon.style.cssText = `
            width: 64px;
            height: 64px;
            margin: 8px auto;
            border: 2px solid #ebd670;
            border-radius: 4px;
            background-size: cover;
            background-position: center;
        `;
        panel.insertBefore(detailIcon, document.getElementById('selected-card-desc'));
    }
    detailIcon.style.backgroundImage = `url('/api/icon/card?id=${encodeURIComponent(card.id)}')`;

    // Show coordinate target inputs only for fireball
    const targetInputs = document.getElementById('card-target-inputs');
    if (card.effectId === 'fireball') {
        targetInputs.style.display = 'flex';
    } else {
        targetInputs.style.display = 'none';
    }

    panel.scrollIntoView({ behavior: 'smooth' });
}

function hideCardDetails() {
    const panel = document.getElementById('selected-card-panel');
    if (panel) panel.style.display = 'none';
    selectedCard = null;
}

export function initActionCardsControls(sessionRef) {
    document.getElementById('play-card-cancel').addEventListener('click', hideCardDetails);
    
    document.getElementById('play-card-confirm').addEventListener('click', async () => {
        if (!selectedCard || !sessionRef.current?.playerId) return;

        const targetX = parseInt(document.getElementById('card-target-x').value) || 0;
        const targetY = parseInt(document.getElementById('card-target-y').value) || 0;

        try {
            const response = await apiPost('/api/action/play-card', {
                playerId: sessionRef.current.playerId,
                cardId: selectedCard.id,
                targetX: targetX,
                targetY: targetY
            });

            if (response.status === 'success') {
                // Animate fade-out and close
                const panel = document.getElementById('selected-card-panel');
                panel.style.transition = 'opacity 0.3s';
                panel.style.opacity = '0';
                
                setTimeout(() => {
                    hideCardDetails();
                    panel.style.opacity = '1';
                    // Route back to game main screen
                    window.dispatchEvent(new CustomEvent('route', { detail: 'game' }));
                }, 300);
            } else {
                alert(`Не удалось разыграть карту: ${response.reason || 'неизвестная ошибка'}`);
            }
        } catch (err) {
            console.error("Play card request failed:", err);
            alert("Сетевая ошибка при розыгрыше карты.");
        }
    });
}
