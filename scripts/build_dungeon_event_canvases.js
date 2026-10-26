const fs = require('fs');
const path = require('path');

// Component GUIDs
const GUID_TMP = 'f4688fdb7df04437aeb418b961361dc5';
const GUID_IMAGE = 'fe87c0e1cc204ed48ad3b37840f39efc';
const GUID_BUTTON = '4e29b1a8efbd4b44bb3f3716e73f07ff';
const GUID_SCROLLRECT = '1aa08ab6e0800fa44ae55d278d1423e3';
const GUID_SCROLLBAR = '2a4db7a114972834c8e4117be1d82ba3';
const GUID_CANVAS_SCALER = '0cd44c1031e13a943bb63640046fad76';
const GUID_GRAPHIC_RAYCASTER = 'dc42784cf147c0c48a680349fa168899';
const GUID_VLG = '59f8146938fff824cb5fd77236b75775';
const GUID_GLG = '8a8695521f0d02e499659fee002a26c2';
const GUID_CSF = '3245ec927659c4140ac4f8d17403cc18';
const GUID_RECTMASK2D = '3312d7739989d2b4e91e6319e9a96d76';
const GUID_EVENT_UI = 'f2612bc2ec574f6eae81e3fd3f7071a6';
const GUID_MERCHANT_UI = 'e819b523c048455490184cba3019aa55';

// Asset GUIDs
const GUID_FONT = '8f586378b4e144a9851e7b34d9b748ee';
const GUID_FRAME = 'aa6f9a2ecff34c14d84e5bd91727c940';
const SPRITE_FRAME = '-5668518402485374812';
const GUID_SLOT_PREFAB = '02cead57d3ffaca44b4e995473eaea19';
const FILEID_SLOT_PREFAB = '3631650896398165525';

let nextId = 940000000;
function genId() {
  return nextId++;
}

class Node {
  constructor(name, isRoot = false) {
    this.name = name;
    this.isRoot = isRoot;
    this.goId = genId();
    this.rtId = genId();
    this.children = [];
    this.father = null;
    this.isActive = 1;
    this.layer = 5; // UI layer
    
    this.anchorMin = { x: 0.5, y: 0.5 };
    this.anchorMax = { x: 0.5, y: 0.5 };
    this.pivot = { x: 0.5, y: 0.5 };
    this.anchoredPosition = { x: 0, y: 0 };
    this.sizeDelta = { x: 100, y: 100 };
    this.offsetMin = null;
    this.offsetMax = null;

    // Component IDs
    this.crId = null;
    this.imgId = null;
    this.tmpId = null;
    this.btnId = null;
    this.scrollId = null;
    this.sbId = null;
    this.maskId = null;
    this.vlgId = null;
    this.glgId = null;
    this.csfId = null;
  }

  addChild(child) {
    child.father = this;
    this.children.push(child);
    return child;
  }
}

const allNodes = [];
function reg(n) {
  allNodes.push(n);
  return n;
}

// ==========================================
// 1. BUILD DUNGEON EVENT CANVAS
// ==========================================
const eventCanvas = reg(new Node('DungeonEventCanvas', true));
eventCanvas.canvasId = genId();
eventCanvas.scalerId = genId();
eventCanvas.raycasterId = genId();
eventCanvas.uiId = genId();
eventCanvas.sortingOrder = 115;

const eventOverlay = reg(new Node('DungeonEventPanel'));
eventOverlay.anchorMin = { x: 0, y: 0 };
eventOverlay.anchorMax = { x: 1, y: 1 };
eventOverlay.sizeDelta = { x: 0, y: 0 };
eventOverlay.isActive = 0;
eventOverlay.crId = genId();
eventOverlay.imgId = genId();
eventOverlay.color = { r: 0.04, g: 0.05, b: 0.08, a: 0.88 };
eventCanvas.addChild(eventOverlay);

const eventModal = reg(new Node('EventModal'));
eventModal.sizeDelta = { x: 760, y: 520 };
eventModal.crId = genId();
eventModal.imgId = genId();
eventModal.frameColor = { r: 0.12, g: 0.13, b: 0.18, a: 0.98 };
eventOverlay.addChild(eventModal);

const eventHeader = reg(new Node('Header'));
eventHeader.anchorMin = { x: 0, y: 1 };
eventHeader.anchorMax = { x: 1, y: 1 };
eventHeader.pivot = { x: 0.5, y: 1 };
eventHeader.sizeDelta = { x: 0, y: 68 };
eventHeader.crId = genId();
eventHeader.imgId = genId();
eventHeader.frameColor = { r: 0.20, g: 0.15, b: 0.28, a: 1 };
eventModal.addChild(eventHeader);

const categoryText = reg(new Node('CategoryText'));
categoryText.anchorMin = { x: 0, y: 1 };
categoryText.anchorMax = { x: 1, y: 1 };
categoryText.offsetMin = { x: 24, y: -26 };
categoryText.offsetMax = { x: -24, y: -6 };
categoryText.crId = genId();
categoryText.tmpId = genId();
categoryText.text = '[ EVENT ]';
categoryText.fontSize = 12;
categoryText.fontStyle = 1;
categoryText.align = 258; // MidlineLeft
categoryText.color = { r: 1, g: 1, b: 1, a: 1 };
eventHeader.addChild(categoryText);

