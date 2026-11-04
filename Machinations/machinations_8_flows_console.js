(async () => {
  try {
    // Dismiss cookie banner if visible
    const btns = Array.from(document.querySelectorAll('button, .v-btn, div, span'));
    const allowBtn = btns.find(b => b.innerText && b.innerText.trim() === 'Allow All Cookies');
    if (allowBtn) allowBtn.click();
    document.querySelectorAll('div').forEach(el => {
      if (el.innerText && el.innerText.includes('We use cookies to analyze traffic') && el.offsetHeight < 350 && el.offsetWidth < 650) {
        el.style.display = 'none';
      }
    });

    const editorVue = document.querySelector('.m-editorui')?.__vue__;
    if (editorVue && typeof editorVue.triggerAction === 'function') {
      try { editorVue.triggerAction({ name: "stop" }); } catch (e) {}
    }
    await new Promise(r => setTimeout(r, 300));

    let webpackReq = null;
    window.webpackChunkmachinations.push([
      ['__builder8_' + Date.now()],
      {},
      (req) => { webpackReq = req; }
    ]);

    const T = webpackReq(69750).T;
    const m45916 = webpackReq(45916);
    const G = m45916.Ay;
    const BA = m45916.BA;
    const Mq = m45916.Mq;
    const RM = webpackReq(86782).RM;

    const layer = T.selectedLayer() || T.getNode(201) || T.layers()[0];
    T.setReadonly(false);
    T.setPublicApplication(false);
    G.enable();

    // 1. Delete existing diagram nodes & edges (except root/layer nodes <= 205)
    const existingIds = T.getNodeIds().filter(id => id > 205);
    if (existingIds.length > 0) {
      const delChanges = [];
      for (const id of existingIds.filter(i => T.getNode(i)?.edge)) {
        delChanges.push(Mq(T.getNode(id)));
      }
      for (const id of existingIds.filter(i => T.getNode(i) && !T.getNode(i).edge)) {
        delChanges.push(Mq(T.getNode(id)));
      }
      G.addChanges(delChanges, { history: true, record: true, execute: true });
      await new Promise(r => setTimeout(r, 350));
    }

    if (typeof G.reserveNewIdRange === 'function') {
      try { await G.reserveNewIdRange(120); } catch (e) {}
    }
    let fallbackId = Math.max(T.getMaxNodeId() || 300, 500) + 1;
    function nextId() {
      try {
        const id = G.reserveId();
        if (id && id > 0) return id;
      } catch (e) {}
      return fallbackId++;
    }

    function headerStyle(stroke, fill, font) {
      return { fontFamily: "Roboto", fontSize: 12, fontColor: font, strokeColor: stroke, strokeWidth: 2, fillColor: fill };
    }
    function nodeStyle(stroke, fill = "#ffffff") {
      return { fontFamily: "Roboto", fontSize: 12, strokeColor: stroke, strokeWidth: 2, fillColor: fill };
    }

    const nodeDefs = [
      // ==================== 8 SECTION HEADERS ====================
      {
        key: "h1", type: "TextNode", x: 50, y: 20, w: 530, h: 54,
        title: "LUỒNG I: HỆ THỐNG NÔNG TRẠI & SINH TRƯỞNG (121 Ô ĐẤT - 3 STAGES) | 3 Ngày (Carrot x3, Corn x2, Wheat x1) & 6 Ngày (Tomato x1, Cabbage/Cucumber/Chilli x3)",
        style: headerStyle("#16a34a", "#dcfce7", "#14532d")
      },
      {
        key: "h2", type: "TextNode", x: 670, y: 20, w: 540, h: 54,
        title: "LUỒNG II: HỆ THỐNG NẤU ĂN & CHẾ BIẾN COMBO (KITCHEN & CAMP) | Ăn Thô (+5..15 No, Buff 3T) vs. Nấu 7 Món Combo (+25..50 No, +12..18 HP, Buff 2 Trận / 1 Tầng)",
        style: headerStyle("#d97706", "#fef3c7", "#78350f")
      },
      {
        key: "h3", type: "TextNode", x: 1300, y: 20, w: 590, h: 54,
        title: "LUỒNG III: ĐỘ NO 3 NGƯỠNG & SINH TỒN (FULLNESS 3-TIER STATE MACHINE) | Hungry (<30: -2 ATK) | Satiated (30..79: +2 ATK, +1 DEF, +2 Spd) | Overstuffed (>=80: +3 DEF, +2 HP)",
        style: headerStyle("#2563eb", "#dbeafe", "#1e3a8a")
      },
      {
        key: "h4", type: "TextNode", x: 1980, y: 20, w: 560, h: 54,
        title: "LUỒNG IV: CHU KỲ THỜI GIAN, NGÀY & COOLDOWN (DAY MANAGER) | Ngủ (+1d) | Về Từ Dungeon: Days = max(1, Stages/2+Boss) -> Cây Lớn, Boss (3d), Merchant (5d/3d)",
        style: headerStyle("#4f46e5", "#e0e7ff", "#312e81")
      },
      {
        key: "h5", type: "TextNode", x: 50, y: 520, w: 530, h: 54,
        title: "LUỒNG V: TƯỢNG NỮ THẦN — 40 BẬC HIẾN TẾ & CỔNG BOSS (STATUE) | 4 Nhánh x 10 Tiers: T1-4 & T6-9 (Crops+Meals) | Cổng T5 (AncientCrown) & T10 (SlimeCore/DarkEssence)",
        style: headerStyle("#7c3aed", "#f3e8ff", "#4c1d95")
      },
      {
        key: "h6", type: "TextNode", x: 670, y: 520, w: 540, h: 54,
        title: "LUỒNG VI: ẢI PHI CHIẾN ĐẤU — THƯƠNG NHÂN, LỬA TRẠI & SỰ KIỆN | Đổi Hạt 3:1, Đổi Súp 2:1 | Trại: Rest(+10%), Eat(+15%), Cook(+20%), Exercise(+35% Risk Phục Kích)",
        style: headerStyle("#0891b2", "#cffafe", "#164e63")
      },
      {
        key: "h7", type: "TextNode", x: 1300, y: 520, w: 590, h: 54,
        title: "LUỒNG VII: HẦM NGỤC 3 TẦNG, CHIẾN ĐẤU THEO LƯỢT & 7 PHƯỚC LÀNH | F1 (8 Ải, Boss 150 HP) | F2 (16 Ải, Boss 219 HP) | F3 (18 Ải, Boss 330 HP) | Boss <=50%: +5 ATK",
        style: headerStyle("#dc2626", "#fee2e2", "#7f1d1d")
      },
      {
        key: "h8", type: "TextNode", x: 1980, y: 520, w: 560, h: 54,
        title: "LUỒNG VIII: KHO ĐỒ RỦI RO & BẢO HIỂM TỬ TRẬN (BACKPACK VS. HOMECHEST) | Backpack (33 Ô) -> Thắng/Rút Lui Ở Camp: Về HomeChest (An Toàn) | Tử Trận: ClearRunLoot()",
        style: headerStyle("#0f766e", "#ccfbf1", "#134e4a")
      },

      // ==================== LUỒNG I: NÔNG TRẠI (FARMING) ====================
      { key: "seeds", type: "PoolNode", x: 75, y: 195, title: "Seeds (7 Loại Hạt Giống)", number: 8, capacity: -1, displayCapacity: 0, activation: "passive", actionMode: "pull-any", style: nodeStyle("#16a34a") },
      { key: "plantFast", type: "ConverterNode", x: 230, y: 135, title: "Gieo Hạt Nhanh (3d)", activation: "interactive", actionMode: "pull-any", conversion: "single", style: nodeStyle("#16a34a") },
      { key: "plantSlow", type: "ConverterNode", x: 230, y: 260, title: "Gieo Hạt Chậm (6d)", activation: "interactive", actionMode: "pull-any", conversion: "single", style: nodeStyle("#16a34a") },
      { key: "delayFast", type: "DelayNode", x: 385, y: 135, title: "Lớn 3 Ngày (3x1d)", activation: "passive", queue: false, style: nodeStyle("#16a34a") },
      { key: "delaySlow", type: "DelayNode", x: 385, y: 260, title: "Lớn 6 Ngày (3x2d)", activation: "passive", queue: false, style: nodeStyle("#16a34a") },
      { key: "rawCrops", type: "PoolNode", x: 535, y: 195, title: "Raw Crops (Nông Sản Thô)", number: 16, capacity: -1, displayCapacity: 0, activation: "passive", actionMode: "pull-any", style: nodeStyle("#16a34a") },

      // ==================== LUỒNG II: NẤU ĂN (COOKING) ====================
      { key: "eatRaw", type: "ConverterNode", x: 720, y: 135, title: "Ăn Đồ Thô (RawVitality)", activation: "interactive", actionMode: "pull-any", conversion: "single", style: nodeStyle("#d97706") },
      { key: "cookCombo", type: "ConverterNode", x: 720, y: 260, title: "Nấu Combo (2-3 Crops)", activation: "interactive", actionMode: "pull-any", conversion: "single", style: nodeStyle("#d97706") },
      { key: "cookedMeals", type: "PoolNode", x: 900, y: 260, title: "Cooked Meals (7 Món)", number: 3, capacity: -1, displayCapacity: 0, activation: "passive", actionMode: "pull-any", style: nodeStyle("#d97706") },
      { key: "eatCombo", type: "ConverterNode", x: 1080, y: 260, title: "Ăn Món Combo (1 AP/0 AP)", activation: "interactive", actionMode: "pull-any", conversion: "single", style: nodeStyle("#d97706") },
      { key: "foodBuffs", type: "PoolNode", x: 1160, y: 390, title: "Food Buff (+2..3 ATK/DEF)", number: 2, capacity: 10, displayCapacity: 0, activation: "passive", actionMode: "pull-any", style: nodeStyle("#d97706") },

      // ==================== LUỒNG III: ĐỘ NO 3 NGƯỠNG (FULLNESS) ====================
      { key: "hunger", type: "PoolNode", x: 1360, y: 135, title: "Player Hunger (Max 100)", number: 85, capacity: 100, displayCapacity: 0, activation: "passive", actionMode: "pull-any", style: nodeStyle("#d97706") },
      { key: "stageDrain", type: "DrainNode", x: 1560, y: 135, title: "Qua Ải (-12 Hunger)", activation: "passive", actionMode: "pull-any", style: nodeStyle("#d97706") },
      { key: "tierReg", type: "RegisterNode", x: 1760, y: 135, title: "Tier ATK (Hungry/Sat/Over)", formula: "h>=80 ? -1 : (h>=30 ? 2 : -2)", initialValue: 2, style: nodeStyle("#2563eb") },
      { key: "playerHp", type: "PoolNode", x: 1360, y: 270, title: "Player HP (35..165)", number: 35, capacity: 165, displayCapacity: 0, activation: "passive", actionMode: "pull-any", style: nodeStyle("#dc2626") },
      { key: "enemyHitDrain", type: "DrainNode", x: 1360, y: 400, title: "Quái Đánh (-HP Sau Giáp)", activation: "passive", actionMode: "pull-any", style: nodeStyle("#dc2626") },
      { key: "digestRegen", type: "SourceNode", x: 1560, y: 270, title: "Overstuffed Regen (+2 HP)", activation: "passive", actionMode: "push-any", style: nodeStyle("#16a34a") },
      { key: "apPool", type: "PoolNode", x: 1760, y: 270, title: "Action Points (3 AP)", number: 3, capacity: 3, displayCapacity: 0, activation: "passive", actionMode: "pull-any", style: nodeStyle("#2563eb") },
      { key: "shieldPool", type: "PoolNode", x: 1760, y: 400, title: "Shield (+5 Defend)", number: 5, capacity: 30, displayCapacity: 0, activation: "passive", actionMode: "pull-any", style: nodeStyle("#2563eb") },

      // ==================== LUỒNG IV: CHU KỲ NGÀY (DAY & CYCLES) ====================
      { key: "advanceDaySrc", type: "SourceNode", x: 2020, y: 210, title: "Ngủ / Kết Thúc Run", activation: "interactive", actionMode: "push-any", style: nodeStyle("#4f46e5") },
      { key: "dayGate", type: "GateNode", x: 2200, y: 210, title: "Days = max(1, Stages/2+Boss)", activation: "passive", actionMode: "pull-any", distributionMode: "deterministic", style: nodeStyle("#4f46e5") },
      { key: "currentDay", type: "PoolNode", x: 2420, y: 120, title: "Current Day (Ngày)", number: 1, capacity: -1, displayCapacity: 0, activation: "passive", actionMode: "pull-any", style: nodeStyle("#4f46e5") },
      { key: "bossCooldown", type: "PoolNode", x: 2420, y: 225, title: "Boss Cooldown (3 Ngày)", number: 3, capacity: 3, displayCapacity: 0, activation: "passive", actionMode: "pull-any", style: nodeStyle("#dc2626") },
      { key: "merchantStay", type: "PoolNode", x: 2420, y: 335, title: "Merchant (5d Ở / 3d Nghỉ)", number: 5, capacity: 5, displayCapacity: 0, activation: "passive", actionMode: "pull-any", style: nodeStyle("#0891b2") },

      // ==================== LUỒNG V: TƯỢNG NỮ THẦN (STATUE 40 TIERS) ====================
      { key: "statueOffer", type: "ConverterNode", x: 100, y: 680, title: "Hiến Tế Nông Sản (T1-9)", activation: "interactive", actionMode: "pull-any", conversion: "single", style: nodeStyle("#7c3aed") },
      { key: "bossDrops", type: "PoolNode", x: 100, y: 860, title: "Boss Drops (Crown/Core/Ess)", number: 1, capacity: 10, displayCapacity: 0, activation: "passive", actionMode: "pull-any", style: nodeStyle("#7c3aed") },
      { key: "statueBossGate", type: "ConverterNode", x: 260, y: 860, title: "Mở Cổng T5 & T10 (BossDrop)", activation: "interactive", actionMode: "pull-any", conversion: "single", style: nodeStyle("#7c3aed") },
      { key: "statueTrackGate", type: "GateNode", x: 300, y: 680, title: "4 Nhánh Tượng (40 Bậc)", activation: "passive", actionMode: "pull-any", distributionMode: "deterministic", style: nodeStyle("#7c3aed") },
      { key: "permAtk", type: "PoolNode", x: 510, y: 620, title: "Player ATK (Base 7..20)", number: 7, capacity: 20, displayCapacity: 0, activation: "passive", actionMode: "pull-any", style: nodeStyle("#7c3aed") },
      { key: "permDef", type: "PoolNode", x: 510, y: 720, title: "Player DEF (Base 1..13)", number: 1, capacity: 13, displayCapacity: 0, activation: "passive", actionMode: "pull-any", style: nodeStyle("#7c3aed") },
      { key: "permSpd", type: "PoolNode", x: 510, y: 820, title: "Player Speed (Base 14..26)", number: 14, capacity: 26, displayCapacity: 0, activation: "passive", actionMode: "pull-any", style: nodeStyle("#7c3aed") },
      { key: "permMaxHp", type: "PoolNode", x: 510, y: 920, title: "Perm MaxHP (35..165)", number: 35, capacity: 165, displayCapacity: 0, activation: "passive", actionMode: "pull-any", style: nodeStyle("#7c3aed") },

      // ==================== LUỒNG VI: THƯƠNG NHÂN, TRẠI & EVENT ====================
      { key: "tradeSeed3to1", type: "ConverterNode", x: 720, y: 650, title: "Đổi Hạt 3:1 (Merchant)", activation: "interactive", actionMode: "pull-any", conversion: "single", style: nodeStyle("#0891b2") },
      { key: "tradeMeal2to1", type: "ConverterNode", x: 720, y: 780, title: "Đổi Súp/Salad 2:1", activation: "interactive", actionMode: "pull-any", conversion: "single", style: nodeStyle("#0891b2") },
      { key: "campActSrc", type: "SourceNode", x: 940, y: 650, title: "Trại: Rest/Eat/Cook/Ex", activation: "interactive", actionMode: "push-any", style: nodeStyle("#0891b2") },
      { key: "ambushGate", type: "GateNode", x: 1120, y: 650, title: "Ambush Roll (+10..35%)", activation: "passive", actionMode: "pull-any", distributionMode: "dice", style: nodeStyle("#0891b2") },
      { key: "campSafePool", type: "PoolNode", x: 940, y: 780, title: "Camp Rest (+25 HP / Safe)", number: 1, capacity: 10, displayCapacity: 0, activation: "passive", actionMode: "pull-any", style: nodeStyle("#16a34a") },
      { key: "randomEventSrc", type: "SourceNode", x: 940, y: 900, title: "8 Sự Kiện Ngẫu Nhiên", activation: "interactive", actionMode: "push-any", style: nodeStyle("#0891b2") },
      { key: "eventRollGate", type: "GateNode", x: 1120, y: 900, title: "Shrine/Fountain/Blessing", activation: "passive", actionMode: "pull-any", distributionMode: "dice", style: nodeStyle("#0891b2") },

      // ==================== LUỒNG VII: DUNGEON & COMBAT ====================
      { key: "enterWaveSrc", type: "SourceNode", x: 1340, y: 690, title: "Vào Ải Dungeon (Wave)", activation: "interactive", actionMode: "push-any", style: nodeStyle("#dc2626") },
      { key: "waveTypeGate", type: "GateNode", x: 1500, y: 690, title: "Wave Gate (F1:8/F2:16/F3:18)", activation: "passive", actionMode: "pull-any", distributionMode: "dice", style: nodeStyle("#dc2626") },
      { key: "combatTurn", type: "ConverterNode", x: 1680, y: 690, title: "Lượt Chiến Đấu (Combat)", activation: "passive", actionMode: "pull-any", conversion: "single", style: nodeStyle("#dc2626") },
      { key: "dmgReg", type: "RegisterNode", x: 1500, y: 860, title: "Player DMG (max(1,a+b+c-2))", formula: "max(1, a+b+c-2)", initialValue: 9, style: nodeStyle("#dc2626") },
      { key: "dealDmgDrain", type: "DrainNode", x: 1680, y: 860, title: "Đánh Quái (Deal DMG)", activation: "passive", actionMode: "pull-any", style: nodeStyle("#dc2626") },
      { key: "enemyHp", type: "PoolNode", x: 1850, y: 860, title: "Enemy/Boss HP (38..330)", number: 150, capacity: 330, displayCapacity: 0, activation: "passive", actionMode: "pull-any", style: nodeStyle("#dc2626") },
      { key: "lootGate", type: "GateNode", x: 1850, y: 690, title: "Rơi Đồ (Seed / BossDrop)", activation: "passive", actionMode: "pull-any", distributionMode: "dice", style: nodeStyle("#16a34a") },

      // ==================== LUỒNG VIII: KHO ĐỒ & BẢO HIỂM TỬ TRẬN ====================
      { key: "backpack", type: "PoolNode", x: 2040, y: 690, title: "Backpack (33 Ô Mang Đi)", number: 6, capacity: 33, displayCapacity: 0, activation: "passive", actionMode: "pull-any", style: nodeStyle("#0f766e") },
      { key: "extractConv", type: "ConverterNode", x: 2210, y: 690, title: "Kết Thúc Run / Rút Lui", activation: "interactive", actionMode: "pull-any", conversion: "single", style: nodeStyle("#0f766e") },
      { key: "runOutcomeGate", type: "GateNode", x: 2360, y: 690, title: "Thắng/KeepLoot vs. Tử Trận", activation: "passive", actionMode: "pull-any", distributionMode: "dice", style: nodeStyle("#0f766e") },
      { key: "homeChest", type: "PoolNode", x: 2510, y: 620, title: "HomeChest (33 Ô An Toàn)", number: 10, capacity: 33, displayCapacity: 0, activation: "passive", actionMode: "pull-any", style: nodeStyle("#16a34a") },
      { key: "clearLootDrain", type: "DrainNode", x: 2510, y: 770, title: "ClearRunLoot (Chết Mất Đồ)", activation: "passive", actionMode: "pull-any", style: nodeStyle("#dc2626") },
      { key: "sendToFarmConv", type: "ConverterNode", x: 2510, y: 900, title: "Đem Hạt Ra Nông Trại", activation: "interactive", actionMode: "pull-any", conversion: "single", style: nodeStyle("#16a34a") }
    ];

    const created = {};
    const changes = [];

    for (const def of nodeDefs) {
      const node = RM(def.type);
      node.id = nextId();
      node.value = def.title;
      node.title = def.title;
      if (def.number !== undefined) node.number = def.number;
      if (def.capacity !== undefined) node.capacity = def.capacity;
      if (def.displayCapacity !== undefined) node.displayCapacity = def.displayCapacity;
      if (def.activation !== undefined) node.activation = def.activation;
      if (def.actionMode !== undefined) node.actionMode = def.actionMode;
      if (def.conversion !== undefined) node.conversion = def.conversion;
      if (def.distributionMode !== undefined) node.distributionMode = def.distributionMode;
      if (def.queue !== undefined) node.queue = def.queue;
      if (def.formula !== undefined) node.formula = def.formula;
      if (def.initialValue !== undefined) {
        node.initialValue = def.initialValue;
        node.currentValue = def.initialValue;
      }
      node.autoColor = false;
      if (def.style) node.style = def.style;

      const geo = { x: def.x, y: def.y, width: def.w || 44, height: def.h || 44, newFormat: true };
      node.geometry = geo;
      created[def.key] = node;

      changes.push(BA(node, {
        parent: layer,
        geometry: geo,
        select: false,
        refreshDiagram: true
      }));
    }

    const edgeDefs = [
      // LUỒNG I: NÔNG TRẠI
      { type: "ResourceConnection", from: "seeds", to: "plantFast", formulaType: "number", formulaValue: "1", formula: "1", color: "#16a34a" },
      { type: "ResourceConnection", from: "plantFast", to: "delayFast", formulaType: "number", formulaValue: "1", formula: "1", color: "#16a34a" },
      { type: "ResourceConnection", from: "delayFast", to: "rawCrops", formulaType: "time", formulaValue: "3", formula: "3", label: "x1..3 Crops", color: "#16a34a" },
      { type: "ResourceConnection", from: "seeds", to: "plantSlow", formulaType: "number", formulaValue: "1", formula: "1", color: "#16a34a" },
      { type: "ResourceConnection", from: "plantSlow", to: "delaySlow", formulaType: "number", formulaValue: "1", formula: "1", color: "#16a34a" },
      { type: "ResourceConnection", from: "delaySlow", to: "rawCrops", formulaType: "time", formulaValue: "6", formula: "6", label: "x1..3 Crops", color: "#16a34a" },

      // LUỒNG II: NẤU ĂN
      { type: "ResourceConnection", from: "rawCrops", to: "eatRaw", formulaType: "number", formulaValue: "1", formula: "1", label: "1 Raw", color: "#d97706" },
      { type: "ResourceConnection", from: "eatRaw", to: "hunger", formulaType: "number", formulaValue: "10", formula: "10", label: "+5..20 No", color: "#d97706" },
      { type: "ResourceConnection", from: "rawCrops", to: "cookCombo", formulaType: "number", formulaValue: "2", formula: "2", label: "2-3 Crops", color: "#d97706" },
      { type: "ResourceConnection", from: "cookCombo", to: "cookedMeals", formulaType: "number", formulaValue: "1", formula: "1", color: "#d97706" },
      { type: "ResourceConnection", from: "cookedMeals", to: "eatCombo", formulaType: "number", formulaValue: "1", formula: "1", color: "#d97706" },
      { type: "ResourceConnection", from: "eatCombo", to: "hunger", formulaType: "number", formulaValue: "30", formula: "30", label: "+25..50 No", color: "#d97706" },
      { type: "ResourceConnection", from: "eatCombo", to: "playerHp", formulaType: "number", formulaValue: "14", formula: "14", label: "+12..18 HP", color: "#dc2626" },
      { type: "ResourceConnection", from: "eatCombo", to: "foodBuffs", formulaType: "number", formulaValue: "2", formula: "2", label: "Buff 2 Trận", color: "#d97706" },

      // LUỒNG III: ĐỘ NO 3 NGƯỠNG
      { type: "ResourceConnection", from: "hunger", to: "stageDrain", formulaType: "number", formulaValue: "12", formula: "12", label: "-12/Stage", color: "#d97706" },
      { type: "StateConnection", from: "hunger", to: "tierReg", formulaType: "variable", formulaValue: "h", formula: "h", color: "#2563eb" },
      { type: "StateConnection", from: "hunger", to: "digestRegen", formulaType: "condition", formulaTypeParams: ">=", formulaValue: "80", formula: ">=80", label: "Overstuffed", color: "#16a34a" },
      { type: "ResourceConnection", from: "digestRegen", to: "playerHp", formulaType: "number", formulaValue: "2", formula: "2", label: "+2 HP", color: "#16a34a" },
      { type: "StateConnection", from: "hunger", to: "apPool", formulaType: "condition", formulaTypeParams: ">", formulaValue: "0", formula: ">0", label: "Hunger > 0", color: "#2563eb" },
      { type: "ResourceConnection", from: "playerHp", to: "enemyHitDrain", formulaType: "number", formulaValue: "5", formula: "5", label: "Enemy DMG", color: "#dc2626" },

      // LUỒNG IV: CHU KỲ NGÀY
      { type: "ResourceConnection", from: "advanceDaySrc", to: "dayGate", formulaType: "number", formulaValue: "1", formula: "1", color: "#4f46e5" },
      { type: "ResourceConnection", from: "dayGate", to: "currentDay", formulaType: "weights", formulaValue: "1", formula: "1", label: "+1..5 Ngày", color: "#4f46e5" },
      { type: "ResourceConnection", from: "dayGate", to: "bossCooldown", formulaType: "weights", formulaValue: "1", formula: "1", label: "Tick 3d", color: "#dc2626" },
      { type: "ResourceConnection", from: "dayGate", to: "merchantStay", formulaType: "weights", formulaValue: "1", formula: "1", label: "Tick 5d/3d", color: "#0891b2" },

      // LUỒNG V: TƯỢNG NỮ THẦN (routed cleanly outside headers)
      { type: "ResourceConnection", from: "rawCrops", to: "statueOffer", formulaType: "number", formulaValue: "4", formula: "4", label: "4..35 Crops", color: "#7c3aed", points: [{ x: 557, y: 460 }, { x: 25, y: 460 }, { x: 25, y: 702 }] },
      { type: "ResourceConnection", from: "statueOffer", to: "statueTrackGate", formulaType: "number", formulaValue: "1", formula: "1", color: "#7c3aed" },
      { type: "ResourceConnection", from: "bossDrops", to: "statueBossGate", formulaType: "number", formulaValue: "1", formula: "1", label: "1 BossDrop", color: "#7c3aed" },
      { type: "ResourceConnection", from: "statueBossGate", to: "statueTrackGate", formulaType: "number", formulaValue: "2", formula: "2", label: "Unlock T5/T10", color: "#7c3aed" },
      { type: "ResourceConnection", from: "statueTrackGate", to: "permAtk", formulaType: "weights", formulaValue: "1", formula: "1", label: "+1 ATK", color: "#7c3aed" },
      { type: "ResourceConnection", from: "statueTrackGate", to: "permDef", formulaType: "weights", formulaValue: "1", formula: "1", label: "+1 DEF", color: "#7c3aed" },
      { type: "ResourceConnection", from: "statueTrackGate", to: "permSpd", formulaType: "weights", formulaValue: "1", formula: "1", label: "+1 SPD", color: "#7c3aed" },
      { type: "ResourceConnection", from: "statueTrackGate", to: "permMaxHp", formulaType: "weights", formulaValue: "1", formula: "1", label: "+10 MaxHP", color: "#7c3aed" },

      // LUỒNG VI: THƯƠNG NHÂN, TRẠI & EVENT (routed via vertical corridor x=625..645)
      { type: "ResourceConnection", from: "seeds", to: "tradeSeed3to1", formulaType: "number", formulaValue: "3", formula: "3", label: "3 Hạt", color: "#0891b2", points: [{ x: 97, y: 430 }, { x: 635, y: 430 }, { x: 635, y: 672 }] },
      { type: "ResourceConnection", from: "tradeSeed3to1", to: "seeds", formulaType: "number", formulaValue: "1", formula: "1", label: "1 Hạt Cao", color: "#16a34a", points: [{ x: 650, y: 650 }, { x: 650, y: 400 }, { x: 115, y: 400 }] },
      { type: "ResourceConnection", from: "rawCrops", to: "tradeMeal2to1", formulaType: "number", formulaValue: "2", formula: "2", label: "2 Nông Sản", color: "#0891b2", points: [{ x: 615, y: 217 }, { x: 615, y: 802 }] },
      { type: "ResourceConnection", from: "tradeMeal2to1", to: "cookedMeals", formulaType: "number", formulaValue: "1", formula: "1", label: "1 Súp/Salad", color: "#d97706", points: [{ x: 922, y: 802 }] },
      { type: "ResourceConnection", from: "campActSrc", to: "ambushGate", formulaType: "number", formulaValue: "1", formula: "1", color: "#0891b2" },
      { type: "ResourceConnection", from: "ambushGate", to: "campSafePool", formulaType: "chance", formulaValue: "75", formula: "75%", label: "An Toàn", color: "#16a34a" },
      { type: "ResourceConnection", from: "ambushGate", to: "combatTurn", formulaType: "chance", formulaValue: "25", formula: "25%", label: "Phục Kích!", color: "#dc2626", points: [{ x: 1142, y: 615 }, { x: 1702, y: 615 }] },
      { type: "ResourceConnection", from: "randomEventSrc", to: "eventRollGate", formulaType: "number", formulaValue: "1", formula: "1", color: "#0891b2" },
      { type: "ResourceConnection", from: "eventRollGate", to: "foodBuffs", formulaType: "chance", formulaValue: "50", formula: "50%", label: "Buff/Blessing", color: "#d97706", points: [{ x: 1182, y: 922 }] },
      { type: "ResourceConnection", from: "eventRollGate", to: "campSafePool", formulaType: "chance", formulaValue: "50", formula: "50%", label: "+HP/Hạt", color: "#16a34a" },

      // LUỒNG VII: DUNGEON & CHIẾN ĐẤU
      { type: "ResourceConnection", from: "enterWaveSrc", to: "waveTypeGate", formulaType: "number", formulaValue: "1", formula: "1", color: "#dc2626" },
      { type: "ResourceConnection", from: "waveTypeGate", to: "combatTurn", formulaType: "chance", formulaValue: "65", formula: "65%", label: "Combat/Boss", color: "#dc2626" },
      { type: "ResourceConnection", from: "waveTypeGate", to: "lootGate", formulaType: "chance", formulaValue: "35", formula: "35%", label: "Reward Chest", color: "#16a34a", points: [{ x: 1522, y: 785 }, { x: 1872, y: 785 }] },
      { type: "ResourceConnection", from: "combatTurn", to: "lootGate", formulaType: "number", formulaValue: "1", formula: "1", label: "Thắng Trận", color: "#16a34a" },
      { type: "StateConnection", from: "combatTurn", to: "stageDrain", formulaType: "trigger", formulaValue: "*", formula: "*", color: "#d97706", points: [{ x: 1935, y: 712 }, { x: 1935, y: 100 }, { x: 1582, y: 100 }] },
      { type: "StateConnection", from: "combatTurn", to: "enemyHitDrain", formulaType: "trigger", formulaValue: "*", formula: "*", color: "#dc2626", points: [{ x: 1702, y: 485 }, { x: 1382, y: 485 }] },
      { type: "StateConnection", from: "permAtk", to: "dmgReg", formulaType: "variable", formulaValue: "a", formula: "a", color: "#7c3aed", points: [{ x: 532, y: 990 }, { x: 1522, y: 990 }] },
      { type: "StateConnection", from: "foodBuffs", to: "dmgReg", formulaType: "variable", formulaValue: "b", formula: "b", color: "#d97706", points: [{ x: 1245, y: 412 }, { x: 1245, y: 882 }] },
      { type: "StateConnection", from: "tierReg", to: "dmgReg", formulaType: "variable", formulaValue: "c", formula: "c", color: "#2563eb", points: [{ x: 1915, y: 157 }, { x: 1915, y: 465 }, { x: 1265, y: 465 }, { x: 1265, y: 865 }] },
      { key: "dealDmgEdge", type: "ResourceConnection", from: "enemyHp", to: "dealDmgDrain", formulaType: "number", formulaValue: "9", formula: "9", label: "Deal DMG", color: "#dc2626" },
      { type: "StateConnection", from: "combatTurn", to: "dealDmgDrain", formulaType: "trigger", formulaValue: "*", formula: "*", color: "#dc2626" },
      { type: "ResourceConnection", from: "lootGate", to: "backpack", formulaType: "chance", formulaValue: "75", formula: "75%", label: "Seeds/Crops", color: "#16a34a" },
      { type: "ResourceConnection", from: "lootGate", to: "bossDrops", formulaType: "chance", formulaValue: "25", formula: "25%", label: "Boss Drop", color: "#7c3aed", points: [{ x: 1872, y: 1025 }, { x: 122, y: 1025 }] },

      // LUỒNG VIII: KHO ĐỒ BACKPACK VS HOMECHEST
      { type: "ResourceConnection", from: "backpack", to: "extractConv", formulaType: "number", formulaValue: "1", formula: "1", color: "#0f766e" },
      { type: "ResourceConnection", from: "extractConv", to: "runOutcomeGate", formulaType: "number", formulaValue: "1", formula: "1", color: "#0f766e" },
      { type: "ResourceConnection", from: "runOutcomeGate", to: "homeChest", formulaType: "chance", formulaValue: "80", formula: "80%", label: "Về Nhà / KeepLoot", color: "#16a34a" },
      { type: "ResourceConnection", from: "runOutcomeGate", to: "clearLootDrain", formulaType: "chance", formulaValue: "20", formula: "20%", label: "Chết Mất Đồ", color: "#dc2626" },
      { type: "ResourceConnection", from: "homeChest", to: "sendToFarmConv", formulaType: "number", formulaValue: "1", formula: "1", color: "#16a34a" },
      { type: "ResourceConnection", from: "sendToFarmConv", to: "seeds", formulaType: "number", formulaValue: "2", formula: "2", label: "Cấp Hạt Trồng", color: "#16a34a", points: [{ x: 2532, y: 1055 }, { x: 12, y: 1055 }, { x: 12, y: 217 }] }
    ];

    for (const ed of edgeDefs) {
      const edge = RM(ed.type);
      edge.id = nextId();
      edge.formulaType = ed.formulaType || null;
      edge.formulaTypeParams = ed.formulaTypeParams || null;
      edge.formulaValue = ed.formulaValue || null;
      edge.formula = ed.formula || null;
      if (ed.label) {
        edge.value = ed.label;
        edge.label = ed.label;
      }
      edge.autoColor = false;
      edge.style = nodeStyle(ed.color || "#334155");
      const geo = ed.points ? { newFormat: true, points: ed.points } : { newFormat: true };
      edge.geometry = geo;
      if (ed.key) created[ed.key] = edge;

      changes.push(BA(edge, {
        parent: layer,
        source: created[ed.from],
        target: created[ed.to],
        geometry: geo,
        select: false,
        refreshDiagram: true
      }));
    }

    // Connect dmgReg -> dealDmgEdge with StateConnection (=)
    const regOutEdge = RM("StateConnection");
    regOutEdge.id = nextId();
    regOutEdge.formulaType = "modify-value";
    regOutEdge.formulaTypeParams = "overwrite";
    regOutEdge.formulaValue = "=";
    regOutEdge.formula = "=";
    regOutEdge.autoColor = false;
    regOutEdge.style = nodeStyle("#dc2626");
    regOutEdge.geometry = { newFormat: true };
    changes.push(BA(regOutEdge, {
      parent: layer,
      source: created.dmgReg,
      target: created.dealDmgEdge,
      geometry: { newFormat: true },
      select: false,
      refreshDiagram: true
    }));

    G.addChanges(changes, { history: true, record: true, execute: true });
    if (typeof G.sendChanges === 'function') {
      try { G.sendChanges(); } catch (e) {}
    }

    await new Promise(r => setTimeout(r, 600));
    const appVue = document.querySelector('.m-app__diagram-container')?.__vue__ || document.querySelector('.m-editorui')?.__vue__;
    if (appVue && typeof appVue.fitWindow === 'function') {
      appVue.fitWindow();
    }

    await new Promise(r => setTimeout(r, 800));
    return JSON.stringify({
      status: "ok",
      totalNodes: T.getNodeIds().length,
      unsavedChanges: G.hasUnsavedChanges()
    });
  } catch (err) {
    return JSON.stringify({ status: "error", message: err.message, stack: err.stack });
  }
})()