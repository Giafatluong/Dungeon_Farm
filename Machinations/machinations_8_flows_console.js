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
      ['__builder_core_' + Date.now()],
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
      try { await G.reserveNewIdRange(60); } catch (e) {}
    }
    let fallbackId = Math.max(T.getMaxNodeId() || 300, 600) + 1;
    function nextId() {
      try {
        const id = G.reserveId();
        if (id && id > 0) return id;
      } catch (e) {}
      return fallbackId++;
    }

    function headerStyle(stroke, fill, font) {
      return { fontFamily: "Roboto", fontSize: 13, fontColor: font, strokeColor: stroke, strokeWidth: 2, fillColor: fill };
    }
    function nodeStyle(stroke, fill = "#ffffff", font = "#111827") {
      return { fontFamily: "Roboto", fontSize: 11, fontColor: font, strokeColor: stroke, strokeWidth: 2, fillColor: fill, labelPosition: "bottom", captionWidth: 135 };
    }

    // 4 CORE INTERACTIVE FLOWS (Đánh chết quái HP <= 0 mới rơi đồ & gọi quái ải tiếp theo)
    const nodeDefs = [
      // ==================== TIÊU ĐỀ 4 LUỒNG CƠ BẢN ====================
      { key: "h1", type: "TextNode", x: 150, y: 30,  w: 575, h: 42, title: "LUỒNG 1: NÔNG TRẠI & NẤU ĂN (Bấm Gieo Trồng -> Nấu Món Ăn)", style: headerStyle("#16a34a", "#dcfce7", "#14532d") },
      { key: "h2", type: "TextNode", x: 825, y: 30,  w: 525, h: 42, title: "LUỒNG 2: SINH TỒN & CHỈ SỐ (Bấm Ăn Hồi Sức & Hiến Tế Tượng)", style: headerStyle("#2563eb", "#dbeafe", "#1e3a8a") },
      { key: "h3", type: "TextNode", x: 825, y: 400, w: 525, h: 42, title: "LUỒNG 3: CHIẾN ĐẤU DUNGEON (Đánh Quái Về 0 HP Mới Rơi Đồ)", style: headerStyle("#dc2626", "#fee2e2", "#7f1d1d") },
      { key: "h4", type: "TextNode", x: 150, y: 400, w: 575, h: 42, title: "LUỒNG 4: TÚI ĐỒ & VỀ NHÀ (Bấm Về Nhà Lưu 100% Đồ vs. HP=0 Mất Sạch)", style: headerStyle("#0f766e", "#ccfbf1", "#134e4a") },

      // ==================== LUỒNG 1: NÔNG TRẠI & NẤU ĂN (INTERACTIVE 👆) ====================
      { key: "seeds",      type: "PoolNode",      x: 70,  y: 165, title: "Hạt Giống (Seeds)",     number: 10, capacity: 30, displayCapacity: 0, activation: "passive",     actionMode: "pull-any", style: nodeStyle("#16a34a") },
      { key: "plantConv",  type: "ConverterNode", x: 225, y: 165, title: "Gieo Trồng (Bấm)",      activation: "interactive", actionMode: "pull-any", conversion: "single", style: nodeStyle("#16a34a") },
      { key: "crops",      type: "PoolNode",      x: 380, y: 165, title: "Nông Sản (Crops)",      number: 8,  capacity: 40, displayCapacity: 0, activation: "passive",     actionMode: "pull-any", style: nodeStyle("#16a34a") },
      { key: "cookConv",   type: "ConverterNode", x: 535, y: 165, title: "Nấu Món Ăn (Bấm)",      activation: "interactive", actionMode: "pull-any", conversion: "single", style: nodeStyle("#d97706") },
      { key: "meals",      type: "PoolNode",      x: 680, y: 165, title: "Món Ăn (Meals)",        number: 4,  capacity: 20, displayCapacity: 0, activation: "passive",     actionMode: "pull-any", style: nodeStyle("#d97706") },
      { key: "statueConv", type: "ConverterNode", x: 535, y: 295, title: "Hiến Tế Tượng (Bấm)",   activation: "interactive", actionMode: "pull-any", conversion: "single", style: nodeStyle("#7c3aed") },

      // ==================== LUỒNG 2: SINH TỒN & CHỈ SỐ (INTERACTIVE 👆) ====================
      { key: "eatConv",       type: "ConverterNode", x: 840,  y: 165, title: "Ăn Hồi Sức (Bấm)",    activation: "interactive", actionMode: "pull-any", conversion: "single", style: nodeStyle("#d97706") },
      { key: "hunger",        type: "PoolNode",      x: 1025, y: 115, title: "Độ No (Max 100)",     number: 80, capacity: 100, displayCapacity: 0, activation: "passive",    actionMode: "pull-any", style: nodeStyle("#d97706") },
      { key: "hungerDrain",   type: "DrainNode",     x: 1220, y: 115, title: "Đói Mỗi Lượt (-10)",  activation: "passive",     actionMode: "pull-any", style: nodeStyle("#d97706") },
      { key: "playerHp",      type: "PoolNode",      x: 1025, y: 235, title: "Máu Player (HP)",     number: 60, capacity: 100, displayCapacity: 0, activation: "passive",    actionMode: "pull-any", style: nodeStyle("#dc2626") },
      { key: "enemyHitDrain", type: "DrainNode",     x: 1220, y: 235, title: "Quái Đánh (-6 HP)",   activation: "passive",     actionMode: "pull-any", style: nodeStyle("#dc2626") },
      { key: "playerAtk",     type: "PoolNode",      x: 1025, y: 330, title: "Sức Mạnh (ATK)",      number: 10, capacity: 50,  displayCapacity: 0, activation: "passive",    actionMode: "pull-any", style: nodeStyle("#7c3aed") },

      // ==================== LUỒNG 3: CHIẾN ĐẤU DUNGEON (ĐÁNH CHẾT QUÁI HP<=0 MỚI RƠI ĐỒ) ====================
      { key: "dealDmgDrain",  type: "DrainNode",     x: 960,  y: 490, title: "Đánh Quái (Bấm)",            activation: "interactive", actionMode: "pull-any", style: nodeStyle("#dc2626") },
      { key: "enemyHp",       type: "PoolNode",      x: 1220, y: 490, title: "Máu Quái (30 HP)",           number: 30, capacity: 100, displayCapacity: 0, activation: "passive",   actionMode: "pull-any", style: nodeStyle("#dc2626") },
      { key: "killRewardSrc", type: "SourceNode",    x: 960,  y: 635, title: "Hạ Quái (HP=0 -> Rơi Đồ)",   activation: "automatic",   actionMode: "push-any", style: nodeStyle("#16a34a") },

      // ==================== LUỒNG 4: TÚI ĐỒ & VỀ NHÀ LƯU 100% ĐỒ (INTERACTIVE 👆) ====================
      { key: "backpack",    type: "PoolNode",      x: 640, y: 540, title: "Túi Đồ (Backpack)",           number: 2,  capacity: 33, displayCapacity: 0, activation: "passive",     actionMode: "pull-any", style: nodeStyle("#0f766e") },
      { key: "extractGate", type: "GateNode",      x: 410, y: 490, title: "Về Nhà Cất Đồ (Bấm Lưu All)", activation: "interactive", actionMode: "pull-any", distributionMode: "deterministic", style: nodeStyle("#16a34a") },
      { key: "homeChest",   type: "PoolNode",      x: 185, y: 490, title: "Rương Nhà (An Toàn 100%)",    number: 6,  capacity: 33, displayCapacity: 0, activation: "passive",     actionMode: "pull-any", style: nodeStyle("#16a34a") },
      { key: "deathDrain",  type: "DrainNode",     x: 410, y: 635, title: "Tử Trận Mất Sạch (Khi HP=0)", activation: "automatic",   actionMode: "pull-any", style: nodeStyle("#dc2626") },
      { key: "toSeedConv",  type: "ConverterNode", x: 70,  y: 330, title: "Đem Hạt Ra Trồng (Bấm)",      activation: "interactive", actionMode: "pull-any", conversion: "single", style: nodeStyle("#16a34a") }
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
      // LUỒNG 1: NÔNG TRẠI & NẤU ĂN
      { type: "ResourceConnection", from: "seeds",      to: "plantConv",  raw: "1", label: "1 Hạt",      color: "#16a34a" },
      { type: "ResourceConnection", from: "plantConv",  to: "crops",      raw: "2", label: "2 Nông Sản", color: "#16a34a" },
      { type: "ResourceConnection", from: "crops",      to: "cookConv",   raw: "2", label: "2 Nông Sản", color: "#d97706" },
      { type: "ResourceConnection", from: "cookConv",   to: "meals",      raw: "1", label: "1 Món Ăn",   color: "#d97706" },
      { type: "ResourceConnection", from: "crops",      to: "statueConv", raw: "2", label: "2 Nông Sản", color: "#7c3aed" },
      { type: "ResourceConnection", from: "statueConv", to: "playerAtk",  raw: "1", label: "+1 ATK",     color: "#7c3aed", points: [{ x: 557, y: 352 }, { x: 1025, y: 352 }] },

      // LUỒNG 2: SINH TỒN & CHỈ SỐ
      { type: "ResourceConnection", from: "meals",    to: "eatConv",       raw: "1",  label: "1 Món",   color: "#d97706" },
      { type: "ResourceConnection", from: "eatConv",  to: "hunger",        raw: "15", label: "+15 No",  color: "#d97706" },
      { type: "ResourceConnection", from: "eatConv",  to: "playerHp",      raw: "10", label: "+10 HP",  color: "#dc2626" },
      { type: "ResourceConnection", from: "hunger",   to: "hungerDrain",   raw: "10", label: "-10 No",  color: "#d97706" },
      { type: "ResourceConnection", from: "playerHp", to: "enemyHitDrain", raw: "6",  label: "-6 HP",   color: "#dc2626" },
      { type: "StateConnection",    from: "hunger",   to: "dealDmgDrain",  raw: ">0", label: "Còn No (>0)", color: "#2563eb", points: [{ x: 775, y: 137 }, { x: 775, y: 512 }] },

      // LUỒNG 3: CHIẾN ĐẤU DUNGEON (Đánh chết quái HP<=0 mới rơi đồ & qua quái mới)
      { key: "dmgEdge", type: "ResourceConnection", from: "enemyHp", to: "dealDmgDrain", raw: "5", label: "Sát Thương", color: "#dc2626" },
      { type: "StateConnection",    from: "dealDmgDrain",  to: "hungerDrain",   raw: "*", triggerOnMode: "any", color: "#d97706", points: [{ x: 982, y: 455 }, { x: 1425, y: 455 }, { x: 1425, y: 137 }] },
      { type: "StateConnection",    from: "dealDmgDrain",  to: "enemyHitDrain", raw: "*", triggerOnMode: "any", color: "#dc2626", points: [{ x: 982, y: 470 }, { x: 1400, y: 470 }, { x: 1400, y: 257 }] },
      { type: "StateConnection",    from: "enemyHp",       to: "killRewardSrc", raw: "<=0", label: "Quái Chết (HP<=0)", color: "#16a34a", points: [{ x: 1242, y: 590 }, { x: 982, y: 590 }] },
      { type: "ResourceConnection", from: "killRewardSrc", to: "backpack",      raw: "2",   label: "+2 Rơi Đồ (Loot)",  color: "#16a34a", points: [{ x: 662, y: 657 }] },
      { type: "ResourceConnection", from: "killRewardSrc", to: "enemyHp",       raw: "30",  label: "Quái Ải Mới (+30 HP)", color: "#dc2626", points: [{ x: 1242, y: 657 }] },

      // LUỒNG 4: TÚI ĐỒ & VỀ NHÀ LƯU TOÀN BỘ ĐỒ (ALL)
      { type: "ResourceConnection", from: "backpack",    to: "extractGate", raw: "all",  label: "Lưu Toàn Bộ (all)", color: "#16a34a" },
      { type: "ResourceConnection", from: "extractGate", to: "homeChest",   raw: "100%", label: "100% An Toàn",      color: "#16a34a" },
      { type: "ResourceConnection", from: "backpack",    to: "deathDrain",  raw: "all",  label: "Mất Hết (all)",     color: "#dc2626" },
      { type: "StateConnection",    from: "playerHp",    to: "extractGate", raw: ">0",   label: "Còn Sống (HP>0)",   color: "#16a34a", points: [{ x: 750, y: 257 }, { x: 750, y: 465 }, { x: 432, y: 465 }] },
      { type: "StateConnection",    from: "playerHp",    to: "deathDrain",  raw: "<=0",  label: "Khi Chết (HP<=0)",  color: "#dc2626", points: [{ x: 795, y: 257 }, { x: 795, y: 700 }, { x: 432, y: 700 }] },
      { type: "ResourceConnection", from: "homeChest",   to: "toSeedConv",  raw: "1",    color: "#16a34a", points: [{ x: 92, y: 512 }] },
      { type: "ResourceConnection", from: "toSeedConv",  to: "seeds",       raw: "2",    label: "+2 Hạt Mới", color: "#16a34a" }
    ];

    for (const ed of edgeDefs) {
      const edge = RM(ed.type);
      edge.id = nextId();
      edge.source = created[ed.from];
      edge.target = created[ed.to];
      if (ed.triggerOnMode) edge.triggerOnMode = ed.triggerOnMode;
      edge.migrateToFormulaType(ed.raw);
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

    // StateConnection from playerAtk -> dmgEdge (+1 per ATK point)
    const atkModEdge = RM("StateConnection");
    atkModEdge.id = nextId();
    atkModEdge.source = created.playerAtk;
    atkModEdge.target = created.dmgEdge;
    atkModEdge.migrateToFormulaType("+1");
    atkModEdge.value = "+ATK";
    atkModEdge.label = "+ATK";
    atkModEdge.autoColor = false;
    atkModEdge.style = nodeStyle("#7c3aed");
    const atkGeo = { newFormat: true, points: [{ x: 1105, y: 352 }, { x: 1105, y: 512 }] };
    atkModEdge.geometry = atkGeo;
    changes.push(BA(atkModEdge, {
      parent: layer,
      source: created.playerAtk,
      target: created.dmgEdge,
      geometry: atkGeo,
      select: false,
      refreshDiagram: true
    }));

    G.addChanges(changes, { history: true, record: true, execute: true });
    if (editorVue?.$store?.state?.diagramPropertiesStore?.playCtrl) {
      editorVue.$store.state.diagramPropertiesStore.playCtrl.stepsToPlay = 10;
      editorVue.$store.state.diagramPropertiesStore.playCtrl.runsToPlay = 1;
    }
    if (typeof G.sendChanges === 'function') {
      try { G.sendChanges(); } catch (e) {}
    }
    if (editorVue && typeof editorVue.saveDiagram === 'function') {
      try { editorVue.saveDiagram(); } catch (e) {}
    }
    if (editorVue && typeof editorVue.triggerAction === 'function') {
      try { editorVue.triggerAction({ name: "save" }); } catch (e) {}
    }

    await new Promise(r => setTimeout(r, 600));
    const appVue = document.querySelector('.m-app__diagram-container')?.__vue__ || document.querySelector('.m-editorui')?.__vue__;
    if (appVue && typeof appVue.fitWindow === 'function') {
      appVue.fitWindow();
    }

    await new Promise(r => setTimeout(r, 600));
    return JSON.stringify({
      status: "ok",
      totalNodes: T.getNodeIds().length,
      unsavedChanges: G.hasUnsavedChanges()
    });
  } catch (err) {
    return JSON.stringify({ status: "error", message: err.message, stack: err.stack });
  }
})()