const titleText = reg(new Node('TitleText'));
titleText.anchorMin = { x: 0, y: 0 };
titleText.anchorMax = { x: 1, y: 1 };
titleText.offsetMin = { x: 24, y: 6 };
titleText.offsetMax = { x: -24, y: -28 };
titleText.crId = genId();
titleText.tmpId = genId();
titleText.text = 'EVENT TITLE';
titleText.fontSize = 19;
titleText.fontStyle = 1;
titleText.align = 258;
titleText.color = { r: 1, g: 0.88, b: 0.25, a: 1 };
eventHeader.addChild(titleText);

const narrativeBox = reg(new Node('NarrativeBox'));
narrativeBox.anchorMin = { x: 0, y: 0.52 };
narrativeBox.anchorMax = { x: 1, y: 1 };
narrativeBox.offsetMin = { x: 24, y: 0 };
narrativeBox.offsetMax = { x: -24, y: -76 };
narrativeBox.crId = genId();
narrativeBox.imgId = genId();
narrativeBox.frameColor = { r: 0.07, g: 0.08, b: 0.12, a: 0.95 };
eventModal.addChild(narrativeBox);

const narrativeText = reg(new Node('NarrativeText'));
narrativeText.anchorMin = { x: 0, y: 0 };
narrativeText.anchorMax = { x: 1, y: 1 };
narrativeText.offsetMin = { x: 16, y: 12 };
narrativeText.offsetMax = { x: -16, y: -12 };
narrativeText.crId = genId();
narrativeText.tmpId = genId();
narrativeText.text = 'Event narrative text...';
narrativeText.fontSize = 13.5;
narrativeText.align = 257; // TopLeft
narrativeText.color = { r: 0.92, g: 0.94, b: 0.98, a: 1 };
narrativeBox.addChild(narrativeText);

const choicesContainer = reg(new Node('ChoicesContainer'));
choicesContainer.anchorMin = { x: 0, y: 0 };
choicesContainer.anchorMax = { x: 1, y: 0.50 };
choicesContainer.offsetMin = { x: 24, y: 56 };
choicesContainer.offsetMax = { x: -24, y: 0 };
choicesContainer.vlgId = genId();
choicesContainer.vlgSpacing = 8;
eventModal.addChild(choicesContainer);

const statusFeedbackText = reg(new Node('StatusFeedbackText'));
statusFeedbackText.anchorMin = { x: 0, y: 0 };
statusFeedbackText.anchorMax = { x: 0.68, y: 0 };
statusFeedbackText.offsetMin = { x: 24, y: 12 };
statusFeedbackText.offsetMax = { x: 0, y: 48 };
statusFeedbackText.crId = genId();
statusFeedbackText.tmpId = genId();
statusFeedbackText.text = '';
statusFeedbackText.fontSize = 12.5;
statusFeedbackText.align = 258;
statusFeedbackText.color = { r: 1, g: 1, b: 1, a: 1 };
eventModal.addChild(statusFeedbackText);

const continueButton = reg(new Node('ContinueButton'));
continueButton.anchorMin = { x: 1, y: 0 };
continueButton.anchorMax = { x: 1, y: 0 };
continueButton.pivot = { x: 1, y: 0 };
continueButton.anchoredPosition = { x: -24, y: 12 };
continueButton.sizeDelta = { x: 160, y: 38 };
continueButton.isActive = 0;
continueButton.crId = genId();
continueButton.imgId = genId();
continueButton.btnId = genId();
continueButton.btnColor = { r: 0.18, g: 0.55, b: 0.28, a: 1 };
continueButton.targetMethod = 'OnContinueClicked';
continueButton.targetUI = eventCanvas.uiId;
eventModal.addChild(continueButton);

const continueBtnText = reg(new Node('Text'));
continueBtnText.anchorMin = { x: 0, y: 0 };
continueBtnText.anchorMax = { x: 1, y: 1 };
continueBtnText.sizeDelta = { x: 0, y: 0 };
continueBtnText.crId = genId();
continueBtnText.tmpId = genId();
continueBtnText.text = 'CONTINUE [->]';
continueBtnText.fontSize = 13;
continueBtnText.fontStyle = 1;
continueBtnText.align = 514;
continueBtnText.color = { r: 1, g: 1, b: 1, a: 1 };
continueButton.addChild(continueBtnText);


// ==========================================
// 2. BUILD MERCHANT CANVAS
// ==========================================
const merchantCanvas = reg(new Node('MerchantCanvas', true));
merchantCanvas.canvasId = genId();
merchantCanvas.scalerId = genId();
merchantCanvas.raycasterId = genId();
merchantCanvas.uiId = genId();
merchantCanvas.sortingOrder = 130;

const merchantOverlay = reg(new Node('MerchantPanel'));
merchantOverlay.anchorMin = { x: 0, y: 0 };
merchantOverlay.anchorMax = { x: 1, y: 1 };
merchantOverlay.sizeDelta = { x: 0, y: 0 };
merchantOverlay.isActive = 0;
merchantOverlay.crId = genId();
merchantOverlay.imgId = genId();
merchantOverlay.color = { r: 0.04, g: 0.05, b: 0.08, a: 0.88 };
merchantCanvas.addChild(merchantOverlay);

const merchantWindow = reg(new Node('Window'));
merchantWindow.sizeDelta = { x: 980, y: 600 };
merchantWindow.crId = genId();
merchantWindow.imgId = genId();
merchantWindow.frameColor = { r: 0.10, g: 0.12, b: 0.17, a: 0.98 };
merchantOverlay.addChild(merchantWindow);

