const heroes = [
    {
        id: 1,
        name: "Jenna Cirai",
        rarity: 1,
        class: "Vanguard",
        deployed: true,
        fatigueValue: 65, // Strained
        cp: 1450,
        traits: ["Heart of Iron", "Sect Survivor"],
        traumas: [],
        imageUrl: "file:///C:/Users/hp/.gemini/antigravity/brain/655856f2-a099-450e-9c69-72173e9b9b84/hero_jenna_1779266230479.png"
    },
    {
        id: 2,
        name: "Han Israt",
        rarity: 2,
        class: "Assassin",
        deployed: false,
        fatigueValue: 15, // Fresh
        cp: 2100,
        traits: ["Qi Flow"],
        traumas: [],
        imageUrl: "file:///C:/Users/hp/.gemini/antigravity/brain/655856f2-a099-450e-9c69-72173e9b9b84/hero_han_1779266258662.png"
    },
    {
        id: 3,
        name: "Aaron Delcut",
        rarity: 1,
        class: "Acolyte",
        deployed: false,
        fatigueValue: 85, // Breaking Point
        cp: 890,
        traits: [],
        traumas: ["Paralytic Dread", "Maimed Meridian"],
        imageUrl: "file:///C:/Users/hp/.gemini/antigravity/brain/655856f2-a099-450e-9c69-72173e9b9b84/hero_aaron_1779266285664.png"
    },
    {
        id: 4,
        name: "Gide",
        rarity: 3,
        class: "Immortal",
        deployed: true,
        fatigueValue: 45, // Strained
        cp: 5400,
        traits: ["Heart of Iron", "Qi Flow"],
        traumas: ["Blood-Stained Guilt"],
        imageUrl: "file:///C:/Users/hp/.gemini/antigravity/brain/655856f2-a099-450e-9c69-72173e9b9b84/hero_han_1779266258662.png" // Reusing image
    }
];

function getFatigueLevel(val) {
    if (val < 30) return { class: 'condition-green', color: 'var(--fresh-green)', text: 'Fresh' };
    if (val < 75) return { class: 'condition-yellow', color: 'var(--strained-yellow)', text: 'Strained' };
    return { class: 'condition-red', color: 'var(--danger-red)', text: 'Breaking Point' };
}

document.addEventListener('DOMContentLoaded', () => {
    const grid = document.getElementById('hero-grid');
    const template = document.getElementById('hero-card-template');
    const detailPanel = document.getElementById('detail-panel');

    let selectedHeroId = null;

    // Render Grid
    heroes.forEach(hero => {
        const clone = template.content.cloneNode(true);
        const card = clone.querySelector('.hero-card');
        
        card.dataset.id = hero.id;
        
        // Background Image
        card.querySelector('.card-bg').style.backgroundImage = `url('${hero.imageUrl}')`;
        
        // Condition Icon
        const fLevel = getFatigueLevel(hero.fatigueValue);
        card.querySelector('.condition-icon').classList.add(fLevel.class);
        
        // Deployment Badge
        if (hero.deployed) {
            card.querySelector('.deployment-badge').classList.remove('hidden');
        }
        
        // Bottom details
        card.querySelector('.stars').textContent = '★'.repeat(hero.rarity);
        card.querySelector('.hero-name').textContent = hero.name;
        card.querySelector('.hero-class').textContent = hero.class;
        card.querySelector('.hero-cp').textContent = `CP ${hero.cp}`;

        // Click Event
        card.addEventListener('click', () => selectHero(hero.id, card));

        grid.appendChild(clone);
    });

    function selectHero(id, cardElement) {
        // Update selection UI
        document.querySelectorAll('.hero-card').forEach(c => c.classList.remove('selected'));
        if (cardElement) cardElement.classList.add('selected');
        
        const hero = heroes.find(h => h.id === id);
        if (!hero) return;

        renderDetailPanel(hero);
        detailPanel.classList.remove('hidden');
    }

    function renderDetailPanel(hero) {
        const fLevel = getFatigueLevel(hero.fatigueValue);
        const lockedState = hero.deployed ? 'locked' : '';
        const disabledState = hero.deployed ? 'disabled' : '';
        const lockIcon = hero.deployed ? '🔒 Deployed - Actions Locked' : '';

        // Generate Traits HTML
        let traitsHtml = '';
        if (hero.traits.length > 0) {
            traitsHtml += `<div class="section-title">Positive Traits</div>`;
            hero.traits.forEach(t => {
                traitsHtml += `<div class="trait-item">${t}</div>`;
            });
        }
        if (hero.traumas.length > 0) {
            traitsHtml += `<div class="section-title">Negative Traumas</div>`;
            hero.traumas.forEach(t => {
                traitsHtml += `<div class="trait-item negative">${t}</div>`;
            });
        }
        if (!traitsHtml) traitsHtml = '<div class="trait-item" style="color:#666">No traits acquired yet.</div>';

        detailPanel.innerHTML = `
            <div class="detail-header">
                <div class="detail-portrait" style="background-image: url('${hero.imageUrl}')"></div>
                <div class="detail-info">
                    <div class="detail-name">${hero.name}</div>
                    <div class="stars"> ${'★'.repeat(hero.rarity)} </div>
                    <div class="detail-class-realm">${hero.class}</div>
                    <div class="detail-cp">${hero.cp} CP</div>
                </div>
            </div>

            <div class="fatigue-section">
                <div class="fatigue-header">
                    <span>Soul Fatigue</span>
                    <span style="color: ${fLevel.color}">${fLevel.text} (${hero.fatigueValue}%)</span>
                </div>
                <div class="fatigue-bar-bg">
                    <div class="fatigue-bar-fill" style="width: ${hero.fatigueValue}%; background-color: ${fLevel.color}"></div>
                </div>
                ${hero.deployed ? `<div style="font-size: 10px; color: #aaa; margin-top: 5px; text-align:right;">${lockIcon}</div>` : ''}
            </div>

            <div class="section-title">Gear Loadout</div>
            <div class="gear-grid">
                <div class="gear-slot ${lockedState}" title="Weapon">⚔️</div>
                <div class="gear-slot ${lockedState}" title="Armor">🛡️</div>
                <div class="gear-slot ${lockedState}" title="Accessory">💍</div>
                <div class="gear-slot ${lockedState}" title="Artifact">🔮</div>
            </div>

            <div class="traits-section">
                ${traitsHtml}
            </div>

            <div class="action-buttons">
                <button class="btn" ${disabledState}>Facility Assignment</button>
                <button class="btn" ${disabledState}>Dismiss</button>
            </div>
        `;
    }
});
