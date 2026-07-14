import { apiGet, apiPost } from '../api.js';
import { saveSession } from '../session.js';

let currentSession = null;
let characterData = null;
let activeTab = 1; // 1 = weapon 1, 2 = weapon 2, 3 = defense
let selectedSlotIndex = null;
let selectedAbilityName = null;

export async function initCharacterEditorScreen(session) {
    currentSession = session;
    selectedSlotIndex = null;
    selectedAbilityName = null;
    hideDetails();
    setupTabListeners();
    setupAttributeListeners();
    setupBackRoute();
    await ensureCharacter();
    await refreshCharacterDetails();
}

async function ensureCharacter() {
    if (!currentSession?.playerId) return;
    if (currentSession.hasCharacter) return;

    const response = await apiPost('/api/character/create', {
        playerId: currentSession.playerId
    });

    if (response.status === 'success') {
        currentSession = {
            ...currentSession,
            hasCharacter: true,
            isReady: response.isReady
        };
        saveSession(currentSession);
    }
}

export async function refreshCharacterDetails() {
    if (!currentSession?.playerId) return;
    try {
        const data = await apiGet(`/api/character/details?playerId=${encodeURIComponent(currentSession.playerId)}`);
        if (data.status !== 'success') {
            await ensureCharacter();
            characterData = await apiGet(`/api/character/details?playerId=${encodeURIComponent(currentSession.playerId)}`);
        } else {
            characterData = data;
        }
        renderCharacterDetails();
    } catch (err) {
        console.error('Failed to load character details:', err);
    }
}

function setupTabListeners() {
    const tabs = [
        document.getElementById('skills-tab-1'),
        document.getElementById('skills-tab-2'),
        document.getElementById('skills-tab-3')
    ];

    tabs.forEach((tab, index) => {
        if (!tab) return;
        tab.onclick = () => {
            activeTab = index + 1;
            selectedSlotIndex = null;
            selectedAbilityName = null;
            hideDetails();
            renderCharacterDetails();
        };
    });
}

function setupAttributeListeners() {
    document.querySelectorAll('.attr-box[data-attr]').forEach(box => {
        const button = box.querySelector('.attr-increase');
        if (!button) return;
        button.onclick = async event => {
            event.stopPropagation();
            await increaseAttribute(box.dataset.attr);
        };
    });
}

function setupBackRoute() {
    const backButton = document.querySelector('#screen-character-editor [data-route]');
    if (backButton) {
        backButton.dataset.route = currentSession?.gameStarted ? 'game' : 'lobby';
    }
}

function getActiveWeaponAttackType() {
    const weaponName = activeTab === 1 ? characterData.eqWeapon : characterData.eqWeapon2;
    if (!weaponName) return 'Melee';
    const item = (characterData.allItems || []).find(i => i.title === weaponName);
    return item ? item.attackType : 'Melee';
}

function getWeaponDisplayName() {
    if (activeTab === 3) return 'Защита';
    const weaponName = activeTab === 1 ? characterData.eqWeapon : characterData.eqWeapon2;
    return weaponName || 'Без оружия';
}

function getAttackTypeRussian(type) {
    switch (type) {
        case 'Melee': return 'Ближний бой';
        case 'Ranged': return 'Дальний бой';
        case 'Magic': return 'Магия';
        case 'Defense': return 'Защита';
        default: return type || '-';
    }
}

function getEffectTypeRussian(type) {
    switch (type) {
        case 'Damage': return 'Урон';
        case 'Heal': return 'Лечение';
        case 'Move': return 'Перемещение';
        case 'Status': return 'Статус';
        case 'Reveal': return 'Раскрытие';
        default: return type || '-';
    }
}

function getCurrentSlots() {
    if (activeTab === 1) return characterData.attackSlots || [];
    if (activeTab === 2) return characterData.attack2Slots || [];
    return characterData.defenseSlots || [];
}

function getAbility(name) {
    if (!name) return null;
    return (characterData.allAbilities || []).find(a => a.title === name) || null;
}

function abilityCost(name) {
    const ability = getAbility(name);
    return ability ? Math.max(0, Number(ability.cost) || 0) : 0;
}

function slotsCost(slots) {
    return (slots || []).reduce((sum, name) => sum + abilityCost(name), 0);
}

function getActiveBudget() {
    return activeTab === 3
        ? Number(characterData.defenseSkillPoints) || 0
        : Number(characterData.attackSkillPoints) || 0;
}

function getProjectedCost(abilityName) {
    if (selectedSlotIndex === null) return Number.POSITIVE_INFINITY;
    const slots = [...getCurrentSlots()];
    while (slots.length < 6) slots.push('');
    slots[selectedSlotIndex] = abilityName;
    return slotsCost(slots);
}

function canAssignAbility(abilityName) {
    if (selectedSlotIndex === null) return false;
    return getProjectedCost(abilityName) <= getActiveBudget();
}