// Header elements
const merchantTitle = reg(new Node('Title'));
merchantTitle.anchorMin = { x: 0, y: 1 };
merchantTitle.anchorMax = { x: 1, y: 1 };
merchantTitle.pivot = { x: 0.5, y: 1 };
merchantTitle.anchoredPosition = { x: 0, y: -14 };
merchantTitle.sizeDelta = { x: -60, y: 32 };
merchantTitle.crId = genId();
merchantTitle.tmpId = genId();
merchantTitle.text = 'WANDERING MERCHANT - DUNGEON OUTPOST';
merchantTitle.fontSize = 21;
merchantTitle.fontStyle = 1;
merchantTitle.align = 514;
merchantTitle.color = { r: 1, g: 0.85, b: 0.35, a: 1 };
merchantWindow.addChild(merchantTitle);

const merchantSubtitle = reg(new Node('Subtitle'));
merchantSubtitle.anchorMin = { x: 0, y: 1 };
merchantSubtitle.anchorMax = { x: 1, y: 1 };
merchantSubtitle.pivot = { x: 0.5, y: 1 };
merchantSubtitle.anchoredPosition = { x: 0, y: -44 };
merchantSubtitle.sizeDelta = { x: -60, y: 22 };
merchantSubtitle.crId = genId();
merchantSubtitle.tmpId = genId();
merchantSubtitle.text = 'A mysterious trader braving the dungeon. Barter seeds, recipes, and survival rations.';
merchantSubtitle.fontSize = 12.5;
merchantSubtitle.align = 514;
merchantSubtitle.color = { r: 0.82, g: 0.86, b: 0.94, a: 1 };
merchantWindow.addChild(merchantSubtitle);

const merchantFeedback = reg(new Node('Feedback'));
merchantFeedback.anchorMin = { x: 0, y: 1 };
merchantFeedback.anchorMax = { x: 1, y: 1 };
merchantFeedback.pivot = { x: 0.5, y: 1 };
merchantFeedback.anchoredPosition = { x: 0, y: -68 };
merchantFeedback.sizeDelta = { x: -60, y: 24 };
merchantFeedback.crId = genId();
merchantFeedback.tmpId = genId();
merchantFeedback.text = 'Welcome traveler! What would you like to barter today?';
merchantFeedback.fontSize = 13;
merchantFeedback.fontStyle = 1;
merchantFeedback.align = 514;
merchantFeedback.color = { r: 0.88, g: 0.94, b: 1, a: 1 };
merchantWindow.addChild(merchantFeedback);

// Close button (X)
const merchantCloseBtn = reg(new Node('BtnClose'));
merchantCloseBtn.anchorMin = { x: 1, y: 1 };
merchantCloseBtn.anchorMax = { x: 1, y: 1 };
merchantCloseBtn.pivot = { x: 1, y: 1 };
merchantCloseBtn.anchoredPosition = { x: -14, y: -14 };
merchantCloseBtn.sizeDelta = { x: 32, y: 32 };
merchantCloseBtn.crId = genId();
merchantCloseBtn.imgId = genId();
merchantCloseBtn.btnId = genId();
merchantCloseBtn.btnColor = { r: 0.6, g: 0.18, b: 0.18, a: 1 };
merchantCloseBtn.targetMethod = 'Close';
merchantCloseBtn.targetUI = merchantCanvas.uiId;
merchantWindow.addChild(merchantCloseBtn);

const xText = reg(new Node('X'));
xText.anchorMin = { x: 0, y: 0 };
xText.anchorMax = { x: 1, y: 1 };
xText.sizeDelta = { x: 0, y: 0 };
xText.crId = genId();
xText.tmpId = genId();
xText.text = 'X';
xText.fontSize = 16;
xText.fontStyle = 1;
xText.align = 514;
xText.color = { r: 1, g: 1, b: 1, a: 1 };
merchantCloseBtn.addChild(xText);

// Left Column: Wares
const waresCol = reg(new Node('WaresColumn'));
waresCol.anchorMin = { x: 0, y: 0 };
waresCol.anchorMax = { x: 0, y: 1 };
waresCol.pivot = { x: 0, y: 0.5 };
waresCol.anchoredPosition = { x: 20, y: -18 };
waresCol.sizeDelta = { x: 530, y: -140 };
waresCol.crId = genId();
waresCol.imgId = genId();
waresCol.frameColor = { r: 0.07, g: 0.08, b: 0.12, a: 0.95 };
waresCol.scrollId = genId();
merchantWindow.addChild(waresCol);

const waresVp = reg(new Node('Viewport'));
waresVp.anchorMin = { x: 0, y: 0 };
waresVp.anchorMax = { x: 1, y: 1 };
waresVp.offsetMin = { x: 6, y: 6 };
waresVp.offsetMax = { x: -22, y: -6 };
waresVp.crId = genId();
waresVp.maskId = genId();
waresCol.addChild(waresVp);

const waresContent = reg(new Node('Content'));
waresContent.anchorMin = { x: 0, y: 1 };
waresContent.anchorMax = { x: 1, y: 1 };
waresContent.pivot = { x: 0.5, y: 1 };
waresContent.anchoredPosition = { x: 0, y: 0 };
waresContent.sizeDelta = { x: 0, y: 0 };
waresContent.vlgId = genId();
waresContent.csfId = genId();
waresContent.vlgSpacing = 10;
waresVp.addChild(waresContent);

const waresSb = reg(new Node('VerticalScrollbar'));
waresSb.anchorMin = { x: 1, y: 0 };
waresSb.anchorMax = { x: 1, y: 1 };
waresSb.pivot = { x: 1, y: 0.5 };
waresSb.anchoredPosition = { x: -6, y: 0 };
waresSb.sizeDelta = { x: 12, y: -12 };
waresSb.crId = genId();
waresSb.imgId = genId();
waresSb.sbId = genId();
waresCol.addChild(waresSb);

