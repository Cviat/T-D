import { apiGet, apiPost } from '../api.js';

let currentSession = null;
let characterData = null;
let selectedItem = null; // { type: 'backpack'|'equip', name: string, index?: number, slotName?: string }

export async function initInventoryScreen(session) {
    currentSession = session;
    selectedItem = null;
    hideDetails();
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

function getItemTypeRussian(type) {
    switch (type) {
        case "Helmet": return "Шлем";
        case "Armor": return "Доспех";
        case "Weapon": return "Оружие";
        case "Shield": return "Щит";
        case "Boots": return "Обувь";
        case "Amulet": return "Амулет";
        case "Ring": return "Кольцо";
        case "Artifact": return "Артефакт";
        case "Belt": return "Пояс";
        default: return "Общий";
    }
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

function showItemDetails(itemName, isEquipped, slotOrIndex) {
    const detailsPanel = document.getElementById('item-details-panel');
    const contentEl = document.getElementById('item-details-content');
    
    if (!itemName) {
        hideDetails();
        return;
    }

    const item = characterData.allItems.find(i => i.title === itemName);
    if (!item) {
        hideDetails();
        return;
    }

    detailsPanel.style.display = 'block';

    const statParts = [];
    if (item.armorPoints) statParts.push(`<span style="color: #6fb9ff; font-weight: bold;">+${item.armorPoints} Броня</span>`);
    if (item.bonusHp) statParts.push(`<span style="color: #ff6b6b; font-weight: bold;">+${item.bonusHp} HP</span>`);
    if (item.bonusStr) statParts.push(`<span style="color: var(--accent);">+${item.bonusStr} Сила (STR)</span>`);
    if (item.bonusAgi) statParts.push(`<span style="color: var(--accent);">+${item.bonusAgi} Ловкость (AGI)</span>`);
    if (item.bonusInt) statParts.push(`<span style="color: var(--accent);">+${item.bonusInt} Интеллект (INT)</span>`);
    if (item.bonusHol) statParts.push(`<span style="color: var(--accent);">+${item.bonusHol} Святость (HOL)</span>`);
    
    if (item.itemType === "Weapon") {
        statParts.push(`<div>Тип атаки: <strong>${getAttackTypeRussian(item.attackType)}</strong></div>`);
        if (item.scaleStat1 && item.scaleStat1 !== "None") {
            statParts.push(`<div>Масштабирование 1: <strong>${item.scaleStat1} x${item.coef1}</strong></div>`);
        }
        if (item.scaleStat2 && item.scaleStat2 !== "None") {
            statParts.push(`<div>Масштабирование 2: <strong>${item.scaleStat2} x${item.coef2}</strong></div>`);
        }
    }

    const statsHtml = statParts.length > 0 
        ? `<div style="margin: 8px 0; padding: 6px; border-left: 2px solid var(--accent); background: rgba(255,255,255,0.03);">${statParts.join('<br>')}</div>` 
        : '';

    const buttonHtml = isEquipped 
        ? `<button id="item-action-btn" class="button danger" style="width: 100%; margin-top: 12px; min-height: 40px;" type="button">Снять предмет</button>`
        : `<button id="item-action-btn" class="button success" style="width: 100%; margin-top: 12px; min-height: 40px;" type="button">Экипировать предмет</button>`;

    contentEl.innerHTML = `
        <div style="display: flex; gap: 12px; align-items: center; margin-bottom: 10px;">
            <div style="width: 48px; height: 48px; border: 2px solid var(--accent); border-radius: 4px; background-image: url('/api/icon/item?title=${encodeURIComponent(item.title)}'); background-size: cover; background-position: center; background-repeat: no-repeat;"></div>
            <div>
                <h3 style="margin: 0; color: var(--accent); font-family: 'Cinzel', serif; font-size: 16px;">${item.title}</h3>
                <span class="muted" style="font-size: 11px;">Тип: ${getItemTypeRussian(item.itemType)}</span>
            </div>
        </div>
        <p style="margin: 4px 0 8px 0; font-style: italic; color: #ccc;">${item.description || 'Нет описания.'}</p>
        ${statsHtml}
        ${buttonHtml}
    `;

    document.getElementById('item-action-btn').addEventListener('click', async () => {
        if (isEquipped) {
            await unequipItem(slotOrIndex, itemName);
        } else {
            await equipItem(itemName, slotOrIndex);
        }
    });
}

function hideDetails() {
    const detailsPanel = document.getElementById('item-details-panel');
    if (detailsPanel) detailsPanel.style.display = 'none';
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
            el.style.textShadow = "0 1px 3px #000, 0 0 2px #000";
        }

        if (slotEl) {
            slotEl.style.boxShadow = '';
            
            if (selectedItem?.type === 'equip' && selectedItem?.slotName === slot) {
                slotEl.style.borderColor = "var(--accent)";
                slotEl.style.boxShadow = "0 0 8px rgba(212, 175, 55, 0.5)";
            } else {
                slotEl.style.borderColor = "var(--line)";
            }

            if (value) {
                const iconUrl = `/api/icon/item?title=${encodeURIComponent(value)}`;
                slotEl.style.backgroundImage = `url('${iconUrl}')`;
                slotEl.style.backgroundSize = 'cover';
                slotEl.style.backgroundPosition = 'center';
                slotEl.style.backgroundRepeat = 'no-repeat';
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
        slotEl.style.cssText = "border: 2px solid var(--line); padding: 10px; border-radius: 4px; background: rgba(0,0,0,0.5); text-align: center; cursor: pointer; min-height: 56px; display: flex; align-items: center; justify-content: center; font-size: 13px; text-shadow: 0 1px 3px #000, 0 0 2px #000; font-weight: bold; position: relative;";
        slotEl.textContent = item || "Пусто";
        
        if (selectedItem?.type === 'backpack' && selectedItem?.index === i) {
            slotEl.style.borderColor = "var(--accent)";
            slotEl.style.boxShadow = "0 0 8px rgba(212, 175, 55, 0.5)";
        }

        if (item) {
            slotEl.style.color = "#fff";
            
            const iconUrl = `/api/icon/item?title=${encodeURIComponent(item)}`;
            slotEl.style.backgroundImage = `url('${iconUrl}')`;
            slotEl.style.backgroundSize = 'cover';
            slotEl.style.backgroundPosition = 'center';
            slotEl.style.backgroundRepeat = 'no-repeat';
            
            slotEl.addEventListener('click', () => {
                selectedItem = { type: 'backpack', name: item, index: i };
                renderInventory();
                showItemDetails(item, false, i);
            });
        } else {
            slotEl.style.color = "#666";
            slotEl.style.backgroundImage = '';
            slotEl.addEventListener('click', () => {
                selectedItem = null;
                renderInventory();
                hideDetails();
            });
        }
        bpContainer.appendChild(slotEl);
    }

    // Add click events to equip slots to show details
    document.querySelectorAll('.equip-slot').forEach(slotEl => {
        const newSlotEl = slotEl.cloneNode(true);
        slotEl.parentNode.replaceChild(newSlotEl, slotEl);

        const slotName = newSlotEl.dataset.slot;
        const currentItem = characterData[slotName];

        newSlotEl.addEventListener('click', () => {
            if (currentItem) {
                selectedItem = { type: 'equip', name: currentItem, slotName: slotName };
                renderInventory();
                showItemDetails(currentItem, true, slotName);
            } else {
                selectedItem = null;
                renderInventory();
                hideDetails();
            }
        });
    });
}

async function equipItem(itemName, backpackIndex) {
    const item = characterData.allItems.find(i => i.title === itemName);
    if (!item) return;

    let slotName = "";
    switch (item.itemType) {
        case "Helmet": slotName = "eqHelmet"; break;
        case "Armor": slotName = "eqArmor"; break;
        case "Weapon": 
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
            selectedItem = null;
            hideDetails();
            await refreshInventory();
        }
    } catch (err) {
        console.error(err);
    }
}

async function unequipItem(slotName, itemName) {
    const emptyIndex = characterData.backpackSlots.findIndex(s => !s);
    if (emptyIndex === -1) {
        alert("Нет свободного места в рюкзаке для снятия предмета!");
        return;
    }

    try {
        const response = await apiPost('/api/character/equip-item', {
            playerId: currentSession.playerId,
            slotName: slotName,
            itemName: "", 
            backpackIndex: emptyIndex 
        });
        if (response.status === 'success') {
            selectedItem = null;
            hideDetails();
            await refreshInventory();
        }
    } catch (err) {
        console.error(err);
    }
}
