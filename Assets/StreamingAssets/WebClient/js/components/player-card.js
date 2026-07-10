export function renderPlayerCard(player) {
    const card = document.createElement('div');
    card.className = `player-card ${player.isReady ? '' : 'not-ready'}`;

    const avatar = document.createElement('div');
    avatar.className = 'avatar';
    if (player.portraitUrl) {
        avatar.style.backgroundImage = `url('${player.portraitUrl}')`;
    }

    const body = document.createElement('div');
    const name = document.createElement('strong');
    name.textContent = player.name || 'Игрок';

    const status = document.createElement('span');
    status.className = `status-pill ${player.isReady ? 'ready' : ''}`;
    status.textContent = player.isReady
        ? 'Готов'
        : player.hasCharacter
            ? 'Персонаж выбран'
            : 'Без персонажа';

    body.append(name, status);
    card.append(avatar, body);
    return card;
}