const waresSa = reg(new Node('SlidingArea'));
waresSa.anchorMin = { x: 0, y: 0 };
waresSa.anchorMax = { x: 1, y: 1 };
waresSa.sizeDelta = { x: 0, y: 0 };
waresSb.addChild(waresSa);

const waresHandle = reg(new Node('Handle'));
waresHandle.sizeDelta = { x: 0, y: 0 };
waresHandle.crId = genId();
waresHandle.imgId = genId();
waresHandle.frameColor = { r: 0.85, g: 0.65, b: 0.2, a: 1 };
waresSa.addChild(waresHandle);

// Right Column: Backpack
const bpCol = reg(new Node('BackpackColumn'));
bpCol.anchorMin = { x: 1, y: 0 };
bpCol.anchorMax = { x: 1, y: 1 };
bpCol.pivot = { x: 1, y: 0.5 };
bpCol.anchoredPosition = { x: -20, y: -18 };
bpCol.sizeDelta = { x: 390, y: -140 };
bpCol.crId = genId();
bpCol.imgId = genId();
bpCol.frameColor = { r: 0.07, g: 0.08, b: 0.12, a: 0.95 };
merchantWindow.addChild(bpCol);

const bpHeader = reg(new Node('BackpackHeader'));
bpHeader.anchorMin = { x: 0, y: 1 };
bpHeader.anchorMax = { x: 1, y: 1 };
bpHeader.pivot = { x: 0.5, y: 1 };
bpHeader.anchoredPosition = { x: 0, y: -8 };
bpHeader.sizeDelta = { x: -20, y: 26 };
bpHeader.crId = genId();
bpHeader.tmpId = genId();
bpHeader.text = 'PLAYER BACKPACK';
bpHeader.fontSize = 13.5;
bpHeader.fontStyle = 1;
bpHeader.align = 514;
bpHeader.color = { r: 0.4, g: 0.85, b: 1, a: 1 };
bpCol.addChild(bpHeader);

const bpScroll = reg(new Node('BackpackScrollArea'));
bpScroll.anchorMin = { x: 0, y: 0 };
bpScroll.anchorMax = { x: 1, y: 1 };
bpScroll.offsetMin = { x: 8, y: 8 };
bpScroll.offsetMax = { x: -8, y: -38 };
bpScroll.scrollId = genId();
bpCol.addChild(bpScroll);

const bpVp = reg(new Node('Viewport'));
bpVp.anchorMin = { x: 0, y: 0 };
bpVp.anchorMax = { x: 1, y: 1 };
bpVp.offsetMin = { x: 0, y: 0 };
bpVp.offsetMax = { x: -16, y: 0 };
bpVp.crId = genId();
bpVp.maskId = genId();
bpScroll.addChild(bpVp);

const bpGrid = reg(new Node('BackpackSlots'));
bpGrid.anchorMin = { x: 0, y: 1 };
bpGrid.anchorMax = { x: 1, y: 1 };
bpGrid.pivot = { x: 0.5, y: 1 };
bpGrid.anchoredPosition = { x: 0, y: 0 };
bpGrid.sizeDelta = { x: 0, y: 0 };
bpGrid.glgId = genId();
bpGrid.csfId = genId();
bpVp.addChild(bpGrid);

const bpSb = reg(new Node('VerticalScrollbar'));
bpSb.anchorMin = { x: 1, y: 0 };
bpSb.anchorMax = { x: 1, y: 1 };
bpSb.pivot = { x: 1, y: 0.5 };
bpSb.anchoredPosition = { x: 0, y: 0 };
bpSb.sizeDelta = { x: 12, y: 0 };
bpSb.crId = genId();
bpSb.imgId = genId();
bpSb.sbId = genId();
bpScroll.addChild(bpSb);

const bpSa = reg(new Node('SlidingArea'));
bpSa.anchorMin = { x: 0, y: 0 };
bpSa.anchorMax = { x: 1, y: 1 };
bpSa.sizeDelta = { x: 0, y: 0 };
bpSb.addChild(bpSa);

const bpHandle = reg(new Node('Handle'));
bpHandle.sizeDelta = { x: 0, y: 0 };
bpHandle.crId = genId();
bpHandle.imgId = genId();
bpHandle.frameColor = { r: 0.25, g: 0.42, b: 0.65, a: 1 };
bpSa.addChild(bpHandle);

// Continue Button at bottom of window
const merchantContinueBtn = reg(new Node('BtnContinue'));
merchantContinueBtn.anchorMin = { x: 0.5, y: 0 };
merchantContinueBtn.anchorMax = { x: 0.5, y: 0 };
merchantContinueBtn.pivot = { x: 0.5, y: 0 };
merchantContinueBtn.anchoredPosition = { x: 0, y: 12 };
merchantContinueBtn.sizeDelta = { x: 300, y: 42 };
merchantContinueBtn.crId = genId();
merchantContinueBtn.imgId = genId();
merchantContinueBtn.btnId = genId();
merchantContinueBtn.btnColor = { r: 0.18, g: 0.55, b: 0.32, a: 1 };
merchantContinueBtn.targetMethod = 'Close';
merchantContinueBtn.targetUI = merchantCanvas.uiId;
merchantWindow.addChild(merchantContinueBtn);

