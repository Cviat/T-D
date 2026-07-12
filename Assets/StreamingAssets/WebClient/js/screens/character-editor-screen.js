import { apiGet, apiPost } from '../api.js';

let currentSession = null;
let characterData = null;
let activeTab = 1; // 1 = Weapon 1, 2 = Weapon 2, 3 = Defense
let selectedSlotIndex = null; // Clicked slot to assign ability to

export async function initCharacterEditorScreen(session) {
    currentSession = session;
    selectedSlotIndex = null;
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
        renderCharacterDetails();
    });

    newTab2.addEventListener('click', () => {
        activeTab = 2;
        newTab2.classList.add('active');
        newTab1.classList.remove('active');
        newTab3.classList.remove('active');
        selectedSlotIndex = null;
        renderCharacterDetails();
    });

    newTab3.addEventListener('click', () => {
        activeTab = 3;
        newTab3.classList.add('active');
        newTab1.classList.remove('active');
        newTab2.classList.remove('active');
        selectedSlotIndex = null;
        renderCharacterDetails();
    });
}

function getActiveWeaponAttackType() {
    const weaponName = activeTab === 1 ? characterData.eqWeapon : characterData.eqWeapon2;
    if (!weaponName) return "Melee"; // Default to Melee (Unarmed/Fists)
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

function renderCharacterDetails() {
    if (!characterData) return;

    // Render Attributes
    document.getElementById('char-str').textContent = characterData.strength || "-";
    document.getElementById('char-agi').textContent = characterData.agility || "-";
    document.getElementById('char-int').textContent = characterData.intelligence || "-";
    document.getElementById('char-hol').textContent = characterData.holiness || "-";

    // Determine current attack type requirements for the tab
    const isDefense = activeTab === 3;
    const requiredType = isDefense ? "Defense" : getActiveWeaponAttackType();
    
    // Update instruction text with weapon and type
    const instructionEl = document.querySelector('.skills-instruction');
    if (instructionEl) {
        instructionEl.innerHTML = `
            <div>Выбранное снаряжение: <strong>${getWeaponDisplayName()}</strong></div>
            <div>Тип способностей: <span style="color: var(--accent);">${getAttackTypeRussian(requiredType)}</span></div>
            <div style="font-size: 11px; margin-top: 4px; color: var(--muted);">Нажмите на слот выше, затем выберите способность ниже</div>
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
        slotEl.style.cssText = "border: 2px solid var(--line); padding: 8px; border-radius: 4px; background: rgba(0,0,0,0.3); text-align: center; cursor: pointer; min-height: 56px; display: flex; flex-direction: column; align-items: center; justify-content: center; position: relative;";
        
        if (selectedSlotIndex === i) {
            slotEl.style.borderColor = "var(--accent)";
            slotEl.style.boxShadow = "0 0 8px rgba(212, 175, 55, 0.4)";
        }

        if (abilityName) {
            const iconUrl = `/api/icon/ability?title=${encodeURIComponent(abilityName)}`;
            slotEl.style.backgroundImage = `linear-gradient(180deg, rgba(0, 0, 0, 0.45) 0%, rgba(0, 0, 0, 0.9) 100%), url('${iconUrl}')`;
            slotEl.style.backgroundSize = 'cover';
            slotEl.style.backgroundPosition = 'center';
        }

        const slotNum = document.createElement('span');
        slotNum.style.cssText = "font-size: 8px; color: var(--muted); text-transform: uppercase; z-index: 1;";
        slotNum.textContent = `Слот ${i + 1}`;
        slotEl.appendChild(slotNum);

        const skillNameEl = document.createElement('strong');
        skillNameEl.style.cssText = "font-size: 11px; color: #fff; display: block; margin-top: 2px; z-index: 1;";
        skillNameEl.textContent = abilityName || "Пусто";
        if (!abilityName) skillNameEl.style.color = "#555";
        slotEl.appendChild(skillNameEl);

        slotEl.addEventListener('click', () => {
            selectedSlotIndex = i;
            renderCharacterDetails();
        });

        slotsContainer.appendChild(slotEl);
    }

    // Filter available abilities by required type
    const filteredAbilities = (characterData.allAbilities || []).filter(a => a.attackType === requiredType);

    // Render Grimoire List
    const grimoireList = document.getElementById('grimoire-list');
    grimoireList.innerHTML = '';

    // Add a special "Убрать" (Clear) ability option at the top if a slot is selected
    if (selectedSlotIndex !== null) {
        const clearEl = document.createElement('div');
        clearEl.className = 'character-card';
        clearEl.style.cssText = "cursor: pointer; border-color: var(--danger); background: rgba(156, 42, 42, 0.1);";
        clearEl.innerHTML = `
            <div class="avatar" style="background-image: none; border-color: var(--danger); display: flex; align-items: center; justify-content: center; font-size: 14px; color: var(--danger); font-family: 'Cinzel', serif;">
                ✖
            </div>
            <div>
                <strong style="color: #ff6b6b;">Освободить слот</strong>
                <p style="margin: 0; font-size: 12px; color: var(--muted);">Очистить способность в выбранном слоте</p>
            </div>
        `;
        clearEl.addEventListener('click', () => assignAbility(""));
        grimoireList.appendChild(clearEl);
    }

    if (filteredAbilities.length === 0) {
        const emptyEl = document.createElement('div');
        emptyEl.style.cssText = "text-align: center; color: var(--muted); padding: 20px; font-size: 13px;";
        emptyEl.textContent = "Нет доступных способностей для этого типа атаки.";
        grimoireList.appendChild(emptyEl);
    } else {
        filteredAbilities.forEach(ability => {
            const abEl = document.createElement('div');
            abEl.className = 'character-card';
            abEl.style.cursor = selectedSlotIndex !== null ? 'pointer' : 'default';
            if (selectedSlotIndex !== null) {
                abEl.style.borderColor = 'var(--line-active)';
            }
            
            const iconUrl = `/api/icon/ability?title=${encodeURIComponent(ability.title)}`;
            abEl.innerHTML = `
                <div class="avatar" style="background-image: url('${iconUrl}'); background-size: cover; background-position: center; border-color: var(--line); display: flex; align-items: center; justify-content: center; font-size: 10px; color: var(--accent); font-family: 'Cinzel', serif; position: relative;">
                    <span style="position: absolute; bottom: 2px; right: 4px; font-size: 8px; background: rgba(0,0,0,0.85); padding: 1px 3px; border-radius: 2px; color: #fff; font-family: 'Lora', serif;">${ability.cost} AP</span>
                </div>
                <div>
                    <strong>${ability.title}</strong>
                    <p style="margin: 0; font-size: 12px; color: var(--muted);">${ability.description || 'Способность'}</p>
                </div>
            `;
            
            if (selectedSlotIndex !== null) {
                abEl.addEventListener('click', () => assignAbility(ability.title));
            }
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
            selectedSlotIndex = null; // Reset selection
            await refreshCharacterDetails();
        }
    } catch (err) {
        console.error(err);
    }
}
