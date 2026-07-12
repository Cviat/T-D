import { apiGet, apiPost } from '../api.js';

let currentSession = null;
let characterData = null;

export async function initInventoryScreen(session) {
    currentSession = session;
    await refreshInventory();
}

export async function refreshInventory() {
    if (!currentSession?.playerId) return;
    try {
        const data = await apiGet(`/api/character/details?playerId=${encodeURIComponent(currentSession.playerId)}`);
        characterData = data;
        renderInventory();
    } catch (err) {
        console.error("Failed to load character details for inventory:", err);
    }
}

function renderInventory() {
    if (!characterData) return;

    // Render equipment slots
    const slots = [
        "eqHelmet", "eqArmor", "eqWeapon", "eqWeapon2", "eqShield", 
        "eqBoots", "eqAmulet", "eqRing", "eqArtifact", "eqBelt"
    ];

    slots.forEach(slot => {
        const value = characterData[slot] || "";
        const cleanSlot = slot.replace("eq", ""); // eqHelmet -> Helmet
        const el = document.getElementById(`eq-${cleanSlot}`);
        if (el) {
            el.textContent = value || "Пусто";
            el.style.color = value ? "var(--accent)" : "#888";
        }
    });

    // Render backpack slots (8 slots)
    const bpContainer = document.getElementById('backpack-container');
    bpContainer.innerHTML = '';
    
    for (let i = 0; i < 8; i++) {
        const item = characterData.backpackSlots?.[i] || "";
        const slotEl = document.createElement('div');
        slotEl.style.cssText = "border: 2px solid var(--line); padding: 10px; border-radius: 4px; background: rgba(0,0,0,0.5); text-align: center; cursor: pointer; min-height: 48px; display: flex; align-items: center; justify-content: center; font-size: 13px;";
        slotEl.textContent = item || "Пусто";
        if (item) {
            slotEl.style.color = "#fff";
            slotEl.style.borderColor = "var(--line-active)";
            // Add click event to equip item
            slotEl.addEventListener('click', () => equipItem(item, i));
        } else {
            slotEl.style.color = "#666";
        }
        bpContainer.appendChild(slotEl);
    }

    // Render item pool (all available items)
    const poolList = document.getElementById('item-pool-list');
    poolList.innerHTML = '';
    
    if (characterData.allItems) {
        characterData.allItems.forEach(item => {
            const itemEl = document.createElement('div');
            itemEl.className = 'character-card'; // Reuse style for nice fantasy frame
            itemEl.style.cursor = 'pointer';
            itemEl.innerHTML = `
                <div class="avatar" style="background-image: none; border-color: var(--line); display: flex; align-items: center; justify-content: center; font-size: 10px; color: var(--accent); font-family: 'Cinzel', serif;">
                    ${item.itemType[0]}
                </div>
                <div>
                    <strong>${item.title}</strong>
                    <p style="margin: 0; font-size: 12px; color: var(--muted);">${item.description || 'Снаряжение'}</p>
                </div>
            `;
            itemEl.addEventListener('click', () => addLootToBackpack(item.title));
            poolList.appendChild(itemEl);
        });
    }

    // Add click events to equip slots to unequip item
    document.querySelectorAll('.equip-slot').forEach(slotEl => {
        // Clone node to remove previous listeners
        const newSlotEl = slotEl.cloneNode(true);
        slotEl.parentNode.replaceChild(newSlotEl, slotEl);

        const slotName = newSlotEl.dataset.slot;
        const currentItem = characterData[slotName];

        if (currentItem) {
            newSlotEl.addEventListener('click', () => unequipItem(slotName, currentItem));
        }
    });
}

async function addLootToBackpack(itemName) {
    // Find first empty backpack slot index
    const emptyIndex = characterData.backpackSlots.findIndex(s => !s);
    if (emptyIndex === -1) {
        alert("Рюкзак полон!");
        return;
    }

    try {
        const response = await apiPost('/api/character/equip-item', {
            playerId: currentSession.playerId,
            slotName: "",
            itemName: itemName,
            backpackIndex: emptyIndex
        });
        if (response.status === 'success') {
            await refreshInventory();
        }
    } catch (err) {
        console.error(err);
    }
}

async function equipItem(itemName, backpackIndex) {
    // Decide slot based on itemType
    const item = characterData.allItems.find(i => i.title === itemName);
    if (!item) return;

    let slotName = "";
    switch (item.itemType) {
        case "Helmet": slotName = "eqHelmet"; break;
        case "Armor": slotName = "eqArmor"; break;
        case "Weapon": 
            // Equip in eqWeapon (Weapon 1) by default. If it has something, we can equip it in eqWeapon2 (Weapon 2).
            if (!characterData.eqWeapon) slotName = "eqWeapon";
            else slotName = "eqWeapon2";
            break;
        case "Shield": slotName = "eqShield"; break;
        case "Boots": slotName = "eqBoots"; break;
        case "Amulet": slotName = "eqAmulet"; break;
        case "Ring": slotName = "eqRing"; break;
        case "Artifact": slotName = "eqArtifact"; break;
        case "Belt": slotName = "eqBelt"; break;
        default:
            alert("Этот предмет нельзя экипировать!");
            return;
    }

    try {
        const response = await apiPost('/api/character/equip-item', {
            playerId: currentSession.playerId,
            slotName: slotName,
            itemName: itemName,
            backpackIndex: backpackIndex
        });
        if (response.status === 'success') {
            await refreshInventory();
        }
    } catch (err) {
        console.error(err);
    }
}

async function unequipItem(slotName, itemName) {
    // Find first empty backpack slot index
    const emptyIndex = characterData.backpackSlots.findIndex(s => !s);
    if (emptyIndex === -1) {
        alert("Нет свободного места в рюкзаке для снятия предмета!");
        return;
    }

    try {
        const response = await apiPost('/api/character/equip-item', {
            playerId: currentSession.playerId,
            slotName: slotName,
            itemName: "", // Unequip (set slot to empty)
            backpackIndex: emptyIndex // Move the unequipped item to backpack index
        });
        if (response.status === 'success') {
            await refreshInventory();
        }
    } catch (err) {
        console.error(err);
    }
}
