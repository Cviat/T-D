import { apiGet, apiPost } from '../api.js';

let currentSession = null;
let characterData = null;
let activeTab = 1; // 1 = Weapon 1, 2 = Weapon 2, 3 = Defense
let selectedSlotIndex = null; // Clicked slot
let selectedAbilityName = null; // Clicked ability from grimoire

export async function initCharacterEditorScreen(session) {
    currentSession = session;
    selectedSlotIndex = null;
    selectedAbilityName = null;
    hideDetails();
    await refreshCharacterDetails();
    setupTabListeners();
}

export async function refreshCharacterDetails() {
    if (!currentSession?.playerId) return;
    try {
        const data = await apiGet(`/api/character/details?playerId=${encodeURIComponent(currentSession.playerId)}`);
        characterData = data;
        renderCharacterDetails();
    } catch (err) {
        console.error("Failed to load character details:", err);
    }
}

function setupTabListeners() {
    const tab1 = document.getElementById('skills-tab-1');
    const tab2 = document.getElementById('skills-tab-2');
    const tab3 = document.getElementById('skills-tab-3');

    // Clone to remove previous event listeners
    const newTab1 = tab1.cloneNode(true);
    tab1.parentNode.replaceChild(newTab1, tab1);
    const newTab2 = tab2.cloneNode(true);
    tab2.parentNode.replaceChild(newTab2, tab2);
    const newTab3 = tab3.cloneNode(true);
    tab3.parentNode.replaceChild(newTab3, tab3);

    newTab1.addEventListener('click', () => {
        activeTab = 1;
        newTab1.classList.add('active');
        newTab2.classList.remove('active');
        newTab3.classList.remove('active');
        selectedSlotIndex = null;
        selectedAbilityName = null;
        hideDetails();
        renderCharacterDetails();
    });

    newTab2.addEventListener('click', () => {
        activeTab = 2;
        newTab2.classList.add('active');
        newTab1.classList.remove('active');
        newTab3.classList.remove('active');
        selectedSlotIndex = null;
        selectedAbilityName = null;
        hideDetails();
        renderCharacterDetails();
    });

    newTab3.addEventListener('click', () => {
        activeTab = 3;
        newTab3.classList.add('active');
        newTab1.classList.remove('active');
        newTab2.classList.remove('active');
        selectedSlotIndex = null;
        selectedAbilityName = null;
        hideDetails();
        renderCharacterDetails();
    });
}

function getActiveWeaponAttackType() {
    const weaponName = activeTab === 1 ? characterData.eqWeapon : characterData.eqWeapon2;
    if (!weaponName) return "Melee";
    const item = characterData.allItems.find(i => i.title === weaponName);
    return item ? item.attackType : "Melee";
}

function getWeaponDisplayName() {
    if (activeTab === 3) return "Защита";
    const weaponName = activeTab === 1 ? characterData.eqWeapon : characterData.eqWeapon2;
    return weaponName || "Без оружия (Кулаки)";
}

function getAttackTypeRussian(type) {
    switch (type) {
        case "Melee": return "Ближний бой";
        case "Ranged": return "Дальний бой";
        case "Magic": return "Магия";
        case "Defense": return "Защита";
        default: return type;
    }
}

function getEffectTypeRussian(type) {
    switch (type) {
        case "Damage": return "Нанесение урона";
        case "Heal": return "Лечение";
        case "Move": return "Перемещение";
        case "Status": return "Статусный эффект";
        case "Reveal": return "Раскрытие карты";
        default: return type;
    }
}