const merchantContinueText = reg(new Node('Text'));
merchantContinueText.anchorMin = { x: 0, y: 0 };
merchantContinueText.anchorMax = { x: 1, y: 1 };
merchantContinueText.sizeDelta = { x: 0, y: 0 };
merchantContinueText.crId = genId();
merchantContinueText.tmpId = genId();
merchantContinueText.text = 'CONTINUE EXPEDITION >>';
merchantContinueText.fontSize = 13.5;
merchantContinueText.fontStyle = 1;
merchantContinueText.align = 514;
merchantContinueText.color = { r: 1, g: 1, b: 1, a: 1 };
merchantContinueBtn.addChild(merchantContinueText);


// ==========================================
// YAML EMISSION
// ==========================================
const out = [];
function emit(s) { out.push(s); }

for (const n of allNodes) {
  // 1. GameObject
  const compList = [`  - component: {fileID: ${n.rtId}}`];
  if (n.canvasId) compList.push(`  - component: {fileID: ${n.canvasId}}`);
  if (n.scalerId) compList.push(`  - component: {fileID: ${n.scalerId}}`);
  if (n.raycasterId) compList.push(`  - component: {fileID: ${n.raycasterId}}`);
  if (n.uiId) compList.push(`  - component: {fileID: ${n.uiId}}`);
  if (n.crId) compList.push(`  - component: {fileID: ${n.crId}}`);
  if (n.imgId) compList.push(`  - component: {fileID: ${n.imgId}}`);
  if (n.btnId) compList.push(`  - component: {fileID: ${n.btnId}}`);
  if (n.tmpId) compList.push(`  - component: {fileID: ${n.tmpId}}`);
  if (n.scrollId) compList.push(`  - component: {fileID: ${n.scrollId}}`);
  if (n.maskId) compList.push(`  - component: {fileID: ${n.maskId}}`);
  if (n.vlgId) compList.push(`  - component: {fileID: ${n.vlgId}}`);
  if (n.glgId) compList.push(`  - component: {fileID: ${n.glgId}}`);
  if (n.csfId) compList.push(`  - component: {fileID: ${n.csfId}}`);
  if (n.sbId) compList.push(`  - component: {fileID: ${n.sbId}}`);

  emit(`--- !u!1 &${n.goId}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
${compList.join('\n')}
  m_Layer: ${n.layer}
  m_Name: ${n.name}
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: ${n.isActive}`);

  // 2. RectTransform
  const childrenLines = n.children.length === 0 ? '  m_Children: []' : `  m_Children:\n${n.children.map(c => `  - {fileID: ${c.rtId}}`).join('\n')}`;
  const fatherLine = n.father ? `  m_Father: {fileID: ${n.father.rtId}}` : `  m_Father: {fileID: 0}`;

  if (n.isRoot) {
    n.anchorMin = { x: 0, y: 0 };
    n.anchorMax = { x: 0, y: 0 };
    n.pivot = { x: 0, y: 0 };
    n.anchoredPosition = { x: 0, y: 0 };
    n.sizeDelta = { x: 0, y: 0 };
    n.localScale = { x: 0, y: 0, z: 0 };
  } else if (n.offsetMin && n.offsetMax) {
    const sizeX = n.offsetMax.x - n.offsetMin.x;
    const sizeY = n.offsetMax.y - n.offsetMin.y;
    const posX = n.offsetMin.x + sizeX * n.pivot.x;
    const posY = n.offsetMin.y + sizeY * n.pivot.y;
    n.sizeDelta = { x: sizeX, y: sizeY };
    n.anchoredPosition = { x: posX, y: posY };
  }

  emit(`--- !u!224 &${n.rtId}
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${n.goId}}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: ${n.localScale ? n.localScale.x : 1}, y: ${n.localScale ? n.localScale.y : 1}, z: ${n.localScale ? n.localScale.z : 1}}
  m_ConstrainProportionsScale: 0
${childrenLines}
${fatherLine}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: ${n.anchorMin.x}, y: ${n.anchorMin.y}}
  m_AnchorMax: {x: ${n.anchorMax.x}, y: ${n.anchorMax.y}}
  m_AnchoredPosition: {x: ${n.anchoredPosition.x}, y: ${n.anchoredPosition.y}}
  m_SizeDelta: {x: ${n.sizeDelta.x}, y: ${n.sizeDelta.y}}
  m_Pivot: {x: ${n.pivot.x}, y: ${n.pivot.y}}`);

  // 3. Canvas (for root nodes)
  if (n.canvasId) {
    emit(`--- !u!223 &${n.canvasId}
Canvas:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${n.goId}}
  m_Enabled: 1
  serializedVersion: 3
  m_RenderMode: 0
  m_Camera: {fileID: 0}
  m_PlaneDistance: 100
  m_PixelPerfect: 0
  m_ReceivesEvents: 1
  m_OverrideSorting: 0
  m_OverridePixelPerfect: 0
  m_SortingBucketNormalizedSize: 0
  m_VertexColorAlwaysGammaSpace: 0
  m_AdditionalShaderChannelsFlag: 25
  m_UpdateRectTransformForStandalone: 0
  m_SortingLayerID: 0
  m_SortingOrder: ${n.sortingOrder}
  m_TargetDisplay: 0
--- !u!114 &${n.scalerId}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${n.goId}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${GUID_CANVAS_SCALER}, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.CanvasScaler
  m_UiScaleMode: 1
  m_ReferencePixelsPerUnit: 100
  m_ScaleFactor: 1
  m_ReferenceResolution: {x: 1920, y: 1080}
  m_ScreenMatchMode: 0
  m_MatchWidthOrHeight: 0.5
  m_PhysicalUnit: 3
  m_FallbackScreenDPI: 96
  m_DefaultSpriteDPI: 96
  m_DynamicPixelsPerUnit: 1
  m_PresetInfoIsWorld: 0
--- !u!114 &${n.raycasterId}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${n.goId}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${GUID_GRAPHIC_RAYCASTER}, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.GraphicRaycaster
  m_IgnoreReversedGraphics: 1
  m_BlockingObjects: 0
  m_BlockingMask:
    serializedVersion: 2
    m_Bits: 4294967295`);
  }

  // 4. CanvasRenderer
  if (n.crId) {
    emit(`--- !u!222 &${n.crId}
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${n.goId}}
  m_CullTransparentMesh: 1`);
  }

  // 5. Image
  if (n.imgId) {
    let col = n.btnColor || n.frameColor || n.color || { r: 1, g: 1, b: 1, a: 1 };
    let spriteId = SPRITE_FRAME;
    let spriteGuid = GUID_FRAME;
    let imgType = 1; // Sliced
    if (n.name === 'DungeonEventPanel' || n.name === 'MerchantPanel' || n.name === 'VerticalScrollbar') {
      imgType = 0; // Simple
      spriteId = '0';
      spriteGuid = '00000000000000000000000000000000';
    }

    emit(`--- !u!114 &${n.imgId}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${n.goId}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${GUID_IMAGE}, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Image
  m_Material: {fileID: 0}
  m_Color: {r: ${col.r}, g: ${col.g}, b: ${col.b}, a: ${col.a}}
  m_RaycastTarget: 1
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: {fileID: ${spriteId}, guid: ${spriteGuid}, type: 3}
  m_Type: ${imgType}
  m_PreserveAspect: 0
  m_FillCenter: 1
  m_FillMethod: 0
  m_FillAmount: 0
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseSpriteMesh: 0
  m_PixelsPerUnitMultiplier: 1`);
  }

  // 6. Button
  if (n.btnId) {
    let targetUI = n.targetUI || (n.father.father === eventCanvas || n.father === eventCanvas ? eventCanvas.uiId : merchantCanvas.uiId);
    let actionMethod = n.targetMethod || 'Close';
    emit(`--- !u!114 &${n.btnId}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${n.goId}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${GUID_BUTTON}, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Button
  m_Navigation:
    m_Mode: 3
    m_WrapAround: 0
    m_SelectOnUp: {fileID: 0}
    m_SelectOnDown: {fileID: 0}
    m_SelectOnLeft: {fileID: 0}
    m_SelectOnRight: {fileID: 0}
  m_Transition: 1
  m_Colors:
    m_NormalColor: {r: 1, g: 1, b: 1, a: 1}
    m_HighlightedColor: {r: 1.15, g: 1.15, b: 1.15, a: 1}
    m_PressedColor: {r: 0.85, g: 0.85, b: 0.85, a: 1}
    m_SelectedColor: {r: 1, g: 1, b: 1, a: 1}
    m_DisabledColor: {r: 0.5, g: 0.5, b: 0.5, a: 0.5}
    m_ColorMultiplier: 1
    m_FadeDuration: 0.1
  m_SpriteState:
    m_HighlightedSprite: {fileID: 0}
    m_PressedSprite: {fileID: 0}
    m_SelectedSprite: {fileID: 0}
    m_DisabledSprite: {fileID: 0}
  m_AnimationTriggers:
    m_NormalTrigger: Normal
    m_HighlightedTrigger: Highlighted
    m_PressedTrigger: Pressed
    m_SelectedTrigger: Selected
    m_DisabledTrigger: Disabled
  m_Interactable: 1
  m_TargetGraphic: {fileID: ${n.imgId}}
  m_OnClick:
    m_PersistentCalls:
      m_Calls:
      - m_Target: {fileID: ${targetUI}}
        m_TargetAssemblyTypeName: Assembly-CSharp
        m_MethodName: ${actionMethod}
        m_Mode: 1
        m_Arguments:
          m_ObjectArgument: {fileID: 0}
          m_ObjectArgumentAssemblyTypeName: UnityEngine.Object, UnityEngine
          m_IntArgument: 0
          m_FloatArgument: 0
          m_StringArgument: 
          m_BoolArgument: 0
        m_CallState: 2`);
  }

  // 7. TextMeshProUGUI
  if (n.tmpId) {
    let col = n.color || { r: 1, g: 1, b: 1, a: 1 };
    let align = n.align || 257;
    let fontStyle = n.fontStyle || 0;
    emit(`--- !u!114 &${n.tmpId}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${n.goId}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${GUID_TMP}, type: 3}
  m_Name: 
  m_EditorClassIdentifier: Unity.TextMeshPro::TMPro.TextMeshProUGUI
  m_Material: {fileID: 0}
  m_Color: {r: 1, g: 1, b: 1, a: 1}
  m_RaycastTarget: 0
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_text: '${n.text}'
  m_isRightToLeft: 0
  m_fontAsset: {fileID: 11400000, guid: ${GUID_FONT}, type: 2}
  m_sharedMaterial: {fileID: 2180264, guid: ${GUID_FONT}, type: 2}
  m_fontSharedMaterials: []
  m_fontMaterial: {fileID: 0}
  m_fontMaterials: []
  m_fontColor32:
    serializedVersion: 2
    rgba: 4294967295
  m_fontColor: {r: ${col.r}, g: ${col.g}, b: ${col.b}, a: ${col.a}}
  m_enableVertexGradient: 0
  m_colorMode: 3
  m_fontSize: ${n.fontSize || 14}
  m_fontSizeBase: ${n.fontSize || 14}
  m_fontWeight: 400
  m_enableAutoSizing: 0
  m_fontSizeMin: 18
  m_fontSizeMax: 72
  m_fontStyle: ${fontStyle}
  m_HorizontalAlignment: 2
  m_VerticalAlignment: 512
  m_textAlignment: 65535
  m_characterSpacing: 0
  m_wordSpacing: 0
  m_lineSpacing: 0
  m_lineSpacingMax: 0
  m_paragraphSpacing: 0
  m_charWidthMaxAdj: 0
  m_enableWordWrapping: 1
  m_wordWrappingRatios: 0.4
  m_overflowMode: 0
  m_linkedTextComponent: {fileID: 0}
  parentLinkedComponent: {fileID: 0}
  m_enableKerning: 1
  m_enableExtraPadding: 0
  checkPaddingRequired: 0
  m_isRichText: 1
  m_parseCtrlCharacters: 1
  m_isOrthographic: 1
  m_isCullingEnabled: 0
  m_horizontalMapping: 0
  m_verticalMapping: 0
  m_uvLineOffset: 0
  m_geometrySortingOrder: 0
  m_IsTextObjectScaleStatic: 0
  m_VertexBufferAutoSizeReduction: 0
  m_useMaxVisibleDescender: 1
  m_pageToDisplay: 1
  m_margin: {x: 0, y: 0, z: 0, w: 0}
  m_isUsingLegacyAnimationComponent: 0
  m_isVolumetricText: 0
  m_hasFontAssetChanged: 0
  m_baseMaterial: {fileID: 0}
  m_maskOffset: {x: 0, y: 0, z: 0, w: 0}`);
  }

  // 8. ScrollRect
  if (n.scrollId) {
    let vp = n === waresCol ? waresVp : bpVp;
    let content = n === waresCol ? waresContent : bpGrid;
    let sb = n === waresCol ? waresSb : bpSb;
    emit(`--- !u!114 &${n.scrollId}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${n.goId}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${GUID_SCROLLRECT}, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.ScrollRect
  m_Content: {fileID: ${content.rtId}}
  m_Horizontal: 0
  m_Vertical: 1
  m_MovementType: 1
  m_Elasticity: 0.1
  m_Inertia: 1
  m_DecelerationRate: 0.135
  m_ScrollSensitivity: 25
  m_Viewport: {fileID: ${vp.rtId}}
  m_HorizontalScrollbar: {fileID: 0}
  m_VerticalScrollbar: {fileID: ${sb.sbId}}
  m_HorizontalScrollbarVisibility: 2
  m_VerticalScrollbarVisibility: 2
  m_HorizontalScrollbarSpacing: 0
  m_VerticalScrollbarSpacing: 0
  m_OnValueChanged:
    m_PersistentCalls:
      m_Calls: []`);
  }

  // 9. RectMask2D
  if (n.maskId) {
    emit(`--- !u!114 &${n.maskId}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${n.goId}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${GUID_RECTMASK2D}, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.RectMask2D
  m_Padding: {x: 0, y: 0, z: 0, w: 0}
  m_Softness: {x: 0, y: 0}`);
  }

  // 10. VerticalLayoutGroup
  if (n.vlgId) {
    let sp = n.vlgSpacing || 8;
    emit(`--- !u!114 &${n.vlgId}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${n.goId}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${GUID_VLG}, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.VerticalLayoutGroup
  m_Padding:
    m_Left: 6
    m_Right: 6
    m_Top: 6
    m_Bottom: 6
  m_ChildAlignment: 0
  m_Spacing: ${sp}
  m_ChildForceExpandWidth: 1
  m_ChildForceExpandHeight: 0
  m_ChildControlWidth: 1
  m_ChildControlHeight: 1
  m_ChildScaleWidth: 0
  m_ChildScaleHeight: 0
  m_ReverseArrangement: 0`);
  }

  // 11. GridLayoutGroup
  if (n.glgId) {
    emit(`--- !u!114 &${n.glgId}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${n.goId}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${GUID_GLG}, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.GridLayoutGroup
  m_Padding:
    m_Left: 6
    m_Right: 6
    m_Top: 6
    m_Bottom: 6
  m_CellSize: {x: 50, y: 50}
  m_Spacing: {x: 7, y: 7}
  m_StartCorner: 0
  m_StartAxis: 0
  m_ChildAlignment: 1
  m_Constraint: 1
  m_ConstraintCount: 6`);
  }

  // 12. ContentSizeFitter
  if (n.csfId) {
    emit(`--- !u!114 &${n.csfId}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${n.goId}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${GUID_CSF}, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.ContentSizeFitter
  m_HorizontalFit: 0
  m_VerticalFit: 2`);
  }

  // 13. Scrollbar
  if (n.sbId) {
    let handle = n === waresSb ? waresHandle : bpHandle;
    emit(`--- !u!114 &${n.sbId}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${n.goId}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${GUID_SCROLLBAR}, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Scrollbar
  m_Navigation:
    m_Mode: 3
    m_WrapAround: 0
    m_SelectOnUp: {fileID: 0}
    m_SelectOnDown: {fileID: 0}
    m_SelectOnLeft: {fileID: 0}
    m_SelectOnRight: {fileID: 0}
  m_Transition: 1
  m_Colors:
    m_NormalColor: {r: 1, g: 1, b: 1, a: 1}
    m_HighlightedColor: {r: 0.96, g: 0.96, b: 0.96, a: 1}
    m_PressedColor: {r: 0.78, g: 0.78, b: 0.78, a: 1}
    m_SelectedColor: {r: 0.96, g: 0.96, b: 0.96, a: 1}
    m_DisabledColor: {r: 0.78, g: 0.78, b: 0.78, a: 0.5}
    m_ColorMultiplier: 1
    m_FadeDuration: 0.1
  m_SpriteState:
    m_HighlightedSprite: {fileID: 0}
    m_PressedSprite: {fileID: 0}
    m_SelectedSprite: {fileID: 0}
    m_DisabledSprite: {fileID: 0}
  m_AnimationTriggers:
    m_NormalTrigger: Normal
    m_HighlightedTrigger: Highlighted
    m_PressedTrigger: Pressed
    m_SelectedTrigger: Selected
    m_DisabledTrigger: Disabled
  m_Interactable: 1
  m_TargetGraphic: {fileID: ${handle.imgId}}
  m_HandleRect: {fileID: ${handle.rtId}}
  m_Direction: 2
  m_Value: 0
  m_Size: 0.2
  m_NumberOfSteps: 0
  m_OnValueChanged:
    m_PersistentCalls:
      m_Calls: []`);
  }
}

