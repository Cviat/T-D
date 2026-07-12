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
        const slotEl = document.querySelector(`.equip-slot[data-slot="${slot}"]`);

        if (el) {
            el.textContent = value || "Пусто";
            el.style.color = value ? "var(--accent)" : "#888";
        }

        if (slotEl) {
            if (value) {
                const iconUrl = `/api/icon/item?title=${encodeURIComponent(value)}`;
                slotEl.style.backgroundImage = `linear-gradient(180deg, rgba(0, 0, 0, 0.45) 0%, rgba(0, 0, 0, 0.9) 100%), url('${iconUrl}')`;
                slotEl.style.backgroundSize = 'cover';
                slotEl.style.backgroundPosition = 'center';
            } else {
                slotEl.style.backgroundImage = '';
            }
        }
    });

    // Render backpack slots (8 slots)
    const bpContainer = document.getElementById('backpack-container');
    bpContainer.innerHTML = '';
    
    for (let i = 0; i < 8; i++) {
        const item = characterData.backpackSlots?.[i] || "";
        const slotEl = document.createElement('div');
        slotEl.style.cssText = "border: 2px solid var(--line); padding: 10px; border-radius: 4px; background: rgba(0,0,0,0.5); text-align: center; cursor: pointer; min-height: 54px; display: flex; align-items: center; justify-content: center; font-size: 13px; position: relative;";
        slotEl.textContent = item || "Пусто";
        
        if (item) {
            slotEl.style.color = "#fff";
            slotEl.style.borderColor = "var(--line-active)";
            
            const iconUrl = `/api/icon/item?title=${encodeURIComponent(item)}`;
            slotEl.style.backgroundImage = `linear-gradient(180deg, rgba(0, 0, 0, 0.3) 0%, rgba(0, 0, 0, 0.8) 100%), url('${iconUrl}')`;
            slotEl.style.backgroundSize = 'cover';
            slotEl.style.backgroundPosition = 'center';
            
            // Add click event to equip item
            slotEl.addEventListener('click', () => equipItem(item, i));
        } else {
            slotEl.style.color = "#666";
            slotEl.style.backgroundImage = '';
        }
        bpContainer.appendChild(slotEl);
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