function showAbilityDetails(abilityName, origin) {
    const detailsPanel = document.getElementById('ability-details-panel');
    const contentEl = document.getElementById('ability-details-content');
    
    if (!abilityName) {
        hideDetails();
        return;
    }

    const ability = characterData.allAbilities.find(a => a.title === abilityName);
    if (!ability) {
        hideDetails();
        return;
    }

    detailsPanel.style.display = 'block';

    const statParts = [
        `<div>Стоимость: <strong>${ability.cost} AP (Очков Действий)</strong></div>`,
        `<div>Дальность применения: <strong>${ability.range} кл.</strong></div>`,
        `<div>Эффект: <strong>${getEffectTypeRussian(ability.effectType)}</strong></div>`
    ];

    if (ability.multiplier && ability.multiplier !== 1) {
        statParts.push(`<div>Множитель эффекта: <strong>x${ability.multiplier}</strong></div>`);
    }
    if (ability.defenseValue) {
        statParts.push(`<div>Значение защиты: <strong>+${ability.defenseValue}</strong></div>`);
    }

    let actionButtonHtml = '';
    if (origin === 'slot') {
        actionButtonHtml = `<button id="ability-action-btn" class="button danger" style="width: 100%; margin-top: 12px; min-height: 40px;" type="button">Освободить слот</button>`;
    } else {
        if (selectedSlotIndex !== null) {
            actionButtonHtml = `<button id="ability-action-btn" class="button success" style="width: 100%; margin-top: 12px; min-height: 40px;" type="button">Экипировать в Слот ${selectedSlotIndex + 1}</button>`;
        } else {
            actionButtonHtml = `<div style="text-align: center; font-size: 12px; color: var(--muted); margin-top: 12px; padding: 8px; border: 1px dashed var(--line); border-radius: 4px;">Сначала выберите Слот способностей выше, чтобы экипировать эту способность</div>`;
        }
    }

    contentEl.innerHTML = `
        <div style="display: flex; gap: 12px; align-items: center; margin-bottom: 10px;">
            <div style="width: 48px; height: 48px; border: 2px solid var(--accent); border-radius: 4px; background-image: url('/api/icon/ability?title=${encodeURIComponent(ability.title)}'); background-size: cover; background-position: center; background-repeat: no-repeat;"></div>
            <div>
                <h3 style="margin: 0; color: var(--accent); font-family: 'Cinzel', serif; font-size: 16px;">${ability.title}</h3>
                <span class="muted" style="font-size: 11px;">Тип: ${getAttackTypeRussian(ability.attackType)}</span>
            </div>
        </div>
        <p style="margin: 4px 0 8px 0; font-style: italic; color: #ccc;">${ability.description || 'Нет описания.'}</p>
        <div style="margin: 8px 0; padding: 6px; border-left: 2px solid var(--accent); background: rgba(255,255,255,0.03); font-size: 12px;">
            ${statParts.join('')}
        </div>
        ${actionButtonHtml}
    `;

    const actionBtn = document.getElementById('ability-action-btn');
    if (actionBtn) {
        actionBtn.addEventListener('click', async () => {
            if (origin === 'slot') {
                await assignAbility("");
            } else {
                await assignAbility(abilityName);
            }
        });
    }
}

function hideDetails() {
    const detailsPanel = document.getElementById('ability-details-panel');
    if (detailsPanel) detailsPanel.style.display = 'none';
}