function renderCharacterDetails() {
    if (!characterData || characterData.status !== 'success') return;

    document.getElementById('char-str').textContent = characterData.strength || '-';
    document.getElementById('char-agi').textContent = characterData.agility || '-';
    document.getElementById('char-int').textContent = characterData.intelligence || '-';
    document.getElementById('char-hol').textContent = characterData.holiness || '-';

    renderAttributeControls();
    renderSkillPools();
    renderTabs();
    renderSlots();
    renderGrimoire();
}

function renderAttributeControls() {
    const points = Number(characterData.attributePoints) || 0;
    document.querySelectorAll('.attr-increase').forEach(button => {
        button.classList.toggle('hidden', points <= 0);
        button.disabled = points <= 0;
    });
}

function renderSkillPools() {
    const panel = document.getElementById('skill-points-panel');
    if (!panel) return;

    const free = Number(characterData.skillPoints) || 0;
    const attack = Number(characterData.attackSkillPoints) || 0;
    const defense = Number(characterData.defenseSkillPoints) || 0;
    const attack1Spent = slotsCost(characterData.attackSlots);
    const attack2Spent = slotsCost(characterData.attack2Slots);
    const defenseSpent = slotsCost(characterData.defenseSlots);

    panel.innerHTML = `
        <div class="skill-pool-row">
            <span>Свободные</span>
            <strong>${free}</strong>
        </div>
        <div class="skill-pool-row">
            <span>Атака</span>
            <strong>${attack}</strong>
            <small>Оружие 1: ${attack1Spent}/${attack} · Оружие 2: ${attack2Spent}/${attack}</small>
            <button class="pool-increase ${free > 0 ? '' : 'hidden'}" data-pool="attack" type="button">↑</button>
        </div>
        <div class="skill-pool-row">
            <span>Защита</span>
            <strong>${defense}</strong>
            <small>${defenseSpent}/${defense}</small>
            <button class="pool-increase ${free > 0 ? '' : 'hidden'}" data-pool="defense" type="button">↑</button>
        </div>
    `;

    panel.querySelectorAll('.pool-increase').forEach(button => {
        button.onclick = () => allocateSkillPoint(button.dataset.pool);
    });
}

function renderTabs() {
    [1, 2, 3].forEach(tab => {
        const button = document.getElementById(`skills-tab-${tab}`);
        if (button) button.classList.toggle('active', activeTab === tab);
    });

    const isDefense = activeTab === 3;
    const requiredType = isDefense ? 'Defense' : getActiveWeaponAttackType();
    const spent = slotsCost(getCurrentSlots());
    const budget = getActiveBudget();
    const instructionEl = document.querySelector('.skills-instruction');
    if (instructionEl) {
        instructionEl.innerHTML = `
            <div>Источник: <strong>${getWeaponDisplayName()}</strong></div>
            <div>Тип: <span style="color: var(--accent);">${getAttackTypeRussian(requiredType)}</span></div>
            <div>Потрачено: <strong>${spent}/${budget}</strong></div>
        `;
    }
}

function renderSlots() {
    const slotsContainer = document.getElementById('skills-slots-container');
    slotsContainer.innerHTML = '';

    const currentSlots = getCurrentSlots();
    for (let i = 0; i < 6; i++) {
        const abilityName = currentSlots?.[i] || '';
        const slotEl = document.createElement('div');
        slotEl.className = 'ability-slot';
        if (selectedSlotIndex === i) slotEl.classList.add('selected');

        if (abilityName) {
            const iconUrl = `/api/icon/ability?title=${encodeURIComponent(abilityName)}`;
            slotEl.style.backgroundImage = `url('${iconUrl}')`;
        }

        slotEl.innerHTML = `
            <span>Слот ${i + 1}</span>
            <strong>${abilityName || 'Пусто'}</strong>
            ${abilityName ? `<em>${abilityCost(abilityName)} AP</em>` : ''}
        `;

        slotEl.onclick = () => {
            selectedSlotIndex = i;
            selectedAbilityName = null;
            renderCharacterDetails();
            if (abilityName) showAbilityDetails(abilityName, 'slot');
            else hideDetails();
        };

        slotsContainer.appendChild(slotEl);
    }
}