// Emitting Custom UI components
// 1. DungeonEventUI
emit(`--- !u!114 &${eventCanvas.uiId}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${eventCanvas.goId}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${GUID_EVENT_UI}, type: 3}
  m_Name: 
  m_EditorClassIdentifier: Assembly-CSharp::DungeonEventUI
  eventPanel: {fileID: ${eventOverlay.goId}}
  choicesContainer: {fileID: ${choicesContainer.rtId}}
  canvas: {fileID: ${eventCanvas.canvasId}}
  categoryText: {fileID: ${categoryText.tmpId}}
  titleText: {fileID: ${titleText.tmpId}}
  narrativeText: {fileID: ${narrativeText.tmpId}}
  statusFeedbackText: {fileID: ${statusFeedbackText.tmpId}}
  continueButton: {fileID: ${continueButton.btnId}}`);

// 2. MerchantUI
emit(`--- !u!114 &${merchantCanvas.uiId}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${merchantCanvas.goId}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${GUID_MERCHANT_UI}, type: 3}
  m_Name: 
  m_EditorClassIdentifier: Assembly-CSharp::MerchantUI
  merchantPanel: {fileID: ${merchantOverlay.goId}}
  waresContainer: {fileID: ${waresContent.rtId}}
  backpackSlotsParent: {fileID: ${bpGrid.rtId}}
  canvas: {fileID: ${merchantCanvas.canvasId}}
  slotPrefab: {fileID: ${FILEID_SLOT_PREFAB}, guid: ${GUID_SLOT_PREFAB}, type: 3}
  titleText: {fileID: ${merchantTitle.tmpId}}
  subtitleText: {fileID: ${merchantSubtitle.tmpId}}
  feedbackText: {fileID: ${merchantFeedback.tmpId}}
  backpackHeaderTitle: {fileID: ${bpHeader.tmpId}}
  continueButton: {fileID: ${merchantContinueBtn.btnId}}
  closeButton: {fileID: ${merchantCloseBtn.btnId}}`);

