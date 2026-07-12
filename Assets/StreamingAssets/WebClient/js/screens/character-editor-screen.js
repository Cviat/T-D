import { apiGet, apiPost } from '../api.js';

let currentSession = null;
let characterData = null;
let activeTab = 1; // 1 = Weapon 1, 2 = Weapon 2
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

    // Clone to remove previous event listeners
    const newTab1 = tab1.cloneNode(true);
    tab1.parentNode.replaceChild(newTab1, tab1);
    const newTab2 = tab2.cloneNode(true);
    tab2.parentNode.replaceChild(newTab2, tab2);

    newTab1.addEventListener('click', () => {
        activeTab = 1;
        newTab1.classList.add('active');
        newTab2.classList.remove('active');
        selectedSlotIndex = null;
        renderCharacterDetails();
    });

    newTab2.addEventListener('click', () => {
        activeTab = 2;
        newTab2.classList.add('active');
        newTab1.classList.remove('active');
        selectedSlotIndex = null;
        renderCharacterDetails();
    });
}

function renderCharacterDetails() {
    if (!characterData) return;

    // Render Attributes
    document.getElementById('char-str').textContent = characterData.strength || "-";
    document.getElementById('char-agi').textContent = characterData.agility || "-";
    document.getElementById('char-int').textContent = characterData.intelligence || "-";
    document.getElementById('char-hol').textContent = characterData.holiness || "-";

    // Render Ability Slots (6 slots)
    const slotsContainer = document.getElementById('skills-slots-container');
    slotsContainer.innerHTML = '';

    const currentSlots = activeTab === 1 ? characterData.attackSlots : characterData.attack2Slots;

    for (let i = 0; i < 6; i++) {
        const abilityName = currentSlots?.[i] || "";
        const slotEl = document.createElement('div');
        slotEl.style.cssText = "border: 2px solid var(--line); padding: 8px; border-radius: 4px; background: rgba(0,0,0,0.3); text-align: center; cursor: pointer; min-height: 48px; display: flex; flex-direction: column; align-items: center; justify-content: center;";
        
        if (selectedSlotIndex === i) {
            slotEl.style.borderColor = "var(--accent)";
            slotEl.style.boxShadow = "0 0 8px rgba(212, 175, 55, 0.4)";
        }

        const slotNum = document.createElement('span');
        slotNum.style.cssText = "font-size: 9px; color: var(--muted); text-transform: uppercase;";
        slotNum.textContent = `Слот ${i + 1}`;
        slotEl.appendChild(slotNum);

        const skillNameEl = document.createElement('strong');
        skillNameEl.style.cssText = "font-size: 12px; color: #fff; display: block; margin-top: 2px;";
        skillNameEl.textContent = abilityName || "Пусто";
        if (!abilityName) skillNameEl.style.color = "#555";
        slotEl.appendChild(skillNameEl);

        slotEl.addEventListener('click', () => {
            selectedSlotIndex = i;
            renderCharacterDetails();
        });

        slotsContainer.appendChild(slotEl);
    }

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

    if (characterData.allAbilities) {
        characterData.allAbilities.forEach(ability => {
            const abEl = document.createElement('div');
            abEl.className = 'character-card';
            abEl.style.cursor = selectedSlotIndex !== null ? 'pointer' : 'default';
            if (selectedSlotIndex !== null) {
                abEl.style.borderColor = 'var(--line-active)';
            }
            abEl.innerHTML = `
                <div class="avatar" style="background-image: none; border-color: var(--line); display: flex; align-items: center; justify-content: center; font-size: 10px; color: var(--accent); font-family: 'Cinzel', serif;">
                    ${ability.cost} AP
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

    if (activeTab === 1) {
        attackSlots[selectedSlotIndex] = abilityName;
    } else {
        attack2Slots[selectedSlotIndex] = abilityName;
    }

    try {
        const response = await apiPost('/api/character/update-skills', {
            playerId: currentSession.playerId,
            attackSlots: attackSlots,
            attack2Slots: attack2Slots
        });
        if (response.status === 'success') {
            selectedSlotIndex = null; // Reset selection
            await refreshCharacterDetails();
        }
    } catch (err) {
        console.error(err);
    }
}