function renderGrimoire() {
    const requiredType = activeTab === 3 ? 'Defense' : getActiveWeaponAttackType();
    const filteredAbilities = (characterData.allAbilities || []).filter(a => a.attackType === requiredType);
    const grimoireList = document.getElementById('grimoire-list');
    grimoireList.innerHTML = '';

    if (filteredAbilities.length === 0) {
        grimoireList.innerHTML = '<div class="empty-list">Нет доступных способностей для этого типа.</div>';
        return;
    }

    filteredAbilities.forEach(ability => {
        const canAfford = canAssignAbility(ability.title);
        const abEl = document.createElement('button');
        abEl.type = 'button';
        abEl.className = 'character-card ability-card';
        abEl.classList.toggle('selected', selectedAbilityName === ability.title);
        abEl.classList.toggle('locked', !canAfford);

        const iconUrl = `/api/icon/ability?title=${encodeURIComponent(ability.title)}`;
        abEl.innerHTML = `
            <div class="avatar ability-icon" style="background-image: url('${iconUrl}')">
                <span>${ability.cost} AP</span>
            </div>
            <div>
                <strong>${ability.title}</strong>
                <p>${ability.description || 'Способность'}</p>
            </div>
        `;

        abEl.onclick = () => {
            selectedAbilityName = ability.title;
            renderCharacterDetails();
            showAbilityDetails(ability.title, 'grimoire');
        };
        grimoireList.appendChild(abEl);
    });
}

function showAbilityDetails(abilityName, origin) {
    const detailsPanel = document.getElementById('ability-details-panel');
    const contentEl = document.getElementById('ability-details-content');
    const ability = getAbility(abilityName);

    if (!ability) {
        hideDetails();
        return;
    }

    detailsPanel.style.display = 'block';
    const canEquip = origin === 'grimoire' && canAssignAbility(abilityName);
    const projected = selectedSlotIndex === null ? '-' : getProjectedCost(abilityName);
    const budget = getActiveBudget();

    let actionHtml = '';
    if (origin === 'slot') {
        actionHtml = '<button id="ability-action-btn" class="button danger" type="button">Освободить слот</button>';
    } else if (selectedSlotIndex === null) {
        actionHtml = '<div class="muted detail-note">Сначала выберите слот выше.</div>';
    } else if (!canEquip) {
        actionHtml = `<button class="button secondary" type="button" disabled>Не хватает очков: ${projected}/${budget}</button>`;
    } else {
        actionHtml = `<button id="ability-action-btn" class="button success" type="button">Поставить в слот ${selectedSlotIndex + 1}</button>`;
    }

    contentEl.innerHTML = `
        <div class="ability-details-head">
            <div class="avatar ability-icon" style="background-image: url('/api/icon/ability?title=${encodeURIComponent(ability.title)}')"></div>
            <div>
                <h3>${ability.title}</h3>
                <span class="muted">${getAttackTypeRussian(ability.attackType)} · ${ability.cost} AP</span>
            </div>
        </div>
        <p>${ability.description || 'Нет описания.'}</p>
        <div class="ability-stats">
            <div>Дальность: <strong>${ability.range}</strong></div>
            <div>Эффект: <strong>${getEffectTypeRussian(ability.effectType)}</strong></div>
            <div>Множитель: <strong>x${ability.multiplier}</strong></div>
            <div>Защита: <strong>${ability.defenseValue || 0}</strong></div>
        </div>
        ${actionHtml}
    `;

    const actionBtn = document.getElementById('ability-action-btn');
    if (actionBtn) {
        actionBtn.onclick = async () => {
            await assignAbility(origin === 'slot' ? '' : abilityName);
        };
    }
}

function hideDetails() {
    const detailsPanel = document.getElementById('ability-details-panel');
    if (detailsPanel) detailsPanel.style.display = 'none';
}

async function increaseAttribute(attribute) {
    if (!currentSession?.playerId || !attribute) return;
    const response = await apiPost('/api/character/increase-attribute', {
        playerId: currentSession.playerId,
        attribute
    });

    if (response.status === 'success') {
        await refreshCharacterDetails();
    }
}

async function allocateSkillPoint(pool) {
    if (!currentSession?.playerId || !pool) return;
    const response = await apiPost('/api/character/allocate-skill-point', {
        playerId: currentSession.playerId,
        pool
    });

    if (response.status === 'success') {
        await refreshCharacterDetails();
    }
}

async function assignAbility(abilityName) {
    if (selectedSlotIndex === null) return;
    if (abilityName && !canAssignAbility(abilityName)) return;

    const attackSlots = normalizeSlots(characterData.attackSlots);
    const attack2Slots = normalizeSlots(characterData.attack2Slots);
    const defenseSlots = normalizeSlots(characterData.defenseSlots);

    if (activeTab === 1) attackSlots[selectedSlotIndex] = abilityName;
    else if (activeTab === 2) attack2Slots[selectedSlotIndex] = abilityName;
    else defenseSlots[selectedSlotIndex] = abilityName;

    try {
        const response = await apiPost('/api/character/update-skills', {
            playerId: currentSession.playerId,
            attackSlots,
            attack2Slots,
            defenseSlots
        });

        if (response.status === 'success') {
            selectedSlotIndex = null;
            selectedAbilityName = null;
            hideDetails();
            await refreshCharacterDetails();
        } else if (response.reason) {
            alert(response.reason);
        }
    } catch (err) {
        console.error(err);
    }
}

function normalizeSlots(slots) {
    const result = [...(slots || [])];
    while (result.length < 6) result.push('');
    return result.slice(0, 6);
}