// ==========================================
// APPLY TO SCENE
// ==========================================
const scenePath = 'Assets/_Scenes/Dungeon.unity';
let sceneContent = fs.readFileSync(scenePath, 'utf8');

// 1. Remove empty DungeonEventUI component reference from CombatCanvas if present
if (sceneContent.includes('- component: {fileID: 1873114121}')) {
  sceneContent = sceneContent.replace('  - component: {fileID: 1873114121}\r\n', '');
  sceneContent = sceneContent.replace('  - component: {fileID: 1873114121}\n', '');
  console.log('Removed empty DungeonEventUI component reference from CombatCanvas.');
}

// 2. Remove old unassigned DungeonEventUI block if present
const eventUIBlockRegex = /--- !u!114 &1873114121[\s\S]*?(?=--- !u!|\Z)/;
if (eventUIBlockRegex.test(sceneContent)) {
  sceneContent = sceneContent.replace(eventUIBlockRegex, '');
  console.log('Removed old empty DungeonEventUI block 1873114121 from scene.');
}

// 3. Remove empty MerchantUI component reference from CombatCanvas if present
if (sceneContent.includes('- component: {fileID: 1873114122}')) {
  sceneContent = sceneContent.replace('  - component: {fileID: 1873114122}\r\n', '');
  sceneContent = sceneContent.replace('  - component: {fileID: 1873114122}\n', '');
  console.log('Removed empty MerchantUI component reference from CombatCanvas.');
}

// 4. Remove old unassigned MerchantUI block if present
const merchantUIBlockRegex = /--- !u!114 &1873114122[\s\S]*?(?=--- !u!|\Z)/;
if (merchantUIBlockRegex.test(sceneContent)) {
  sceneContent = sceneContent.replace(merchantUIBlockRegex, '');
  console.log('Removed old empty MerchantUI block 1873114122 from scene.');
}

// 5. Remove previous DungeonEventCanvas or MerchantCanvas if script re-runs
if (sceneContent.includes('m_Name: DungeonEventCanvas')) {
  const eventIdx = sceneContent.indexOf('m_Name: DungeonEventCanvas');
  const eventStart = sceneContent.lastIndexOf('--- !u!1 &', eventIdx);
  sceneContent = sceneContent.substring(0, eventStart);
  console.log('Cleaned up previous DungeonEventCanvas blocks.');
}

const blocks = out.join('\n') + '\n';
sceneContent = sceneContent.trimEnd() + '\n' + blocks;

fs.writeFileSync(scenePath, sceneContent, 'utf8');
console.log(`Successfully inserted DungeonEventCanvas and MerchantCanvas into ${scenePath}!`);