function renderCharacterDetails() {
    if (!characterData) return;

    // Render Attributes
    document.getElementById('char-str').textContent = characterData.strength || "-";
    document.getElementById('char-agi').textContent = characterData.agility || "-";
    document.getElementById('char-int').textContent = characterData.intelligence || "-";
    document.getElementById('char-hol').textContent = characterData.holiness || "-";

    const isDefense = activeTab === 3;
    const requiredType = isDefense ? "Defense" : getActiveWeaponAttackType();
    
    const instructionEl = document.querySelector('.skills-instruction');
    if (instructionEl) {
        instructionEl.innerHTML = `
            <div>Выбранный источник: <strong>${getWeaponDisplayName()}</strong></div>
            <div>Требуется тип: <span style="color: var(--accent);">${getAttackTypeRussian(requiredType)}</span></div>
        `;
    }

    // Render Ability Slots (6 slots)
    const slotsContainer = document.getElementById('skills-slots-container');
    slotsContainer.innerHTML = '';

    let currentSlots = [];
    if (activeTab === 1) currentSlots = characterData.attackSlots;
    else if (activeTab === 2) currentSlots = characterData.attack2Slots;
    else currentSlots = characterData.defenseSlots;

    for (let i = 0; i < 6; i++) {
        const abilityName = currentSlots?.[i] || "";
        const slotEl = document.createElement('div');
        slotEl.style.cssText = "border: 2px solid var(--line); padding: 8px; border-radius: 4px; background: rgba(0,0,0,0.5); text-align: center; cursor: pointer; min-height: 56px; display: flex; flex-direction: column; align-items: center; justify-content: center; position: relative; text-shadow: 0 1px 3px #000, 0 0 2px #000; font-weight: bold;";
        
        if (selectedSlotIndex === i) {
            slotEl.style.borderColor = "var(--accent)";
            slotEl.style.boxShadow = "0 0 8px rgba(212, 175, 55, 0.5)";
        }

        if (abilityName) {
            const iconUrl = `/api/icon/ability?title=${encodeURIComponent(abilityName)}`;
            slotEl.style.backgroundImage = `url('${iconUrl}')`;
            slotEl.style.backgroundSize = 'cover';
            slotEl.style.backgroundPosition = 'center';
            slotEl.style.backgroundRepeat = 'no-repeat';
        }

        const slotNum = document.createElement('span');
        slotNum.style.cssText = "font-size: 8px; color: var(--muted); text-transform: uppercase; z-index: 1;";
        slotNum.textContent = `Слот ${i + 1}`;
        slotEl.appendChild(slotNum);

        const skillNameEl = document.createElement('strong');
        skillNameEl.style.cssText = "font-size: 11px; color: #fff; display: block; margin-top: 2px; z-index: 1;";
        skillNameEl.textContent = abilityName || "Пусто";
        if (!abilityName) {
            skillNameEl.style.color = "#555";
        }
        slotEl.appendChild(skillNameEl);

        slotEl.addEventListener('click', () => {
            selectedSlotIndex = i;
            selectedAbilityName = null;
            renderCharacterDetails();
            if (abilityName) {
                showAbilityDetails(abilityName, 'slot');
            } else {
                hideDetails();
            }
        });

        slotsContainer.appendChild(slotEl);
    }

    const filteredAbilities = (characterData.allAbilities || []).filter(a => a.attackType === requiredType);

    // Render Grimoire List
    const grimoireList = document.getElementById('grimoire-list');
    grimoireList.innerHTML = '';

    if (filteredAbilities.length === 0) {
        const emptyEl = document.createElement('div');
        emptyEl.style.cssText = "text-align: center; color: var(--muted); padding: 20px; font-size: 13px;";
        emptyEl.textContent = "Нет доступных способностей для этого типа атаки.";
        grimoireList.appendChild(emptyEl);
    } else {
        filteredAbilities.forEach(ability => {
            const abEl = document.createElement('div');
            abEl.className = 'character-card';
            abEl.style.cursor = 'pointer';
            
            if (selectedAbilityName === ability.title) {
                abEl.style.borderColor = 'var(--accent)';
                abEl.style.boxShadow = "0 0 8px rgba(212, 175, 55, 0.4)";
            } else {
                abEl.style.borderColor = 'var(--line)';
            }
            
            const iconUrl = `/api/icon/ability?title=${encodeURIComponent(ability.title)}`;
            abEl.innerHTML = `
                <div class="avatar" style="background-image: url('${iconUrl}'); background-size: cover; background-position: center; background-repeat: no-repeat; border-color: var(--line); display: flex; align-items: center; justify-content: center; font-size: 10px; color: var(--accent); font-family: 'Cinzel', serif; position: relative;">
                    <span style="position: absolute; bottom: 2px; right: 4px; font-size: 8px; background: rgba(0,0,0,0.85); padding: 1px 3px; border-radius: 2px; color: #fff; font-family: 'Lora', serif;">${ability.cost} AP</span>
                </div>
                <div>
                    <strong>${ability.title}</strong>
                    <p style="margin: 0; font-size: 12px; color: var(--muted);">${ability.description || 'Способность'}</p>
                </div>
            `;
            
            abEl.addEventListener('click', () => {
                selectedAbilityName = ability.title;
                renderCharacterDetails();
                showAbilityDetails(ability.title, 'grimoire');
            });
            grimoireList.appendChild(abEl);
        });
    }
}

async function assignAbility(abilityName) {
    if (selectedSlotIndex === null) return;

    const attackSlots = [...(characterData.attackSlots || [])];
    const attack2Slots = [...(characterData.attack2Slots || [])];
    const defenseSlots = [...(characterData.defenseSlots || [])];

    if (activeTab === 1) {
        attackSlots[selectedSlotIndex] = abilityName;
    } else if (activeTab === 2) {
        attack2Slots[selectedSlotIndex] = abilityName;
    } else {
        defenseSlots[selectedSlotIndex] = abilityName;
    }

    try {
        const response = await apiPost('/api/character/update-skills', {
            playerId: currentSession.playerId,
            attackSlots: attackSlots,
            attack2Slots: attack2Slots,
            defenseSlots: defenseSlots
        });
        if (response.status === 'success') {
            selectedSlotIndex = null;
            selectedAbilityName = null;
            hideDetails();
            await refreshCharacterDetails();
        }
    } catch (err) {
        console.error(err);
    }
}
