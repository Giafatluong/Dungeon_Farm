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
const GUID_HLG = '30649d3a9faa99c48a7b1166b86bf2a0';
const GUID_GLG = '8a8695521f0d02e499659fee002a26c2';
const GUID_CSF = '3245ec927659c4140ac4f8d17403cc18';
const GUID_RECTMASK2D = '3312d7739989d2b4e91e6319e9a96d76';

// Custom Script GUIDs
const GUID_LOOT_UI = '8e8f3b20756a422db7406a4b12df71bb';
const GUID_REWARD_UI = 'd7313e0ea07047cb9e23d856e6539add';
const GUID_VICTORY_UI = '8f9b4c2e6d1a4e7b9c3f5a8d2e1b4c7f';
const GUID_SUMMARY_UI = '9f1da94f554716343a7786c9696f7966';

// Asset GUIDs
const GUID_FONT = '8f586378b4e144a9851e7b34d9b748ee';
const GUID_FRAME = 'aa6f9a2ecff34c14d84e5bd91727c940';
const SPRITE_FRAME = '-5668518402485374812';
const GUID_TOOLBAR = 'aa6f9a2ecff34c14d84e5bd91727c940';
const SPRITE_TOOLBAR = '-6408785361026046777';
const GUID_CHEST = '641238446beb267429ad1bb17d75680e';
const SPRITE_CHEST = '1921129497';
const GUID_SLOT_PREFAB = '02cead57d3ffaca44b4e995473eaea19';
const FILEID_SLOT_PREFAB = '3631650896398165525';

let nextId = 950000000;
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
    this.hlgId = null;
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

function addScrollbar(parentScrollNode, handleColor) {
  const sbNode = reg(new Node('VerticalScrollbar'));
  sbNode.anchorMin = { x: 1, y: 0 };
  sbNode.anchorMax = { x: 1, y: 1 };
  sbNode.pivot = { x: 1, y: 0.5 };
  sbNode.sizeDelta = { x: 12, y: 0 };
  sbNode.anchoredPosition = { x: 0, y: 0 };
  sbNode.crId = genId();
  sbNode.imgId = genId();
  sbNode.sbId = genId();
  sbNode.color = { r: 0.04, g: 0.06, b: 0.09, a: 0.8 };
  parentScrollNode.addChild(sbNode);

  const saNode = reg(new Node('SlidingArea'));
  saNode.anchorMin = { x: 0, y: 0 };
  saNode.anchorMax = { x: 1, y: 1 };
  saNode.sizeDelta = { x: 0, y: 0 };
  sbNode.addChild(saNode);

  const handleNode = reg(new Node('Handle'));
  handleNode.sizeDelta = { x: 0, y: 0 };
  handleNode.crId = genId();
  handleNode.imgId = genId();
  handleNode.frameColor = handleColor;
  saNode.addChild(handleNode);

  sbNode.sbHandle = handleNode.rtId;
  sbNode.sbGraphic = handleNode.imgId;
  parentScrollNode.scrollbarId = sbNode.sbId;
  return sbNode;
}

// ==========================================
// 1. BUILD LOOT CANVAS
// ==========================================
const lootCanvas = reg(new Node('LootCanvas', true));
lootCanvas.canvasId = genId();
lootCanvas.scalerId = genId();
lootCanvas.raycasterId = genId();
lootCanvas.uiId = genId();
lootCanvas.sortingOrder = 110;

const lootOverlay = reg(new Node('LootPanel'));
lootOverlay.anchorMin = { x: 0, y: 0 };
lootOverlay.anchorMax = { x: 1, y: 1 };
lootOverlay.sizeDelta = { x: 0, y: 0 };
lootOverlay.isActive = 0;
lootOverlay.crId = genId();
lootOverlay.imgId = genId();
lootOverlay.color = { r: 0.02, g: 0.03, b: 0.05, a: 0.94 };
lootCanvas.addChild(lootOverlay);

const lootModal = reg(new Node('LootModal'));
lootModal.sizeDelta = { x: 940, y: 580 };
lootModal.crId = genId();
lootModal.imgId = genId();
lootModal.frameColor = { r: 0.10, g: 0.12, b: 0.17, a: 0.98 };
lootOverlay.addChild(lootModal);

// Loot Header
const lootHeader = reg(new Node('Header'));
lootHeader.anchorMin = { x: 0, y: 1 };
lootHeader.anchorMax = { x: 1, y: 1 };
lootHeader.pivot = { x: 0.5, y: 1 };
lootHeader.sizeDelta = { x: 0, y: 74 };
lootHeader.crId = genId();
lootHeader.imgId = genId();
lootHeader.frameColor = { r: 0.22, g: 0.16, b: 0.06, a: 1 };
lootModal.addChild(lootHeader);

const lootChestIcon = reg(new Node('ChestIcon'));
lootChestIcon.anchorMin = { x: 0, y: 0.5 };
lootChestIcon.anchorMax = { x: 0, y: 0.5 };
lootChestIcon.anchoredPosition = { x: 38, y: 0 };
lootChestIcon.sizeDelta = { x: 48, y: 48 };
lootChestIcon.crId = genId();
lootChestIcon.imgId = genId();
lootChestIcon.isChestSprite = true;
lootHeader.addChild(lootChestIcon);

const lootTitleText = reg(new Node('TitleText'));
lootTitleText.anchorMin = { x: 0, y: 0 };
lootTitleText.anchorMax = { x: 1, y: 1 };
lootTitleText.offsetMin = { x: 76, y: 24 };
lootTitleText.offsetMax = { x: -20, y: -6 };
lootTitleText.crId = genId();
lootTitleText.tmpId = genId();
lootTitleText.text = 'ENEMY LOOT DROPS';
lootTitleText.fontSize = 20;
lootTitleText.fontStyle = 1;
lootTitleText.align = 258; // MidlineLeft
lootTitleText.color = { r: 1, g: 0.85, b: 0.25, a: 1 };
lootHeader.addChild(lootTitleText);

const lootSubtitleText = reg(new Node('SubtitleText'));
lootSubtitleText.anchorMin = { x: 0, y: 0 };
lootSubtitleText.anchorMax = { x: 1, y: 1 };
lootSubtitleText.offsetMin = { x: 76, y: 4 };
lootSubtitleText.offsetMax = { x: -20, y: -32 };
lootSubtitleText.crId = genId();
lootSubtitleText.tmpId = genId();
lootSubtitleText.text = 'Drag and drop items into your Backpack, or click [LOOT ALL] to collect everything.';
lootSubtitleText.fontSize = 12;
lootSubtitleText.align = 258;
lootSubtitleText.color = { r: 0.85, g: 0.88, b: 0.95, a: 1 };
lootHeader.addChild(lootSubtitleText);

// Loot Body
const lootBody = reg(new Node('Body'));
lootBody.anchorMin = { x: 0, y: 0 };
lootBody.anchorMax = { x: 1, y: 1 };
lootBody.offsetMin = { x: 20, y: 68 };
lootBody.offsetMax = { x: -20, y: -82 };
lootModal.addChild(lootBody);

// Left Column: Dropped Loot
const leftLootCol = reg(new Node('Left_LootColumn'));
leftLootCol.anchorMin = { x: 0, y: 0 };
leftLootCol.anchorMax = { x: 0.488, y: 1 };
leftLootCol.offsetMin = { x: 0, y: 0 };
leftLootCol.offsetMax = { x: 0, y: 0 };
leftLootCol.crId = genId();
leftLootCol.imgId = genId();
leftLootCol.frameColor = { r: 0.14, g: 0.16, b: 0.22, a: 0.95 };
lootBody.addChild(leftLootCol);

const leftLootHeader = reg(new Node('HeaderBar'));
leftLootHeader.anchorMin = { x: 0, y: 1 };
leftLootHeader.anchorMax = { x: 1, y: 1 };
leftLootHeader.pivot = { x: 0.5, y: 1 };
leftLootHeader.sizeDelta = { x: -12, y: 32 };
leftLootHeader.anchoredPosition = { x: 0, y: -6 };
leftLootHeader.crId = genId();
leftLootHeader.imgId = genId();
leftLootHeader.frameColor = { r: 0.19, g: 0.22, b: 0.30, a: 1 };
leftLootCol.addChild(leftLootHeader);

const leftLootLabel = reg(new Node('Label'));
leftLootLabel.anchorMin = { x: 0, y: 0 };
leftLootLabel.anchorMax = { x: 1, y: 1 };
leftLootLabel.offsetMin = { x: 10, y: 0 };
leftLootLabel.offsetMax = { x: -10, y: 0 };
leftLootLabel.crId = genId();
leftLootLabel.tmpId = genId();
leftLootLabel.text = 'DROPPED LOOT';
leftLootLabel.fontSize = 12.5;
leftLootLabel.fontStyle = 1;
leftLootLabel.align = 258;
leftLootLabel.color = { r: 1, g: 0.85, b: 0.35, a: 1 };
leftLootHeader.addChild(leftLootLabel);

const leftLootScroll = reg(new Node('ScrollView'));
leftLootScroll.anchorMin = { x: 0, y: 0 };
leftLootScroll.anchorMax = { x: 1, y: 1 };
leftLootScroll.offsetMin = { x: 8, y: 8 };
leftLootScroll.offsetMax = { x: -8, y: -42 };
leftLootScroll.crId = genId();
leftLootScroll.imgId = genId();
leftLootScroll.color = { r: 0, g: 0, b: 0, a: 0.2 };
leftLootScroll.scrollId = genId();
leftLootCol.addChild(leftLootScroll);

const leftLootVp = reg(new Node('Viewport'));
leftLootVp.anchorMin = { x: 0, y: 0 };
leftLootVp.anchorMax = { x: 1, y: 1 };
leftLootVp.offsetMin = { x: 0, y: 0 };
leftLootVp.offsetMax = { x: -14, y: 0 };
leftLootVp.maskId = genId();
leftLootScroll.addChild(leftLootVp);

const leftLootContent = reg(new Node('Content'));
leftLootContent.anchorMin = { x: 0, y: 1 };
leftLootContent.anchorMax = { x: 1, y: 1 };
leftLootContent.pivot = { x: 0.5, y: 1 };
leftLootContent.sizeDelta = { x: 0, y: 300 };
leftLootContent.glgId = genId();
leftLootContent.glgCell = { x: 54, y: 54 };
leftLootContent.glgSpacing = { x: 6, y: 6 };
leftLootContent.glgPad = { l: 8, r: 8, t: 8, b: 8 };
leftLootContent.glgCount = 6;
leftLootContent.csfId = genId();
leftLootVp.addChild(leftLootContent);

leftLootScroll.viewportId = leftLootVp.rtId;
leftLootScroll.contentId = leftLootContent.rtId;
addScrollbar(leftLootScroll, { r: 0.75, g: 0.60, b: 0.30, a: 0.95 });

// Right Column: Backpack
const rightLootCol = reg(new Node('Right_BackpackColumn'));
rightLootCol.anchorMin = { x: 0.512, y: 0 };
rightLootCol.anchorMax = { x: 1, y: 1 };
rightLootCol.offsetMin = { x: 0, y: 0 };
rightLootCol.offsetMax = { x: 0, y: 0 };
rightLootCol.crId = genId();
rightLootCol.imgId = genId();
rightLootCol.frameColor = { r: 0.14, g: 0.16, b: 0.22, a: 0.95 };
lootBody.addChild(rightLootCol);

const rightLootHeader = reg(new Node('HeaderBar'));
rightLootHeader.anchorMin = { x: 0, y: 1 };
rightLootHeader.anchorMax = { x: 1, y: 1 };
rightLootHeader.pivot = { x: 0.5, y: 1 };
rightLootHeader.sizeDelta = { x: -12, y: 32 };
rightLootHeader.anchoredPosition = { x: 0, y: -6 };
rightLootHeader.crId = genId();
rightLootHeader.imgId = genId();
rightLootHeader.frameColor = { r: 0.19, g: 0.22, b: 0.30, a: 1 };
rightLootCol.addChild(rightLootHeader);

const rightLootLabel = reg(new Node('BackpackTitle'));
rightLootLabel.anchorMin = { x: 0, y: 0 };
rightLootLabel.anchorMax = { x: 1, y: 1 };
rightLootLabel.offsetMin = { x: 10, y: 0 };
rightLootLabel.offsetMax = { x: -10, y: 0 };
rightLootLabel.crId = genId();
rightLootLabel.tmpId = genId();
rightLootLabel.text = 'YOUR BACKPACK';
rightLootLabel.fontSize = 12.5;
rightLootLabel.fontStyle = 1;
rightLootLabel.align = 258;
rightLootLabel.color = { r: 0.55, g: 0.95, b: 1, a: 1 };
rightLootHeader.addChild(rightLootLabel);

const rightLootScroll = reg(new Node('ScrollView'));
rightLootScroll.anchorMin = { x: 0, y: 0 };
rightLootScroll.anchorMax = { x: 1, y: 1 };
rightLootScroll.offsetMin = { x: 8, y: 8 };
rightLootScroll.offsetMax = { x: -8, y: -42 };
rightLootScroll.crId = genId();
rightLootScroll.imgId = genId();
rightLootScroll.color = { r: 0, g: 0, b: 0, a: 0.2 };
rightLootScroll.scrollId = genId();
rightLootCol.addChild(rightLootScroll);

const rightLootVp = reg(new Node('Viewport'));
rightLootVp.anchorMin = { x: 0, y: 0 };
rightLootVp.anchorMax = { x: 1, y: 1 };
rightLootVp.offsetMin = { x: 0, y: 0 };
rightLootVp.offsetMax = { x: -14, y: 0 };
rightLootVp.maskId = genId();
rightLootScroll.addChild(rightLootVp);

const rightLootContent = reg(new Node('Content'));
rightLootContent.anchorMin = { x: 0, y: 1 };
rightLootContent.anchorMax = { x: 1, y: 1 };
rightLootContent.pivot = { x: 0.5, y: 1 };
rightLootContent.sizeDelta = { x: 0, y: 300 };
rightLootContent.glgId = genId();
rightLootContent.glgCell = { x: 54, y: 54 };
rightLootContent.glgSpacing = { x: 6, y: 6 };
rightLootContent.glgPad = { l: 8, r: 8, t: 8, b: 8 };
rightLootContent.glgCount = 6;
rightLootContent.csfId = genId();
rightLootVp.addChild(rightLootContent);

rightLootScroll.viewportId = rightLootVp.rtId;
rightLootScroll.contentId = rightLootContent.rtId;
addScrollbar(rightLootScroll, { r: 0.75, g: 0.60, b: 0.30, a: 0.95 });

// Loot Controls
const lootHintText = reg(new Node('HintText'));
lootHintText.anchorMin = { x: 0, y: 0 };
lootHintText.anchorMax = { x: 0.58, y: 0 };
lootHintText.offsetMin = { x: 24, y: 14 };
lootHintText.offsetMax = { x: 0, y: 50 };
lootHintText.crId = genId();
lootHintText.tmpId = genId();
lootHintText.text = 'Drag and drop items into your Backpack, or click [LOOT ALL] to collect everything.';
lootHintText.fontSize = 12;
lootHintText.align = 258;
lootHintText.color = { r: 1, g: 1, b: 1, a: 1 };
lootModal.addChild(lootHintText);

const lootAllBtn = reg(new Node('LootAllButton'));
lootAllBtn.anchorMin = { x: 1, y: 0 };
lootAllBtn.anchorMax = { x: 1, y: 0 };
lootAllBtn.pivot = { x: 1, y: 0 };
lootAllBtn.anchoredPosition = { x: -180, y: 14 };
lootAllBtn.sizeDelta = { x: 145, y: 38 };
lootAllBtn.crId = genId();
lootAllBtn.imgId = genId();
lootAllBtn.btnId = genId();
lootAllBtn.btnColor = { r: 0.85, g: 0.65, b: 0.15, a: 1 };
lootAllBtn.targetMethod = 'OnLootAllClicked';
lootAllBtn.targetUI = lootCanvas.uiId;
lootModal.addChild(lootAllBtn);

const lootAllTxt = reg(new Node('Text'));
lootAllTxt.anchorMin = { x: 0, y: 0 };
lootAllTxt.anchorMax = { x: 1, y: 1 };
lootAllTxt.sizeDelta = { x: 0, y: 0 };
lootAllTxt.crId = genId();
lootAllTxt.tmpId = genId();
lootAllTxt.text = 'LOOT ALL';
lootAllTxt.fontSize = 13;
lootAllTxt.fontStyle = 1;
lootAllTxt.align = 514;
lootAllTxt.color = { r: 1, g: 1, b: 1, a: 1 };
lootAllBtn.addChild(lootAllTxt);

const lootContBtn = reg(new Node('ContinueButton'));
lootContBtn.anchorMin = { x: 1, y: 0 };
lootContBtn.anchorMax = { x: 1, y: 0 };
lootContBtn.pivot = { x: 1, y: 0 };
lootContBtn.anchoredPosition = { x: -20, y: 14 };
lootContBtn.sizeDelta = { x: 145, y: 38 };
lootContBtn.crId = genId();
lootContBtn.imgId = genId();
lootContBtn.btnId = genId();
lootContBtn.btnColor = { r: 0.18, g: 0.55, b: 0.28, a: 1 };
lootContBtn.targetMethod = 'OnContinueClicked';
lootContBtn.targetUI = lootCanvas.uiId;
lootModal.addChild(lootContBtn);

const lootContTxt = reg(new Node('Text'));
lootContTxt.anchorMin = { x: 0, y: 0 };
lootContTxt.anchorMax = { x: 1, y: 1 };
lootContTxt.sizeDelta = { x: 0, y: 0 };
lootContTxt.crId = genId();
lootContTxt.tmpId = genId();
lootContTxt.text = 'CONTINUE [->]';
lootContTxt.fontSize = 13;
lootContTxt.fontStyle = 1;
lootContTxt.align = 514;
lootContTxt.color = { r: 1, g: 1, b: 1, a: 1 };
lootContBtn.addChild(lootContTxt);

// ==========================================
// 2. BUILD REWARD CANVAS
// ==========================================
const rewardCanvas = reg(new Node('RewardCanvas', true));
rewardCanvas.canvasId = genId();
rewardCanvas.scalerId = genId();
rewardCanvas.raycasterId = genId();
rewardCanvas.uiId = genId();
rewardCanvas.sortingOrder = 112;

const rewardOverlay = reg(new Node('RewardPanel'));
rewardOverlay.anchorMin = { x: 0, y: 0 };
rewardOverlay.anchorMax = { x: 1, y: 1 };
rewardOverlay.sizeDelta = { x: 0, y: 0 };
rewardOverlay.isActive = 0;
rewardOverlay.crId = genId();
rewardOverlay.imgId = genId();
rewardOverlay.color = { r: 0.04, g: 0.05, b: 0.08, a: 0.88 };
rewardCanvas.addChild(rewardOverlay);

const rewardModal = reg(new Node('RewardModal'));
rewardModal.sizeDelta = { x: 920, y: 580 };
rewardModal.crId = genId();
rewardModal.imgId = genId();
rewardModal.frameColor = { r: 0.10, g: 0.12, b: 0.17, a: 0.98 };
rewardOverlay.addChild(rewardModal);

const rewardHeader = reg(new Node('Header'));
rewardHeader.anchorMin = { x: 0, y: 1 };
rewardHeader.anchorMax = { x: 1, y: 1 };
rewardHeader.pivot = { x: 0.5, y: 1 };
rewardHeader.sizeDelta = { x: 0, y: 76 };
rewardHeader.crId = genId();
rewardHeader.imgId = genId();
rewardHeader.frameColor = { r: 0.24, g: 0.18, b: 0.08, a: 1 };
rewardModal.addChild(rewardHeader);

const rewardChestIcon = reg(new Node('ChestIcon'));
rewardChestIcon.anchorMin = { x: 0, y: 0.5 };
rewardChestIcon.anchorMax = { x: 0, y: 0.5 };
rewardChestIcon.anchoredPosition = { x: 42, y: 0 };
rewardChestIcon.sizeDelta = { x: 50, y: 50 };
rewardChestIcon.crId = genId();
rewardChestIcon.imgId = genId();
rewardChestIcon.isChestSprite = true;
rewardHeader.addChild(rewardChestIcon);

const rewardTitleText = reg(new Node('TitleText'));
rewardTitleText.anchorMin = { x: 0, y: 0 };
rewardTitleText.anchorMax = { x: 1, y: 1 };
rewardTitleText.offsetMin = { x: 80, y: 24 };
rewardTitleText.offsetMax = { x: -20, y: -6 };
rewardTitleText.crId = genId();
rewardTitleText.tmpId = genId();
rewardTitleText.text = 'TREASURE VAULT - STAGE REWARD';
rewardTitleText.fontSize = 21;
rewardTitleText.fontStyle = 1;
rewardTitleText.align = 258;
rewardTitleText.color = { r: 1, g: 0.88, b: 0.38, a: 1 };
rewardHeader.addChild(rewardTitleText);

const rewardSubtitleText = reg(new Node('SubtitleText'));
rewardSubtitleText.anchorMin = { x: 0, y: 0 };
rewardSubtitleText.anchorMax = { x: 1, y: 1 };
rewardSubtitleText.offsetMin = { x: 80, y: 6 };
rewardSubtitleText.offsetMax = { x: -20, y: -36 };
rewardSubtitleText.crId = genId();
rewardSubtitleText.tmpId = genId();
rewardSubtitleText.text = 'An ancient treasure cache has unlocked! Claim seeds, rare crops, and provisions.';
rewardSubtitleText.fontSize = 12.5;
rewardSubtitleText.align = 258;
rewardSubtitleText.color = { r: 0.92, g: 0.88, b: 0.78, a: 1 };
rewardHeader.addChild(rewardSubtitleText);

const rewardBody = reg(new Node('Body'));
rewardBody.anchorMin = { x: 0, y: 0 };
rewardBody.anchorMax = { x: 1, y: 1 };
rewardBody.offsetMin = { x: 16, y: 64 };
rewardBody.offsetMax = { x: -16, y: -84 };
rewardModal.addChild(rewardBody);

// Left Panel (Chest Cache)
const leftChestPanel = reg(new Node('LeftChestPanel'));
leftChestPanel.anchorMin = { x: 0, y: 0 };
leftChestPanel.anchorMax = { x: 0.53, y: 1 };
leftChestPanel.offsetMin = { x: 0, y: 0 };
leftChestPanel.offsetMax = { x: -6, y: 0 };
leftChestPanel.crId = genId();
leftChestPanel.imgId = genId();
leftChestPanel.frameColor = { r: 0.08, g: 0.10, b: 0.14, a: 0.95 };
rewardBody.addChild(leftChestPanel);

const leftChestHeader = reg(new Node('LeftHeader'));
leftChestHeader.anchorMin = { x: 0, y: 1 };
leftChestHeader.anchorMax = { x: 1, y: 1 };
leftChestHeader.pivot = { x: 0.5, y: 1 };
leftChestHeader.sizeDelta = { x: 0, y: 32 };
leftChestHeader.crId = genId();
leftChestHeader.tmpId = genId();
leftChestHeader.text = 'CHEST CACHE CONTENTS';
leftChestHeader.fontSize = 13.5;
leftChestHeader.fontStyle = 1;
leftChestHeader.align = 514;
leftChestHeader.color = { r: 1, g: 0.85, b: 0.3, a: 1 };
leftChestPanel.addChild(leftChestHeader);

const chestScroll = reg(new Node('ChestScrollArea'));
chestScroll.anchorMin = { x: 0, y: 0 };
chestScroll.anchorMax = { x: 1, y: 1 };
chestScroll.offsetMin = { x: 10, y: 10 };
chestScroll.offsetMax = { x: -10, y: -36 };
chestScroll.scrollId = genId();
leftChestPanel.addChild(chestScroll);

const chestVp = reg(new Node('Viewport'));
chestVp.anchorMin = { x: 0, y: 0 };
chestVp.anchorMax = { x: 1, y: 1 };
chestVp.offsetMin = { x: 0, y: 0 };
chestVp.offsetMax = { x: -16, y: 0 };
chestVp.maskId = genId();
chestScroll.addChild(chestVp);

const chestItemsContainer = reg(new Node('ChestItemsContainer'));
chestItemsContainer.anchorMin = { x: 0, y: 1 };
chestItemsContainer.anchorMax = { x: 1, y: 1 };
chestItemsContainer.pivot = { x: 0.5, y: 1 };
chestItemsContainer.sizeDelta = { x: 0, y: 0 };
chestItemsContainer.vlgId = genId();
chestItemsContainer.vlgSpacing = 8;
chestItemsContainer.vlgPad = { l: 4, r: 4, t: 4, b: 4 };
chestItemsContainer.csfId = genId();
chestVp.addChild(chestItemsContainer);

chestScroll.viewportId = chestVp.rtId;
chestScroll.contentId = chestItemsContainer.rtId;
addScrollbar(chestScroll, { r: 0.85, g: 0.65, b: 0.2, a: 1 });

// Right Panel (Backpack)
const rightBpPanel = reg(new Node('RightBackpackPanel'));
rightBpPanel.anchorMin = { x: 0.53, y: 0 };
rightBpPanel.anchorMax = { x: 1, y: 1 };
rightBpPanel.offsetMin = { x: 6, y: 0 };
rightBpPanel.offsetMax = { x: 0, y: 0 };
rightBpPanel.crId = genId();
rightBpPanel.imgId = genId();
rightBpPanel.frameColor = { r: 0.08, g: 0.10, b: 0.14, a: 0.95 };
rewardBody.addChild(rightBpPanel);

const rightBpHeader = reg(new Node('RightHeader'));
rightBpHeader.anchorMin = { x: 0, y: 1 };
rightBpHeader.anchorMax = { x: 1, y: 1 };
rightBpHeader.pivot = { x: 0.5, y: 1 };
rightBpHeader.sizeDelta = { x: 0, y: 32 };
rightBpHeader.crId = genId();
rightBpHeader.tmpId = genId();
rightBpHeader.text = 'YOUR BACKPACK';
rightBpHeader.fontSize = 13.5;
rightBpHeader.fontStyle = 1;
rightBpHeader.align = 514;
rightBpHeader.color = { r: 0.4, g: 0.85, b: 1, a: 1 };
rightBpPanel.addChild(rightBpHeader);

const bpScroll = reg(new Node('BackpackScrollArea'));
bpScroll.anchorMin = { x: 0, y: 0 };
bpScroll.anchorMax = { x: 1, y: 1 };
bpScroll.offsetMin = { x: 10, y: 10 };
bpScroll.offsetMax = { x: -10, y: -36 };
bpScroll.scrollId = genId();
rightBpPanel.addChild(bpScroll);

const bpVp = reg(new Node('Viewport'));
bpVp.anchorMin = { x: 0, y: 0 };
bpVp.anchorMax = { x: 1, y: 1 };
bpVp.offsetMin = { x: 0, y: 0 };
bpVp.offsetMax = { x: -16, y: 0 };
bpVp.maskId = genId();
bpScroll.addChild(bpVp);

const backpackSlotsParent = reg(new Node('BackpackSlots'));
backpackSlotsParent.anchorMin = { x: 0, y: 1 };
backpackSlotsParent.anchorMax = { x: 1, y: 1 };
backpackSlotsParent.pivot = { x: 0.5, y: 1 };
backpackSlotsParent.sizeDelta = { x: 0, y: 0 };
backpackSlotsParent.glgId = genId();
backpackSlotsParent.glgCell = { x: 52, y: 52 };
backpackSlotsParent.glgSpacing = { x: 6, y: 6 };
backpackSlotsParent.glgPad = { l: 4, r: 4, t: 4, b: 4 };
backpackSlotsParent.glgCount = 5;
backpackSlotsParent.csfId = genId();
bpVp.addChild(backpackSlotsParent);

bpScroll.viewportId = bpVp.rtId;
bpScroll.contentId = backpackSlotsParent.rtId;
addScrollbar(bpScroll, { r: 0.25, g: 0.42, b: 0.65, a: 1 });

// Reward Footer
const rewardFooter = reg(new Node('Footer'));
rewardFooter.anchorMin = { x: 0, y: 0 };
rewardFooter.anchorMax = { x: 1, y: 0 };
rewardFooter.pivot = { x: 0.5, y: 0 };
rewardFooter.sizeDelta = { x: 0, y: 60 };
rewardModal.addChild(rewardFooter);

const rewardHintText = reg(new Node('HintText'));
rewardHintText.anchorMin = { x: 0, y: 0 };
rewardHintText.anchorMax = { x: 0.52, y: 1 };
rewardHintText.offsetMin = { x: 18, y: 4 };
rewardHintText.offsetMax = { x: -10, y: -4 };
rewardHintText.crId = genId();
rewardHintText.tmpId = genId();
rewardHintText.text = 'Items collected will be safely stored in your backpack.';
rewardHintText.fontSize = 12;
rewardHintText.align = 258;
rewardHintText.color = { r: 0.85, g: 0.9, b: 0.96, a: 1 };
rewardFooter.addChild(rewardHintText);

const rewardClaimBtn = reg(new Node('BtnLootAll'));
rewardClaimBtn.anchorMin = { x: 0.54, y: 0.5 };
rewardClaimBtn.anchorMax = { x: 0.54, y: 0.5 };
rewardClaimBtn.pivot = { x: 0, y: 0.5 };
rewardClaimBtn.sizeDelta = { x: 180, y: 42 };
rewardClaimBtn.anchoredPosition = { x: 0, y: 0 };
rewardClaimBtn.crId = genId();
rewardClaimBtn.imgId = genId();
rewardClaimBtn.btnId = genId();
rewardClaimBtn.btnColor = { r: 0.18, g: 0.65, b: 0.32, a: 1 };
rewardClaimBtn.targetMethod = 'OnLootAllClicked';
rewardClaimBtn.targetUI = rewardCanvas.uiId;
rewardFooter.addChild(rewardClaimBtn);

const rewardClaimTxt = reg(new Node('Text'));
rewardClaimTxt.anchorMin = { x: 0, y: 0 };
rewardClaimTxt.anchorMax = { x: 1, y: 1 };
rewardClaimTxt.sizeDelta = { x: 0, y: 0 };
rewardClaimTxt.crId = genId();
rewardClaimTxt.tmpId = genId();
rewardClaimTxt.text = 'CLAIM ALL';
rewardClaimTxt.fontSize = 13;
rewardClaimTxt.fontStyle = 1;
rewardClaimTxt.align = 514;
rewardClaimTxt.color = { r: 1, g: 1, b: 1, a: 1 };
rewardClaimBtn.addChild(rewardClaimTxt);

const rewardContBtn = reg(new Node('BtnContinue'));
rewardContBtn.anchorMin = { x: 1, y: 0.5 };
rewardContBtn.anchorMax = { x: 1, y: 0.5 };
rewardContBtn.pivot = { x: 1, y: 0.5 };
rewardContBtn.sizeDelta = { x: 180, y: 42 };
rewardContBtn.anchoredPosition = { x: -18, y: 0 };
rewardContBtn.crId = genId();
rewardContBtn.imgId = genId();
rewardContBtn.btnId = genId();
rewardContBtn.btnColor = { r: 0.2, g: 0.45, b: 0.8, a: 1 };
rewardContBtn.targetMethod = 'OnContinueClicked';
rewardContBtn.targetUI = rewardCanvas.uiId;
rewardFooter.addChild(rewardContBtn);

const rewardContTxt = reg(new Node('Text'));
rewardContTxt.anchorMin = { x: 0, y: 0 };
rewardContTxt.anchorMax = { x: 1, y: 1 };
rewardContTxt.sizeDelta = { x: 0, y: 0 };
rewardContTxt.crId = genId();
rewardContTxt.tmpId = genId();
rewardContTxt.text = 'CONTINUE >>';
rewardContTxt.fontSize = 13;
rewardContTxt.fontStyle = 1;
rewardContTxt.align = 514;
rewardContTxt.color = { r: 1, g: 1, b: 1, a: 1 };
rewardContBtn.addChild(rewardContTxt);

// ==========================================
// 3. BUILD FLOOR VICTORY CANVAS
// ==========================================
const victoryCanvas = reg(new Node('FloorVictoryCanvas', true));
victoryCanvas.canvasId = genId();
victoryCanvas.scalerId = genId();
victoryCanvas.raycasterId = genId();
victoryCanvas.uiId = genId();
victoryCanvas.sortingOrder = 135;

const victoryOverlay = reg(new Node('VictoryPanel'));
victoryOverlay.anchorMin = { x: 0, y: 0 };
victoryOverlay.anchorMax = { x: 1, y: 1 };
victoryOverlay.sizeDelta = { x: 0, y: 0 };
victoryOverlay.isActive = 0;
victoryOverlay.crId = genId();
victoryOverlay.imgId = genId();
victoryOverlay.color = { r: 0.02, g: 0.03, b: 0.05, a: 0.94 };
victoryCanvas.addChild(victoryOverlay);

const victoryFrame = reg(new Node('MainFrame'));
victoryFrame.sizeDelta = { x: 960, y: 580 };
victoryFrame.crId = genId();
victoryFrame.imgId = genId();
victoryFrame.frameColor = { r: 0.14, g: 0.12, b: 0.10, a: 0.98 };
victoryOverlay.addChild(victoryFrame);

const victoryHeader = reg(new Node('Header'));
victoryHeader.anchorMin = { x: 0, y: 1 };
victoryHeader.anchorMax = { x: 1, y: 1 };
victoryHeader.pivot = { x: 0.5, y: 1 };
victoryHeader.anchoredPosition = { x: 0, y: -6 };
victoryHeader.sizeDelta = { x: -16, y: 72 };
victoryHeader.crId = genId();
victoryHeader.imgId = genId();
victoryHeader.isToolbarSprite = true;
victoryHeader.color = { r: 0.22, g: 0.35, b: 0.18, a: 1 };
victoryFrame.addChild(victoryHeader);

const victoryChestIcon = reg(new Node('ChestIcon'));
victoryChestIcon.anchorMin = { x: 0, y: 0.5 };
victoryChestIcon.anchorMax = { x: 0, y: 0.5 };
victoryChestIcon.anchoredPosition = { x: 16, y: 0 };
victoryChestIcon.sizeDelta = { x: 50, y: 50 };
victoryChestIcon.crId = genId();
victoryChestIcon.imgId = genId();
victoryChestIcon.isChestSprite = true;
victoryHeader.addChild(victoryChestIcon);

const victoryTitleText = reg(new Node('TitleText'));
victoryTitleText.anchorMin = { x: 0, y: 1 };
victoryTitleText.anchorMax = { x: 1, y: 1 };
victoryTitleText.pivot = { x: 0, y: 1 };
victoryTitleText.anchoredPosition = { x: 78, y: -10 };
victoryTitleText.sizeDelta = { x: -96, y: 28 };
victoryTitleText.crId = genId();
victoryTitleText.tmpId = genId();
victoryTitleText.text = 'FLOOR CLEARED!';
victoryTitleText.fontSize = 19;
victoryTitleText.fontStyle = 1;
victoryTitleText.align = 258;
victoryTitleText.color = { r: 1, g: 0.88, b: 0.25, a: 1 };
victoryHeader.addChild(victoryTitleText);

const victorySubtitleText = reg(new Node('SubtitleText'));
victorySubtitleText.anchorMin = { x: 0, y: 1 };
victorySubtitleText.anchorMax = { x: 1, y: 1 };
victorySubtitleText.pivot = { x: 0, y: 1 };
victorySubtitleText.anchoredPosition = { x: 78, y: -40 };
victorySubtitleText.sizeDelta = { x: -96, y: 24 };
victorySubtitleText.crId = genId();
victorySubtitleText.tmpId = genId();
victorySubtitleText.text = 'Floor cleared successfully! Claim rewards and blessings below.';
victorySubtitleText.fontSize = 12;
victorySubtitleText.fontStyle = 1;
victorySubtitleText.align = 258;
victorySubtitleText.color = { r: 0.95, g: 0.90, b: 0.75, a: 1 };
victoryHeader.addChild(victorySubtitleText);

const victoryBody = reg(new Node('Body'));
victoryBody.anchorMin = { x: 0, y: 0 };
victoryBody.anchorMax = { x: 1, y: 1 };
victoryBody.offsetMin = { x: 16, y: 68 };
victoryBody.offsetMax = { x: -16, y: -84 };
victoryFrame.addChild(victoryBody);

// Left Column (Overview & Stats/Blessings)
const leftVictoryCol = reg(new Node('LeftColumn_Container'));
leftVictoryCol.anchorMin = { x: 0, y: 0 };
leftVictoryCol.anchorMax = { x: 0.485, y: 1 };
leftVictoryCol.offsetMin = { x: 0, y: 0 };
leftVictoryCol.offsetMax = { x: 0, y: 0 };
leftVictoryCol.crId = genId();
leftVictoryCol.imgId = genId();
leftVictoryCol.frameColor = { r: 0.10, g: 0.11, b: 0.15, a: 0.95 };
victoryBody.addChild(leftVictoryCol);

const leftVictoryScroll = reg(new Node('LeftScrollView'));
leftVictoryScroll.anchorMin = { x: 0, y: 0 };
leftVictoryScroll.anchorMax = { x: 1, y: 1 };
leftVictoryScroll.offsetMin = { x: 6, y: 6 };
leftVictoryScroll.offsetMax = { x: -6, y: -6 };
leftVictoryScroll.scrollId = genId();
leftVictoryCol.addChild(leftVictoryScroll);

const leftVictoryVp = reg(new Node('Viewport'));
leftVictoryVp.anchorMin = { x: 0, y: 0 };
leftVictoryVp.anchorMax = { x: 1, y: 1 };
leftVictoryVp.offsetMin = { x: 0, y: 0 };
leftVictoryVp.offsetMax = { x: -14, y: 0 };
leftVictoryVp.maskId = genId();
leftVictoryScroll.addChild(leftVictoryVp);

const leftVictoryContent = reg(new Node('Content'));
leftVictoryContent.anchorMin = { x: 0, y: 1 };
leftVictoryContent.anchorMax = { x: 1, y: 1 };
leftVictoryContent.pivot = { x: 0.5, y: 1 };
leftVictoryContent.sizeDelta = { x: 0, y: 0 };
leftVictoryContent.vlgId = genId();
leftVictoryContent.vlgSpacing = 10;
leftVictoryContent.vlgPad = { l: 4, r: 4, t: 4, b: 4 };
leftVictoryContent.csfId = genId();
leftVictoryVp.addChild(leftVictoryContent);

leftVictoryScroll.viewportId = leftVictoryVp.rtId;
leftVictoryScroll.contentId = leftVictoryContent.rtId;
addScrollbar(leftVictoryScroll, { r: 0.85, g: 0.65, b: 0.2, a: 1 });

// Card Stats
const cardStats = reg(new Node('Card_Stats'));
cardStats.crId = genId();
cardStats.imgId = genId();
cardStats.frameColor = { r: 0.16, g: 0.13, b: 0.08, a: 0.95 };
cardStats.vlgId = genId();
cardStats.vlgSpacing = 6;
cardStats.vlgPad = { l: 12, r: 12, t: 10, b: 10 };
cardStats.csfId = genId();
leftVictoryContent.addChild(cardStats);

const cardStatsTitle = reg(new Node('Title'));
cardStatsTitle.crId = genId();
cardStatsTitle.tmpId = genId();
cardStatsTitle.text = 'PERMANENT STAT ENHANCEMENT';
cardStatsTitle.fontSize = 13;
cardStatsTitle.fontStyle = 1;
cardStatsTitle.color = { r: 1, g: 0.85, b: 0.3, a: 1 };
cardStats.addChild(cardStatsTitle);

const cardStatsText = reg(new Node('BadgesText'));
cardStatsText.crId = genId();
cardStatsText.tmpId = genId();
cardStatsText.text = '';
cardStatsText.fontSize = 11.5;
cardStatsText.color = { r: 1, g: 1, b: 1, a: 1 };
cardStats.addChild(cardStatsText);

// Card Blessings
const cardBless = reg(new Node('Card_Blessings'));
cardBless.crId = genId();
cardBless.imgId = genId();
cardBless.frameColor = { r: 0.11, g: 0.11, b: 0.19, a: 0.95 };
cardBless.vlgId = genId();
cardBless.vlgSpacing = 6;
cardBless.vlgPad = { l: 12, r: 12, t: 10, b: 10 };
cardBless.csfId = genId();
leftVictoryContent.addChild(cardBless);

const cardBlessTitle = reg(new Node('Title'));
cardBlessTitle.crId = genId();
cardBlessTitle.tmpId = genId();
cardBlessTitle.text = 'ANCIENT BLESSINGS UNLOCKED';
cardBlessTitle.fontSize = 13;
cardBlessTitle.fontStyle = 1;
cardBlessTitle.color = { r: 0.75, g: 0.85, b: 1, a: 1 };
cardBless.addChild(cardBlessTitle);

const cardBlessText = reg(new Node('DetailsText'));
cardBlessText.crId = genId();
cardBlessText.tmpId = genId();
cardBlessText.text = '';
cardBlessText.fontSize = 11.5;
cardBlessText.color = { r: 1, g: 1, b: 1, a: 1 };
cardBless.addChild(cardBlessText);

// Right Column (Treasure Cache)
const rightVictoryCol = reg(new Node('RightColumn_Container'));
rightVictoryCol.anchorMin = { x: 0.505, y: 0 };
rightVictoryCol.anchorMax = { x: 1, y: 1 };
rightVictoryCol.offsetMin = { x: 0, y: 0 };
rightVictoryCol.offsetMax = { x: 0, y: 0 };
rightVictoryCol.crId = genId();
rightVictoryCol.imgId = genId();
rightVictoryCol.frameColor = { r: 0.08, g: 0.09, b: 0.13, a: 0.95 };
victoryBody.addChild(rightVictoryCol);

const rightVictoryTitle = reg(new Node('Title'));
rightVictoryTitle.anchorMin = { x: 0, y: 1 };
rightVictoryTitle.anchorMax = { x: 1, y: 1 };
rightVictoryTitle.pivot = { x: 0.5, y: 1 };
rightVictoryTitle.sizeDelta = { x: -20, y: 26 };
rightVictoryTitle.anchoredPosition = { x: 0, y: -6 };
rightVictoryTitle.crId = genId();
rightVictoryTitle.tmpId = genId();
rightVictoryTitle.text = 'EXPEDITION GOLDEN TREASURE';
rightVictoryTitle.fontSize = 12.5;
rightVictoryTitle.fontStyle = 1;
rightVictoryTitle.color = { r: 1, g: 0.85, b: 0.3, a: 1 };
rightVictoryCol.addChild(rightVictoryTitle);

const rightVictoryScroll = reg(new Node('ScrollArea'));
rightVictoryScroll.anchorMin = { x: 0, y: 0 };
rightVictoryScroll.anchorMax = { x: 1, y: 1 };
rightVictoryScroll.offsetMin = { x: 8, y: 8 };
rightVictoryScroll.offsetMax = { x: -8, y: -36 };
rightVictoryScroll.scrollId = genId();
rightVictoryCol.addChild(rightVictoryScroll);

const rightVictoryVp = reg(new Node('Viewport'));
rightVictoryVp.anchorMin = { x: 0, y: 0 };
rightVictoryVp.anchorMax = { x: 1, y: 1 };
rightVictoryVp.offsetMin = { x: 0, y: 0 };
rightVictoryVp.offsetMax = { x: -14, y: 0 };
rightVictoryVp.maskId = genId();
rightVictoryScroll.addChild(rightVictoryVp);

const rightVictoryContent = reg(new Node('Content'));
rightVictoryContent.anchorMin = { x: 0, y: 1 };
rightVictoryContent.anchorMax = { x: 1, y: 1 };
rightVictoryContent.pivot = { x: 0.5, y: 1 };
rightVictoryContent.sizeDelta = { x: 0, y: 0 };
rightVictoryContent.vlgId = genId();
rightVictoryContent.vlgSpacing = 6;
rightVictoryContent.vlgPad = { l: 4, r: 4, t: 4, b: 4 };
rightVictoryContent.csfId = genId();
rightVictoryVp.addChild(rightVictoryContent);

rightVictoryScroll.viewportId = rightVictoryVp.rtId;
rightVictoryScroll.contentId = rightVictoryContent.rtId;
addScrollbar(rightVictoryScroll, { r: 0.85, g: 0.65, b: 0.15, a: 1 });

// Victory Footer
const victoryFooter = reg(new Node('Footer'));
victoryFooter.anchorMin = { x: 0, y: 0 };
victoryFooter.anchorMax = { x: 1, y: 0 };
victoryFooter.pivot = { x: 0.5, y: 0 };
victoryFooter.anchoredPosition = { x: 0, y: 6 };
victoryFooter.sizeDelta = { x: -16, y: 56 };
victoryFooter.crId = genId();
victoryFooter.imgId = genId();
victoryFooter.frameColor = { r: 0.10, g: 0.10, b: 0.12, a: 0.95 };
victoryFrame.addChild(victoryFooter);

const victoryHintText = reg(new Node('HintText'));
victoryHintText.anchorMin = { x: 0, y: 0 };
victoryHintText.anchorMax = { x: 0, y: 1 };
victoryHintText.pivot = { x: 0, y: 0.5 };
victoryHintText.anchoredPosition = { x: 16, y: 0 };
victoryHintText.sizeDelta = { x: 430, y: 48 };
victoryHintText.crId = genId();
victoryHintText.tmpId = genId();
victoryHintText.text = '';
victoryHintText.fontSize = 11.5;
victoryHintText.align = 258;
victoryHintText.color = { r: 0.85, g: 0.92, b: 1, a: 1 };
victoryFooter.addChild(victoryHintText);

const victoryBtnRow = reg(new Node('ButtonsRow'));
victoryBtnRow.anchorMin = { x: 1, y: 0.5 };
victoryBtnRow.anchorMax = { x: 1, y: 0.5 };
victoryBtnRow.pivot = { x: 1, y: 0.5 };
victoryBtnRow.anchoredPosition = { x: -16, y: 0 };
victoryBtnRow.sizeDelta = { x: 480, y: 40 };
victoryBtnRow.hlgId = genId();
victoryBtnRow.hlgSpacing = 10;
victoryFooter.addChild(victoryBtnRow);

// BtnLootAll
const vLootAllBtn = reg(new Node('BtnLootAll'));
vLootAllBtn.sizeDelta = { x: 145, y: 38 };
vLootAllBtn.crId = genId();
vLootAllBtn.imgId = genId();
vLootAllBtn.btnId = genId();
vLootAllBtn.btnColor = { r: 0.85, g: 0.65, b: 0.15, a: 1 };
vLootAllBtn.targetMethod = 'OnLootAllClicked';
vLootAllBtn.targetUI = victoryCanvas.uiId;
victoryBtnRow.addChild(vLootAllBtn);

const vLootAllTxt = reg(new Node('Text'));
vLootAllTxt.anchorMin = { x: 0, y: 0 };
vLootAllTxt.anchorMax = { x: 1, y: 1 };
vLootAllTxt.sizeDelta = { x: 0, y: 0 };
vLootAllTxt.crId = genId();
vLootAllTxt.tmpId = genId();
vLootAllTxt.text = 'CLAIM ALL';
vLootAllTxt.fontSize = 13;
vLootAllTxt.fontStyle = 1;
vLootAllTxt.align = 514;
vLootAllTxt.color = { r: 1, g: 1, b: 1, a: 1 };
vLootAllBtn.addChild(vLootAllTxt);

// BtnReturn
const vReturnBtn = reg(new Node('BtnReturn'));
vReturnBtn.sizeDelta = { x: 145, y: 38 };
vReturnBtn.crId = genId();
vReturnBtn.imgId = genId();
vReturnBtn.btnId = genId();
vReturnBtn.btnColor = { r: 0.18, g: 0.55, b: 0.28, a: 1 };
vReturnBtn.targetMethod = 'OnReturnToBaseClicked';
vReturnBtn.targetUI = victoryCanvas.uiId;
victoryBtnRow.addChild(vReturnBtn);

const vReturnTxt = reg(new Node('Text'));
vReturnTxt.anchorMin = { x: 0, y: 0 };
vReturnTxt.anchorMax = { x: 1, y: 1 };
vReturnTxt.sizeDelta = { x: 0, y: 0 };
vReturnTxt.crId = genId();
vReturnTxt.tmpId = genId();
vReturnTxt.text = 'RETURN TO FARM';
vReturnTxt.fontSize = 13;
vReturnTxt.fontStyle = 1;
vReturnTxt.align = 514;
vReturnTxt.color = { r: 1, g: 1, b: 1, a: 1 };
vReturnBtn.addChild(vReturnTxt);

// BtnNextFloor
const vNextBtn = reg(new Node('BtnNextFloor'));
vNextBtn.sizeDelta = { x: 165, y: 38 };
vNextBtn.crId = genId();
vNextBtn.imgId = genId();
vNextBtn.btnId = genId();
vNextBtn.btnColor = { r: 0.25, g: 0.45, b: 0.85, a: 1 };
vNextBtn.targetMethod = 'OnNextFloorClicked';
vNextBtn.targetUI = victoryCanvas.uiId;
victoryBtnRow.addChild(vNextBtn);

const vNextTxt = reg(new Node('Text'));
vNextTxt.anchorMin = { x: 0, y: 0 };
vNextTxt.anchorMax = { x: 1, y: 1 };
vNextTxt.sizeDelta = { x: 0, y: 0 };
vNextTxt.crId = genId();
vNextTxt.tmpId = genId();
vNextTxt.text = 'NEXT FLOOR [->]';
vNextTxt.fontSize = 13;
vNextTxt.fontStyle = 1;
vNextTxt.align = 514;
vNextTxt.color = { r: 1, g: 1, b: 1, a: 1 };
vNextBtn.addChild(vNextTxt);

// ==========================================
// 4. BUILD DUNGEON SUMMARY CANVAS
// ==========================================
const summaryCanvas = reg(new Node('DungeonSummaryCanvas', true));
summaryCanvas.canvasId = genId();
summaryCanvas.scalerId = genId();
summaryCanvas.raycasterId = genId();
summaryCanvas.uiId = genId();
summaryCanvas.sortingOrder = 140;

const summaryOverlay = reg(new Node('DungeonSummaryPanel'));
summaryOverlay.anchorMin = { x: 0, y: 0 };
summaryOverlay.anchorMax = { x: 1, y: 1 };
summaryOverlay.sizeDelta = { x: 0, y: 0 };
summaryOverlay.isActive = 0;
summaryOverlay.crId = genId();
summaryOverlay.imgId = genId();
summaryOverlay.color = { r: 0.02, g: 0.03, b: 0.06, a: 0.98 };
summaryCanvas.addChild(summaryOverlay);

const summaryModal = reg(new Node('SummaryModal'));
summaryModal.sizeDelta = { x: 920, y: 600 };
summaryModal.crId = genId();
summaryModal.imgId = genId();
summaryModal.frameColor = { r: 0.14, g: 0.12, b: 0.10, a: 0.98 };
summaryOverlay.addChild(summaryModal);

const summaryHeader = reg(new Node('Header'));
summaryHeader.anchorMin = { x: 0, y: 1 };
summaryHeader.anchorMax = { x: 1, y: 1 };
summaryHeader.pivot = { x: 0.5, y: 1 };
summaryHeader.anchoredPosition = { x: 0, y: -6 };
summaryHeader.sizeDelta = { x: -12, y: 74 };
summaryHeader.crId = genId();
summaryHeader.imgId = genId();
summaryHeader.isToolbarSprite = true;
summaryHeader.color = { r: 0.24, g: 0.18, b: 0.10, a: 1 };
summaryModal.addChild(summaryHeader);

const summaryHeaderIcon = reg(new Node('HeaderIcon'));
summaryHeaderIcon.anchorMin = { x: 0, y: 0.5 };
summaryHeaderIcon.anchorMax = { x: 0, y: 0.5 };
summaryHeaderIcon.pivot = { x: 0, y: 0.5 };
summaryHeaderIcon.anchoredPosition = { x: 24, y: 0 };
summaryHeaderIcon.sizeDelta = { x: 50, y: 50 };
summaryHeaderIcon.crId = genId();
summaryHeaderIcon.imgId = genId();
summaryHeaderIcon.isChestSprite = true;
summaryHeader.addChild(summaryHeaderIcon);

const summaryTitleText = reg(new Node('TitleText'));
summaryTitleText.anchorMin = { x: 0, y: 0 };
summaryTitleText.anchorMax = { x: 1, y: 1 };
summaryTitleText.offsetMin = { x: 86, y: 24 };
summaryTitleText.offsetMax = { x: -20, y: -6 };
summaryTitleText.crId = genId();
summaryTitleText.tmpId = genId();
summaryTitleText.text = 'DUNGEON EXPEDITION SUMMARY';
summaryTitleText.fontSize = 20;
summaryTitleText.fontStyle = 1;
summaryTitleText.align = 258;
summaryTitleText.color = { r: 1, g: 1, b: 1, a: 1 };
summaryHeader.addChild(summaryTitleText);

const summarySubtitleText = reg(new Node('SubtitleText'));
summarySubtitleText.anchorMin = { x: 0, y: 0 };
summarySubtitleText.anchorMax = { x: 1, y: 1 };
summarySubtitleText.offsetMin = { x: 86, y: 6 };
summarySubtitleText.offsetMax = { x: -20, y: -36 };
summarySubtitleText.crId = genId();
summarySubtitleText.tmpId = genId();
summarySubtitleText.text = 'Overview of progress, stages cleared, and all secured expedition loot.';
summarySubtitleText.fontSize = 12;
summarySubtitleText.align = 258;
summarySubtitleText.color = { r: 0.85, g: 0.88, b: 0.95, a: 1 };
summaryHeader.addChild(summarySubtitleText);

const summaryBody = reg(new Node('Body'));
summaryBody.anchorMin = { x: 0, y: 0 };
summaryBody.anchorMax = { x: 1, y: 1 };
summaryBody.offsetMin = { x: 14, y: 68 };
summaryBody.offsetMax = { x: -14, y: -84 };
summaryModal.addChild(summaryBody);

// Left Column: Stats
const leftSummaryCol = reg(new Node('LeftColumn_Stats'));
leftSummaryCol.anchorMin = { x: 0, y: 0 };
leftSummaryCol.anchorMax = { x: 0.42, y: 1 };
leftSummaryCol.offsetMin = { x: 0, y: 0 };
leftSummaryCol.offsetMax = { x: -6, y: 0 };
leftSummaryCol.crId = genId();
leftSummaryCol.imgId = genId();
leftSummaryCol.frameColor = { r: 0.12, g: 0.14, b: 0.18, a: 0.98 };
summaryBody.addChild(leftSummaryCol);

const leftSummaryHeader = reg(new Node('LeftHeader'));
leftSummaryHeader.anchorMin = { x: 0, y: 1 };
leftSummaryHeader.anchorMax = { x: 1, y: 1 };
leftSummaryHeader.pivot = { x: 0.5, y: 1 };
leftSummaryHeader.sizeDelta = { x: -12, y: 30 };
leftSummaryHeader.anchoredPosition = { x: 0, y: -6 };
leftSummaryHeader.crId = genId();
leftSummaryHeader.imgId = genId();
leftSummaryHeader.isToolbarSprite = true;
leftSummaryHeader.color = { r: 0.25, g: 0.20, b: 0.15, a: 1 };
leftSummaryCol.addChild(leftSummaryHeader);

const leftSummaryHeaderTxt = reg(new Node('Text'));
leftSummaryHeaderTxt.anchorMin = { x: 0, y: 0 };
leftSummaryHeaderTxt.anchorMax = { x: 1, y: 1 };
leftSummaryHeaderTxt.offsetMin = { x: 8, y: 0 };
leftSummaryHeaderTxt.offsetMax = { x: -8, y: 0 };
leftSummaryHeaderTxt.crId = genId();
leftSummaryHeaderTxt.tmpId = genId();
leftSummaryHeaderTxt.text = 'EXPEDITION STATS';
leftSummaryHeaderTxt.fontSize = 12.5;
leftSummaryHeaderTxt.fontStyle = 1;
leftSummaryHeaderTxt.align = 258;
leftSummaryHeaderTxt.color = { r: 1, g: 0.85, b: 0.35, a: 1 };
leftSummaryHeader.addChild(leftSummaryHeaderTxt);

const statsList = reg(new Node('StatsList'));
statsList.anchorMin = { x: 0, y: 0 };
statsList.anchorMax = { x: 1, y: 1 };
statsList.offsetMin = { x: 10, y: 10 };
statsList.offsetMax = { x: -10, y: -42 };
statsList.vlgId = genId();
statsList.vlgSpacing = 8;
leftSummaryCol.addChild(statsList);

function createSummaryStatItem(name, defaultText) {
  const item = reg(new Node(name));
  item.anchorMin = { x: 0, y: 0.5 };
  item.anchorMax = { x: 1, y: 0.5 };
  item.sizeDelta = { x: 0, y: 32 };
  item.crId = genId();
  item.imgId = genId();
  item.frameColor = { r: 0.10, g: 0.13, b: 0.19, a: 0.95 };
  statsList.addChild(item);

  const tNode = reg(new Node('Text'));
  tNode.anchorMin = { x: 0, y: 0 };
  tNode.anchorMax = { x: 1, y: 1 };
  tNode.offsetMin = { x: 10, y: 0 };
  tNode.offsetMax = { x: -10, y: 0 };
  tNode.crId = genId();
  tNode.tmpId = genId();
  tNode.text = defaultText;
  tNode.fontSize = 12;
  tNode.align = 258;
  tNode.color = { r: 1, g: 1, b: 1, a: 1 };
  item.addChild(tNode);
  return tNode;
}

const sFloorTxt = createSummaryStatItem('StatFloor', '[Floor] 1');
const sStagesTxt = createSummaryStatItem('StatStages', '[Stages] 0/5');
const sBossTxt = createSummaryStatItem('StatBoss', '[Boss] Unmet');
const sHealthTxt = createSummaryStatItem('StatHealth', '[Vitality] 100/100 HP');
const sBlessingTxt = createSummaryStatItem('StatBlessing', '[Blessing] None');
const sDayTxt = createSummaryStatItem('StatDay', '[Time] New Day (+1 Day)');

const sNoticeBox = reg(new Node('NoticeBox'));
sNoticeBox.anchorMin = { x: 0, y: 0.5 };
sNoticeBox.anchorMax = { x: 1, y: 0.5 };
sNoticeBox.sizeDelta = { x: 0, y: 110 };
sNoticeBox.crId = genId();
sNoticeBox.imgId = genId();
sNoticeBox.frameColor = { r: 0.14, g: 0.12, b: 0.10, a: 0.98 };
statsList.addChild(sNoticeBox);

const sNoticeTxt = reg(new Node('NoticeText'));
sNoticeTxt.anchorMin = { x: 0, y: 0 };
sNoticeTxt.anchorMax = { x: 1, y: 1 };
sNoticeTxt.offsetMin = { x: 10, y: 8 };
sNoticeTxt.offsetMax = { x: -10, y: -8 };
sNoticeTxt.crId = genId();
sNoticeTxt.tmpId = genId();
sNoticeTxt.text = '';
sNoticeTxt.fontSize = 11.5;
sNoticeTxt.align = 257; // TopLeft
sNoticeTxt.color = { r: 1, g: 1, b: 1, a: 1 };
sNoticeBox.addChild(sNoticeTxt);

// Right Column: Loot
const rightSummaryCol = reg(new Node('RightColumn_Loot'));
rightSummaryCol.anchorMin = { x: 0.42, y: 0 };
rightSummaryCol.anchorMax = { x: 1, y: 1 };
rightSummaryCol.offsetMin = { x: 6, y: 0 };
rightSummaryCol.offsetMax = { x: 0, y: 0 };
rightSummaryCol.crId = genId();
rightSummaryCol.imgId = genId();
rightSummaryCol.frameColor = { r: 0.12, g: 0.14, b: 0.18, a: 0.98 };
summaryBody.addChild(rightSummaryCol);

const rightSummaryHeader = reg(new Node('RightHeader'));
rightSummaryHeader.anchorMin = { x: 0, y: 1 };
rightSummaryHeader.anchorMax = { x: 1, y: 1 };
rightSummaryHeader.pivot = { x: 0.5, y: 1 };
rightSummaryHeader.sizeDelta = { x: -12, y: 30 };
rightSummaryHeader.anchoredPosition = { x: 0, y: -6 };
rightSummaryHeader.crId = genId();
rightSummaryHeader.imgId = genId();
rightSummaryHeader.frameColor = { r: 0.18, g: 0.22, b: 0.30, a: 1 };
rightSummaryCol.addChild(rightSummaryHeader);

const rightSummaryHeaderTxt = reg(new Node('Text'));
rightSummaryHeaderTxt.anchorMin = { x: 0, y: 0 };
rightSummaryHeaderTxt.anchorMax = { x: 1, y: 1 };
rightSummaryHeaderTxt.offsetMin = { x: 8, y: 0 };
rightSummaryHeaderTxt.offsetMax = { x: -8, y: 0 };
rightSummaryHeaderTxt.crId = genId();
rightSummaryHeaderTxt.tmpId = genId();
rightSummaryHeaderTxt.text = 'SAFELY SECURED LOOT';
rightSummaryHeaderTxt.fontSize = 12.5;
rightSummaryHeaderTxt.fontStyle = 1;
rightSummaryHeaderTxt.align = 258;
rightSummaryHeaderTxt.color = { r: 0.4, g: 0.85, b: 1, a: 1 };
rightSummaryHeader.addChild(rightSummaryHeaderTxt);

const summaryScroll = reg(new Node('LootScrollArea'));
summaryScroll.anchorMin = { x: 0, y: 0 };
summaryScroll.anchorMax = { x: 1, y: 1 };
summaryScroll.offsetMin = { x: 8, y: 8 };
summaryScroll.offsetMax = { x: -8, y: -42 };
summaryScroll.scrollId = genId();
rightSummaryCol.addChild(summaryScroll);

const summaryVp = reg(new Node('Viewport'));
summaryVp.anchorMin = { x: 0, y: 0 };
summaryVp.anchorMax = { x: 1, y: 1 };
summaryVp.offsetMin = { x: 0, y: 0 };
summaryVp.offsetMax = { x: -16, y: 0 };
summaryVp.maskId = genId();
summaryScroll.addChild(summaryVp);

const summaryContent = reg(new Node('Content'));
summaryContent.anchorMin = { x: 0, y: 1 };
summaryContent.anchorMax = { x: 1, y: 1 };
summaryContent.pivot = { x: 0.5, y: 1 };
summaryContent.sizeDelta = { x: 0, y: 0 };
summaryContent.glgId = genId();
summaryContent.glgCell = { x: 230, y: 52 };
summaryContent.glgSpacing = { x: 8, y: 6 };
summaryContent.glgPad = { l: 6, r: 6, t: 6, b: 6 };
summaryContent.glgCount = 2;
summaryContent.csfId = genId();
summaryVp.addChild(summaryContent);

summaryScroll.viewportId = summaryVp.rtId;
summaryScroll.contentId = summaryContent.rtId;
addScrollbar(summaryScroll, { r: 0.25, g: 0.75, b: 0.85, a: 1 });

// Summary Footer
const summaryFooter = reg(new Node('Footer'));
summaryFooter.anchorMin = { x: 0, y: 0 };
summaryFooter.anchorMax = { x: 1, y: 0 };
summaryFooter.pivot = { x: 0.5, y: 0 };
summaryFooter.anchoredPosition = { x: 0, y: 14 };
summaryFooter.sizeDelta = { x: -28, y: 44 };
summaryModal.addChild(summaryFooter);

const summaryReturnBtn = reg(new Node('BtnReturnToBase'));
summaryReturnBtn.anchorMin = { x: 1, y: 0.5 };
summaryReturnBtn.anchorMax = { x: 1, y: 0.5 };
summaryReturnBtn.pivot = { x: 1, y: 0.5 };
summaryReturnBtn.anchoredPosition = { x: 0, y: 0 };
summaryReturnBtn.sizeDelta = { x: 260, y: 44 };
summaryReturnBtn.crId = genId();
summaryReturnBtn.imgId = genId();
summaryReturnBtn.btnId = genId();
summaryReturnBtn.btnColor = { r: 0.18, g: 0.55, b: 0.28, a: 1 };
summaryReturnBtn.targetMethod = 'OnReturnClicked';
summaryReturnBtn.targetUI = summaryCanvas.uiId;
summaryFooter.addChild(summaryReturnBtn);

const summaryReturnTxt = reg(new Node('Text'));
summaryReturnTxt.anchorMin = { x: 0, y: 0 };
summaryReturnTxt.anchorMax = { x: 1, y: 1 };
summaryReturnTxt.sizeDelta = { x: 0, y: 0 };
summaryReturnTxt.crId = genId();
summaryReturnTxt.tmpId = genId();
summaryReturnTxt.text = 'RETURN TO FARM  [ -> ]';
summaryReturnTxt.fontSize = 14;
summaryReturnTxt.fontStyle = 1;
summaryReturnTxt.align = 514;
summaryReturnTxt.color = { r: 1, g: 1, b: 1, a: 1 };
summaryReturnBtn.addChild(summaryReturnTxt);


// ==========================================
// YAML EMISSION
// ==========================================
let yaml = '';

for (const n of allNodes) {
  // 1. GameObject
  yaml += `--- !u!1 &${n.goId}\n`;
  yaml += `GameObject:\n`;
  yaml += `  m_ObjectHideFlags: 0\n`;
  yaml += `  m_CorrespondingSourceObject: {fileID: 0}\n`;
  yaml += `  m_PrefabInstance: {fileID: 0}\n`;
  yaml += `  m_PrefabAsset: {fileID: 0}\n`;
  yaml += `  serializedVersion: 6\n`;
  yaml += `  m_Component:\n`;
  yaml += `  - component: {fileID: ${n.rtId}}\n`;
  if (n.canvasId) yaml += `  - component: {fileID: ${n.canvasId}}\n`;
  if (n.scalerId) yaml += `  - component: {fileID: ${n.scalerId}}\n`;
  if (n.raycasterId) yaml += `  - component: {fileID: ${n.raycasterId}}\n`;
  if (n.uiId) yaml += `  - component: {fileID: ${n.uiId}}\n`;
  if (n.crId) yaml += `  - component: {fileID: ${n.crId}}\n`;
  if (n.imgId) yaml += `  - component: {fileID: ${n.imgId}}\n`;
  if (n.tmpId) yaml += `  - component: {fileID: ${n.tmpId}}\n`;
  if (n.btnId) yaml += `  - component: {fileID: ${n.btnId}}\n`;
  if (n.scrollId) yaml += `  - component: {fileID: ${n.scrollId}}\n`;
  if (n.maskId) yaml += `  - component: {fileID: ${n.maskId}}\n`;
  if (n.vlgId) yaml += `  - component: {fileID: ${n.vlgId}}\n`;
  if (n.hlgId) yaml += `  - component: {fileID: ${n.hlgId}}\n`;
  if (n.glgId) yaml += `  - component: {fileID: ${n.glgId}}\n`;
  if (n.csfId) yaml += `  - component: {fileID: ${n.csfId}}\n`;
  if (n.sbId) yaml += `  - component: {fileID: ${n.sbId}}\n`;
  yaml += `  m_Layer: ${n.layer}\n`;
  yaml += `  m_Name: ${n.name}\n`;
  yaml += `  m_TagString: Untagged\n`;
  yaml += `  m_Icon: {fileID: 0}\n`;
  yaml += `  m_NavMeshLayer: 0\n`;
  yaml += `  m_StaticEditorFlags: 0\n`;
  yaml += `  m_IsActive: ${n.isActive}\n`;

  // 2. RectTransform
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

  yaml += `--- !u!224 &${n.rtId}\n`;
  yaml += `RectTransform:\n`;
  yaml += `  m_ObjectHideFlags: 0\n`;
  yaml += `  m_CorrespondingSourceObject: {fileID: 0}\n`;
  yaml += `  m_PrefabInstance: {fileID: 0}\n`;
  yaml += `  m_PrefabAsset: {fileID: 0}\n`;
  yaml += `  m_GameObject: {fileID: ${n.goId}}\n`;
  yaml += `  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}\n`;
  yaml += `  m_LocalPosition: {x: 0, y: 0, z: 0}\n`;
  yaml += `  m_LocalScale: {x: ${n.localScale ? n.localScale.x : 1}, y: ${n.localScale ? n.localScale.y : 1}, z: ${n.localScale ? n.localScale.z : 1}}\n`;
  yaml += `  m_ConstrainProportionsScale: 0\n`;
  yaml += `  m_Children:\n`;
  for (const c of n.children) {
    yaml += `  - {fileID: ${c.rtId}}\n`;
  }
  yaml += `  m_Father: {fileID: ${n.father ? n.father.rtId : 0}}\n`;
  yaml += `  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\n`;
  yaml += `  m_AnchorMin: {x: ${n.anchorMin.x}, y: ${n.anchorMin.y}}\n`;
  yaml += `  m_AnchorMax: {x: ${n.anchorMax.x}, y: ${n.anchorMax.y}}\n`;
  yaml += `  m_AnchoredPosition: {x: ${n.anchoredPosition.x}, y: ${n.anchoredPosition.y}}\n`;
  yaml += `  m_SizeDelta: {x: ${n.sizeDelta.x}, y: ${n.sizeDelta.y}}\n`;
  yaml += `  m_Pivot: {x: ${n.pivot.x}, y: ${n.pivot.y}}\n`;

  // 3. Canvas Components (for Root)
  if (n.canvasId) {
    yaml += `--- !u!223 &${n.canvasId}\n`;
    yaml += `Canvas:\n`;
    yaml += `  m_ObjectHideFlags: 0\n`;
    yaml += `  m_CorrespondingSourceObject: {fileID: 0}\n`;
    yaml += `  m_PrefabInstance: {fileID: 0}\n`;
    yaml += `  m_PrefabAsset: {fileID: 0}\n`;
    yaml += `  m_GameObject: {fileID: ${n.goId}}\n`;
    yaml += `  m_Enabled: 1\n`;
    yaml += `  serializedVersion: 3\n`;
    yaml += `  m_RenderMode: 0\n`;
    yaml += `  m_Camera: {fileID: 0}\n`;
    yaml += `  m_PlaneDistance: 100\n`;
    yaml += `  m_PixelPerfect: 0\n`;
    yaml += `  m_ReceivesEvents: 1\n`;
    yaml += `  m_OverrideSorting: 0\n`;
    yaml += `  m_OverridePixelPerfect: 0\n`;
    yaml += `  m_SortingBucketNormalizedSize: 0\n`;
    yaml += `  m_VertexColorAlwaysGammaSpace: 0\n`;
    yaml += `  m_AdditionalShaderChannelsFlag: 25\n`;
    yaml += `  m_UpdateRectTransformForStandalone: 0\n`;
    yaml += `  m_SortingLayerID: 0\n`;
    yaml += `  m_SortingOrder: ${n.sortingOrder}\n`;
    yaml += `  m_TargetDisplay: 0\n`;

    yaml += `--- !u!114 &${n.scalerId}\n`;
    yaml += `MonoBehaviour:\n`;
    yaml += `  m_ObjectHideFlags: 0\n`;
    yaml += `  m_CorrespondingSourceObject: {fileID: 0}\n`;
    yaml += `  m_PrefabInstance: {fileID: 0}\n`;
    yaml += `  m_PrefabAsset: {fileID: 0}\n`;
    yaml += `  m_GameObject: {fileID: ${n.goId}}\n`;
    yaml += `  m_Enabled: 1\n`;
    yaml += `  m_EditorHideFlags: 0\n`;
    yaml += `  m_Script: {fileID: 11500000, guid: ${GUID_CANVAS_SCALER}, type: 3}\n`;
    yaml += `  m_Name: \n`;
    yaml += `  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.CanvasScaler\n`;
    yaml += `  m_UiScaleMode: 1\n`;
    yaml += `  m_ReferencePixelsPerUnit: 100\n`;
    yaml += `  m_ScaleFactor: 1\n`;
    yaml += `  m_ReferenceResolution: {x: 1920, y: 1080}\n`;
    yaml += `  m_ScreenMatchMode: 0\n`;
    yaml += `  m_MatchWidthOrHeight: 0.5\n`;
    yaml += `  m_PhysicalUnit: 3\n`;
    yaml += `  m_FallbackScreenDPI: 96\n`;
    yaml += `  m_DefaultSpriteDPI: 96\n`;
    yaml += `  m_DynamicPixelsPerUnit: 1\n`;
    yaml += `  m_PresetInfoIsWorld: 0\n`;

    yaml += `--- !u!114 &${n.raycasterId}\n`;
    yaml += `MonoBehaviour:\n`;
    yaml += `  m_ObjectHideFlags: 0\n`;
    yaml += `  m_CorrespondingSourceObject: {fileID: 0}\n`;
    yaml += `  m_PrefabInstance: {fileID: 0}\n`;
    yaml += `  m_PrefabAsset: {fileID: 0}\n`;
    yaml += `  m_GameObject: {fileID: ${n.goId}}\n`;
    yaml += `  m_Enabled: 1\n`;
    yaml += `  m_EditorHideFlags: 0\n`;
    yaml += `  m_Script: {fileID: 11500000, guid: ${GUID_GRAPHIC_RAYCASTER}, type: 3}\n`;
    yaml += `  m_Name: \n`;
    yaml += `  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.GraphicRaycaster\n`;
    yaml += `  m_IgnoreReversedGraphics: 1\n`;
    yaml += `  m_BlockingObjects: 0\n`;
    yaml += `  m_BlockingMask:\n`;
    yaml += `    serializedVersion: 2\n`;
    yaml += `    m_Bits: 4294967295\n`;
  }

  // 4. UI Manager Components
  if (n === lootCanvas) {
    yaml += `--- !u!114 &${n.uiId}\n`;
    yaml += `MonoBehaviour:\n`;
    yaml += `  m_ObjectHideFlags: 0\n`;
    yaml += `  m_CorrespondingSourceObject: {fileID: 0}\n`;
    yaml += `  m_PrefabInstance: {fileID: 0}\n`;
    yaml += `  m_PrefabAsset: {fileID: 0}\n`;
    yaml += `  m_GameObject: {fileID: ${n.goId}}\n`;
    yaml += `  m_Enabled: 1\n`;
    yaml += `  m_EditorHideFlags: 0\n`;
    yaml += `  m_Script: {fileID: 11500000, guid: ${GUID_LOOT_UI}, type: 3}\n`;
    yaml += `  m_Name: \n`;
    yaml += `  m_EditorClassIdentifier: Assembly-CSharp::LootUI\n`;
    yaml += `  lootPanel: {fileID: ${lootOverlay.goId}}\n`;
    yaml += `  lootSlotsParent: {fileID: ${leftLootContent.rtId}}\n`;
    yaml += `  backpackSlotsParent: {fileID: ${rightLootContent.rtId}}\n`;
    yaml += `  lootScrollRect: {fileID: ${leftLootScroll.scrollId}}\n`;
    yaml += `  bpScrollRect: {fileID: ${rightLootScroll.scrollId}}\n`;
    yaml += `  canvas: {fileID: ${lootCanvas.canvasId}}\n`;
    yaml += `  slotPrefab: {fileID: ${FILEID_SLOT_PREFAB}, guid: ${GUID_SLOT_PREFAB}, type: 3}\n`;
    yaml += `  lootAllButton: {fileID: ${lootAllBtn.btnId}}\n`;
    yaml += `  continueButton: {fileID: ${lootContBtn.btnId}}\n`;
    yaml += `  lootAllBtnImg: {fileID: ${lootAllBtn.imgId}}\n`;
    yaml += `  contBtnImg: {fileID: ${lootContBtn.imgId}}\n`;
    yaml += `  lootAllTxt: {fileID: ${lootAllTxt.tmpId}}\n`;
    yaml += `  contTxt: {fileID: ${lootContTxt.tmpId}}\n`;
    yaml += `  titleText: {fileID: ${lootTitleText.tmpId}}\n`;
    yaml += `  subtitleText: {fileID: ${lootSubtitleText.tmpId}}\n`;
    yaml += `  hintText: {fileID: ${lootHintText.tmpId}}\n`;
    yaml += `  lootHeaderTitle: {fileID: ${leftLootLabel.tmpId}}\n`;
    yaml += `  backpackHeaderTitle: {fileID: ${rightLootLabel.tmpId}}\n`;
    yaml += `  emptyLootNoticeGO: {fileID: 0}\n`;
  } else if (n === rewardCanvas) {
    yaml += `--- !u!114 &${n.uiId}\n`;
    yaml += `MonoBehaviour:\n`;
    yaml += `  m_ObjectHideFlags: 0\n`;
    yaml += `  m_CorrespondingSourceObject: {fileID: 0}\n`;
    yaml += `  m_PrefabInstance: {fileID: 0}\n`;
    yaml += `  m_PrefabAsset: {fileID: 0}\n`;
    yaml += `  m_GameObject: {fileID: ${n.goId}}\n`;
    yaml += `  m_Enabled: 1\n`;
    yaml += `  m_EditorHideFlags: 0\n`;
    yaml += `  m_Script: {fileID: 11500000, guid: ${GUID_REWARD_UI}, type: 3}\n`;
    yaml += `  m_Name: \n`;
    yaml += `  m_EditorClassIdentifier: Assembly-CSharp::RewardUI\n`;
    yaml += `  rewardPanel: {fileID: ${rewardOverlay.goId}}\n`;
    yaml += `  chestItemsContainer: {fileID: ${chestItemsContainer.rtId}}\n`;
    yaml += `  backpackSlotsParent: {fileID: ${backpackSlotsParent.rtId}}\n`;
    yaml += `  canvas: {fileID: ${rewardCanvas.canvasId}}\n`;
    yaml += `  slotPrefab: {fileID: ${FILEID_SLOT_PREFAB}, guid: ${GUID_SLOT_PREFAB}, type: 3}\n`;
    yaml += `  chestHeaderIcon: {fileID: ${rewardChestIcon.imgId}}\n`;
    yaml += `  titleText: {fileID: ${rewardTitleText.tmpId}}\n`;
    yaml += `  subtitleText: {fileID: ${rewardSubtitleText.tmpId}}\n`;
    yaml += `  leftHeaderTitle: {fileID: ${leftChestHeader.tmpId}}\n`;
    yaml += `  backpackHeaderTitle: {fileID: ${rightBpHeader.tmpId}}\n`;
    yaml += `  hintText: {fileID: ${rewardHintText.tmpId}}\n`;
    yaml += `  lootAllButton: {fileID: ${rewardClaimBtn.btnId}}\n`;
    yaml += `  continueButton: {fileID: ${rewardContBtn.btnId}}\n`;
  } else if (n === victoryCanvas) {
    yaml += `--- !u!114 &${n.uiId}\n`;
    yaml += `MonoBehaviour:\n`;
    yaml += `  m_ObjectHideFlags: 0\n`;
    yaml += `  m_CorrespondingSourceObject: {fileID: 0}\n`;
    yaml += `  m_PrefabInstance: {fileID: 0}\n`;
    yaml += `  m_PrefabAsset: {fileID: 0}\n`;
    yaml += `  m_GameObject: {fileID: ${n.goId}}\n`;
    yaml += `  m_Enabled: 1\n`;
    yaml += `  m_EditorHideFlags: 0\n`;
    yaml += `  m_Script: {fileID: 11500000, guid: ${GUID_VICTORY_UI}, type: 3}\n`;
    yaml += `  m_Name: \n`;
    yaml += `  m_EditorClassIdentifier: Assembly-CSharp::FloorVictoryUI\n`;
    yaml += `  victoryPanel: {fileID: ${victoryOverlay.goId}}\n`;
    yaml += `  canvas: {fileID: ${victoryCanvas.canvasId}}\n`;
    yaml += `  rewardItemsContainer: {fileID: ${rightVictoryContent.rtId}}\n`;
    yaml += `  rightRewardScrollRect: {fileID: ${rightVictoryScroll.scrollId}}\n`;
    yaml += `  leftOverviewScrollRect: {fileID: ${leftVictoryScroll.scrollId}}\n`;
    yaml += `  titleText: {fileID: ${victoryTitleText.tmpId}}\n`;
    yaml += `  subtitleText: {fileID: ${victorySubtitleText.tmpId}}\n`;
    yaml += `  statsSectionGO: {fileID: ${cardStats.goId}}\n`;
    yaml += `  statsBadgesText: {fileID: ${cardStatsText.tmpId}}\n`;
    yaml += `  blessingSectionGO: {fileID: ${cardBless.goId}}\n`;
    yaml += `  blessingDetailsText: {fileID: ${cardBlessText.tmpId}}\n`;
    yaml += `  lootAllButton: {fileID: ${vLootAllBtn.btnId}}\n`;
    yaml += `  returnToBaseButton: {fileID: ${vReturnBtn.btnId}}\n`;
    yaml += `  nextFloorButton: {fileID: ${vNextBtn.btnId}}\n`;
    yaml += `  statusHintText: {fileID: ${victoryHintText.tmpId}}\n`;
  } else if (n === summaryCanvas) {
    yaml += `--- !u!114 &${n.uiId}\n`;
    yaml += `MonoBehaviour:\n`;
    yaml += `  m_ObjectHideFlags: 0\n`;
    yaml += `  m_CorrespondingSourceObject: {fileID: 0}\n`;
    yaml += `  m_PrefabInstance: {fileID: 0}\n`;
    yaml += `  m_PrefabAsset: {fileID: 0}\n`;
    yaml += `  m_GameObject: {fileID: ${n.goId}}\n`;
    yaml += `  m_Enabled: 1\n`;
    yaml += `  m_EditorHideFlags: 0\n`;
    yaml += `  m_Script: {fileID: 11500000, guid: ${GUID_SUMMARY_UI}, type: 3}\n`;
    yaml += `  m_Name: \n`;
    yaml += `  m_EditorClassIdentifier: Assembly-CSharp::DungeonSummaryUI\n`;
    yaml += `  summaryPanel: {fileID: ${summaryOverlay.goId}}\n`;
    yaml += `  canvas: {fileID: ${summaryCanvas.canvasId}}\n`;
    yaml += `  itemsGridContainer: {fileID: ${summaryContent.rtId}}\n`;
    yaml += `  itemsScrollRect: {fileID: ${summaryScroll.scrollId}}\n`;
    yaml += `  headerBannerImg: {fileID: ${summaryHeader.imgId}}\n`;
    yaml += `  headerIcon: {fileID: ${summaryHeaderIcon.imgId}}\n`;
    yaml += `  titleText: {fileID: ${summaryTitleText.tmpId}}\n`;
    yaml += `  subtitleText: {fileID: ${summarySubtitleText.tmpId}}\n`;
    yaml += `  statFloorText: {fileID: ${sFloorTxt.tmpId}}\n`;
    yaml += `  statStagesText: {fileID: ${sStagesTxt.tmpId}}\n`;
    yaml += `  statBossText: {fileID: ${sBossTxt.tmpId}}\n`;
    yaml += `  statHealthText: {fileID: ${sHealthTxt.tmpId}}\n`;
    yaml += `  statBlessingText: {fileID: ${sBlessingTxt.tmpId}}\n`;
    yaml += `  statDayTransitionText: {fileID: ${sDayTxt.tmpId}}\n`;
    yaml += `  specialNoticeText: {fileID: ${sNoticeTxt.tmpId}}\n`;
    yaml += `  lootHeaderTitle: {fileID: ${rightSummaryHeaderTxt.tmpId}}\n`;
    yaml += `  returnToBaseButton: {fileID: ${summaryReturnBtn.btnId}}\n`;
    yaml += `  returnButtonText: {fileID: ${summaryReturnTxt.tmpId}}\n`;
  }

  // 5. CanvasRenderer
  if (n.crId) {
    yaml += `--- !u!222 &${n.crId}\n`;
    yaml += `CanvasRenderer:\n`;
    yaml += `  m_ObjectHideFlags: 0\n`;
    yaml += `  m_CorrespondingSourceObject: {fileID: 0}\n`;
    yaml += `  m_PrefabInstance: {fileID: 0}\n`;
    yaml += `  m_PrefabAsset: {fileID: 0}\n`;
    yaml += `  m_GameObject: {fileID: ${n.goId}}\n`;
    yaml += `  m_CullTransparentMesh: 1\n`;
  }

  // 6. Image
  if (n.imgId) {
    yaml += `--- !u!114 &${n.imgId}\n`;
    yaml += `MonoBehaviour:\n`;
    yaml += `  m_ObjectHideFlags: 0\n`;
    yaml += `  m_CorrespondingSourceObject: {fileID: 0}\n`;
    yaml += `  m_PrefabInstance: {fileID: 0}\n`;
    yaml += `  m_PrefabAsset: {fileID: 0}\n`;
    yaml += `  m_GameObject: {fileID: ${n.goId}}\n`;
    yaml += `  m_Enabled: 1\n`;
    yaml += `  m_EditorHideFlags: 0\n`;
    yaml += `  m_Script: {fileID: 11500000, guid: ${GUID_IMAGE}, type: 3}\n`;
    yaml += `  m_Name: \n`;
    yaml += `  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Image\n`;
    yaml += `  m_Material: {fileID: 0}\n`;

    if (n.frameColor) {
      yaml += `  m_Color: {r: ${n.frameColor.r}, g: ${n.frameColor.g}, b: ${n.frameColor.b}, a: ${n.frameColor.a}}\n`;
      yaml += `  m_RaycastTarget: 1\n`;
      yaml += `  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}\n`;
      yaml += `  m_Maskable: 1\n`;
      yaml += `  m_OnCullStateChanged:\n`;
      yaml += `    m_PersistentCalls:\n`;
      yaml += `      m_Calls: []\n`;
      yaml += `  m_Sprite: {fileID: ${SPRITE_FRAME}, guid: ${GUID_FRAME}, type: 3}\n`;
      yaml += `  m_Type: 1\n`; // Sliced
      yaml += `  m_PreserveAspect: 0\n`;
      yaml += `  m_FillCenter: 1\n`;
    } else if (n.isToolbarSprite) {
      yaml += `  m_Color: {r: ${n.color.r}, g: ${n.color.g}, b: ${n.color.b}, a: ${n.color.a}}\n`;
      yaml += `  m_RaycastTarget: 1\n`;
      yaml += `  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}\n`;
      yaml += `  m_Maskable: 1\n`;
      yaml += `  m_OnCullStateChanged:\n`;
      yaml += `    m_PersistentCalls:\n`;
      yaml += `      m_Calls: []\n`;
      yaml += `  m_Sprite: {fileID: ${SPRITE_TOOLBAR}, guid: ${GUID_TOOLBAR}, type: 3}\n`;
      yaml += `  m_Type: 1\n`;
      yaml += `  m_PreserveAspect: 0\n`;
      yaml += `  m_FillCenter: 1\n`;
    } else if (n.isChestSprite) {
      yaml += `  m_Color: {r: 1, g: 1, b: 1, a: 1}\n`;
      yaml += `  m_RaycastTarget: 1\n`;
      yaml += `  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}\n`;
      yaml += `  m_Maskable: 1\n`;
      yaml += `  m_OnCullStateChanged:\n`;
      yaml += `    m_PersistentCalls:\n`;
      yaml += `      m_Calls: []\n`;
      yaml += `  m_Sprite: {fileID: ${SPRITE_CHEST}, guid: ${GUID_CHEST}, type: 3}\n`;
      yaml += `  m_Type: 0\n`; // Simple
      yaml += `  m_PreserveAspect: 1\n`;
      yaml += `  m_FillCenter: 1\n`;
    } else if (n.btnColor) {
      yaml += `  m_Color: {r: ${n.btnColor.r}, g: ${n.btnColor.g}, b: ${n.btnColor.b}, a: ${n.btnColor.a}}\n`;
      yaml += `  m_RaycastTarget: 1\n`;
      yaml += `  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}\n`;
      yaml += `  m_Maskable: 1\n`;
      yaml += `  m_OnCullStateChanged:\n`;
      yaml += `    m_PersistentCalls:\n`;
      yaml += `      m_Calls: []\n`;
      yaml += `  m_Sprite: {fileID: ${SPRITE_FRAME}, guid: ${GUID_FRAME}, type: 3}\n`;
      yaml += `  m_Type: 1\n`;
      yaml += `  m_PreserveAspect: 0\n`;
      yaml += `  m_FillCenter: 1\n`;
    } else if (n.color) {
      yaml += `  m_Color: {r: ${n.color.r}, g: ${n.color.g}, b: ${n.color.b}, a: ${n.color.a}}\n`;
      yaml += `  m_RaycastTarget: 1\n`;
      yaml += `  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}\n`;
      yaml += `  m_Maskable: 1\n`;
      yaml += `  m_OnCullStateChanged:\n`;
      yaml += `    m_PersistentCalls:\n`;
      yaml += `      m_Calls: []\n`;
      yaml += `  m_Sprite: {fileID: 0}\n`;
      yaml += `  m_Type: 0\n`;
      yaml += `  m_PreserveAspect: 0\n`;
      yaml += `  m_FillCenter: 1\n`;
    } else {
      yaml += `  m_Color: {r: 1, g: 1, b: 1, a: 1}\n`;
      yaml += `  m_RaycastTarget: 1\n`;
      yaml += `  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}\n`;
      yaml += `  m_Maskable: 1\n`;
      yaml += `  m_OnCullStateChanged:\n`;
      yaml += `    m_PersistentCalls:\n`;
      yaml += `      m_Calls: []\n`;
      yaml += `  m_Sprite: {fileID: 0}\n`;
      yaml += `  m_Type: 0\n`;
      yaml += `  m_PreserveAspect: 0\n`;
      yaml += `  m_FillCenter: 1\n`;
    }
    yaml += `  m_FillMethod: 4\n`;
    yaml += `  m_FillAmount: 1\n`;
    yaml += `  m_FillClockwise: 1\n`;
    yaml += `  m_FillOrigin: 0\n`;
    yaml += `  m_UseSpriteMesh: 0\n`;
  }

  // 7. TextMeshProUGUI
  if (n.tmpId) {
    yaml += `--- !u!114 &${n.tmpId}\n`;
    yaml += `MonoBehaviour:\n`;
    yaml += `  m_ObjectHideFlags: 0\n`;
    yaml += `  m_CorrespondingSourceObject: {fileID: 0}\n`;
    yaml += `  m_PrefabInstance: {fileID: 0}\n`;
    yaml += `  m_PrefabAsset: {fileID: 0}\n`;
    yaml += `  m_GameObject: {fileID: ${n.goId}}\n`;
    yaml += `  m_Enabled: 1\n`;
    yaml += `  m_EditorHideFlags: 0\n`;
    yaml += `  m_Script: {fileID: 11500000, guid: ${GUID_TMP}, type: 3}\n`;
    yaml += `  m_Name: \n`;
    yaml += `  m_EditorClassIdentifier: Unity.TextMeshPro::TMPro.TextMeshProUGUI\n`;
    yaml += `  m_Material: {fileID: 0}\n`;
    yaml += `  m_Color: {r: 1, g: 1, b: 1, a: 1}\n`;
    yaml += `  m_RaycastTarget: 1\n`;
    yaml += `  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}\n`;
    yaml += `  m_Maskable: 1\n`;
    yaml += `  m_OnCullStateChanged:\n`;
    yaml += `    m_PersistentCalls:\n`;
    yaml += `      m_Calls: []\n`;
    yaml += `  m_text: '${n.text || ''}'\n`;
    yaml += `  m_isRightToLeft: 0\n`;
    yaml += `  m_fontAsset: {fileID: 11400000, guid: ${GUID_FONT}, type: 2}\n`;
    yaml += `  m_sharedMaterial: {fileID: 2180264, guid: ${GUID_FONT}, type: 2}\n`;
    yaml += `  m_fontSharedMaterials: []\n`;
    yaml += `  m_fontMaterial: {fileID: 0}\n`;
    yaml += `  m_fontMaterials: []\n`;
    yaml += `  m_fontColor32:\n`;
    yaml += `    serializedVersion: 2\n`;
    const c = n.color || { r: 1, g: 1, b: 1, a: 1 };
    yaml += `    rgba: 4294967295\n`;
    yaml += `  m_fontColor: {r: ${c.r}, g: ${c.g}, b: ${c.b}, a: ${c.a}}\n`;
    yaml += `  m_enableVertexGradient: 0\n`;
    yaml += `  m_colorMode: 3\n`;
    yaml += `  m_fontColorGradient:\n`;
    yaml += `    topLeft: {r: 1, g: 1, b: 1, a: 1}\n`;
    yaml += `    topRight: {r: 1, g: 1, b: 1, a: 1}\n`;
    yaml += `    bottomLeft: {r: 1, g: 1, b: 1, a: 1}\n`;
    yaml += `    bottomRight: {r: 1, g: 1, b: 1, a: 1}\n`;
    yaml += `  m_fontColorGradientPreset: {fileID: 0}\n`;
    yaml += `  m_spriteAsset: {fileID: 0}\n`;
    yaml += `  m_tintAllSprites: 0\n`;
    yaml += `  m_StyleSheet: {fileID: 0}\n`;
    yaml += `  m_TextStyleHashCode: -1183493901\n`;
    yaml += `  m_overrideHtmlColors: 0\n`;
    yaml += `  m_faceColor:\n`;
    yaml += `    serializedVersion: 2\n`;
    yaml += `    rgba: 4294967295\n`;
    yaml += `  m_fontSize: ${n.fontSize || 14}\n`;
    yaml += `  m_fontSizeBase: ${n.fontSize || 14}\n`;
    yaml += `  m_fontWeight: 400\n`;
    yaml += `  m_enableAutoSizing: 0\n`;
    yaml += `  m_fontSizeMin: 18\n`;
    yaml += `  m_fontSizeMax: 72\n`;
    yaml += `  m_fontStyle: ${n.fontStyle || 0}\n`;
    yaml += `  m_HorizontalAlignment: 1\n`;
    yaml += `  m_VerticalAlignment: 256\n`;
    yaml += `  m_textAlignment: ${n.align || 257}\n`;
    yaml += `  m_characterSpacing: 0\n`;
    yaml += `  m_wordSpacing: 0\n`;
    yaml += `  m_lineSpacing: 0\n`;
    yaml += `  m_lineSpacingMax: 0\n`;
    yaml += `  m_paragraphSpacing: 0\n`;
    yaml += `  m_charWidthMaxAdj: 0\n`;
    yaml += `  m_enableWordWrapping: 1\n`;
    yaml += `  m_wordWrappingRatios: 0.4\n`;
    yaml += `  m_overflowMode: 0\n`;
    yaml += `  m_linkedTextComponent: {fileID: 0}\n`;
    yaml += `  parentLinkedComponent: {fileID: 0}\n`;
    yaml += `  m_enableKerning: 1\n`;
    yaml += `  m_enableExtraPadding: 0\n`;
    yaml += `  checkPaddingRequired: 0\n`;
    yaml += `  m_isRichText: 1\n`;
    yaml += `  m_parseCtrlCharacters: 1\n`;
    yaml += `  m_isOrthographic: 1\n`;
    yaml += `  m_isCullingEnabled: 0\n`;
    yaml += `  m_horizontalMapping: 0\n`;
    yaml += `  m_verticalMapping: 0\n`;
    yaml += `  m_uvLineOffset: 0\n`;
    yaml += `  m_geometrySortingOrder: 0\n`;
    yaml += `  m_IsTextObjectScaleStatic: 0\n`;
    yaml += `  m_VertexBufferAutoSizeReduction: 0\n`;
    yaml += `  m_useMaxVisibleDescender: 1\n`;
    yaml += `  m_pageToDisplay: 1\n`;
    yaml += `  m_margin: {x: 0, y: 0, z: 0, w: 0}\n`;
    yaml += `  m_isUsingLegacyAnimationComponent: 0\n`;
    yaml += `  m_isVolumetricText: 0\n`;
    yaml += `  m_hasFontAssetChanged: 0\n`;
    yaml += `  m_baseMaterial: {fileID: 0}\n`;
    yaml += `  m_maskOffset: {x: 0, y: 0, z: 0, w: 0}\n`;
  }

  // 8. Button
  if (n.btnId) {
    yaml += `--- !u!114 &${n.btnId}\n`;
    yaml += `MonoBehaviour:\n`;
    yaml += `  m_ObjectHideFlags: 0\n`;
    yaml += `  m_CorrespondingSourceObject: {fileID: 0}\n`;
    yaml += `  m_PrefabInstance: {fileID: 0}\n`;
    yaml += `  m_PrefabAsset: {fileID: 0}\n`;
    yaml += `  m_GameObject: {fileID: ${n.goId}}\n`;
    yaml += `  m_Enabled: 1\n`;
    yaml += `  m_EditorHideFlags: 0\n`;
    yaml += `  m_Script: {fileID: 11500000, guid: ${GUID_BUTTON}, type: 3}\n`;
    yaml += `  m_Name: \n`;
    yaml += `  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Button\n`;
    yaml += `  m_Navigation:\n`;
    yaml += `    m_Mode: 0\n`;
    yaml += `    m_WrapAround: 0\n`;
    yaml += `    m_SelectOnUp: {fileID: 0}\n`;
    yaml += `    m_SelectOnDown: {fileID: 0}\n`;
    yaml += `    m_SelectOnLeft: {fileID: 0}\n`;
    yaml += `    m_SelectOnRight: {fileID: 0}\n`;
    yaml += `  m_Transition: 1\n`;
    yaml += `  m_Colors:\n`;
    yaml += `    m_NormalColor: {r: 1, g: 1, b: 1, a: 1}\n`;
    yaml += `    m_HighlightedColor: {r: 1.15, g: 1.15, b: 1.15, a: 1}\n`;
    yaml += `    m_PressedColor: {r: 0.8, g: 0.8, b: 0.8, a: 1}\n`;
    yaml += `    m_SelectedColor: {r: 1, g: 1, b: 1, a: 1}\n`;
    yaml += `    m_DisabledColor: {r: 0.5, g: 0.5, b: 0.5, a: 0.5}\n`;
    yaml += `    m_ColorMultiplier: 1\n`;
    yaml += `    m_FadeDuration: 0.1\n`;
    yaml += `  m_SpriteState:\n`;
    yaml += `    m_HighlightedSprite: {fileID: 0}\n`;
    yaml += `    m_PressedSprite: {fileID: 0}\n`;
    yaml += `    m_SelectedSprite: {fileID: 0}\n`;
    yaml += `    m_DisabledSprite: {fileID: 0}\n`;
    yaml += `  m_AnimationTriggers:\n`;
    yaml += `    m_NormalTrigger: Normal\n`;
    yaml += `    m_HighlightedTrigger: Highlighted\n`;
    yaml += `    m_PressedTrigger: Pressed\n`;
    yaml += `    m_SelectedTrigger: Selected\n`;
    yaml += `    m_DisabledTrigger: Disabled\n`;
    yaml += `  m_Interactable: 1\n`;
    yaml += `  m_TargetGraphic: {fileID: ${n.imgId}}\n`;
    yaml += `  m_OnClick:\n`;
    yaml += `    m_PersistentCalls:\n`;
    yaml += `      m_Calls:\n`;
    if (n.targetMethod && n.targetUI) {
      yaml += `      - m_Target: {fileID: ${n.targetUI}}\n`;
      yaml += `        m_TargetAssemblyTypeName: Assembly-CSharp\n`;
      yaml += `        m_MethodName: ${n.targetMethod}\n`;
      yaml += `        m_Mode: 1\n`;
      yaml += `        m_Arguments:\n`;
      yaml += `          m_ObjectArgument: {fileID: 0}\n`;
      yaml += `          m_ObjectArgumentAssemblyTypeName: UnityEngine.Object, UnityEngine\n`;
      yaml += `          m_IntArgument: 0\n`;
      yaml += `          m_FloatArgument: 0\n`;
      yaml += `          m_StringArgument: \n`;
      yaml += `          m_BoolArgument: 0\n`;
      yaml += `        m_CallState: 2\n`;
    }
  }

  // 9. ScrollRect
  if (n.scrollId) {
    yaml += `--- !u!114 &${n.scrollId}\n`;
    yaml += `MonoBehaviour:\n`;
    yaml += `  m_ObjectHideFlags: 0\n`;
    yaml += `  m_CorrespondingSourceObject: {fileID: 0}\n`;
    yaml += `  m_PrefabInstance: {fileID: 0}\n`;
    yaml += `  m_PrefabAsset: {fileID: 0}\n`;
    yaml += `  m_GameObject: {fileID: ${n.goId}}\n`;
    yaml += `  m_Enabled: 1\n`;
    yaml += `  m_EditorHideFlags: 0\n`;
    yaml += `  m_Script: {fileID: 11500000, guid: ${GUID_SCROLLRECT}, type: 3}\n`;
    yaml += `  m_Name: \n`;
    yaml += `  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.ScrollRect\n`;
    yaml += `  m_Content: {fileID: ${n.contentId || 0}}\n`;
    yaml += `  m_Horizontal: 0\n`;
    yaml += `  m_Vertical: 1\n`;
    yaml += `  m_MovementType: 2\n`; // Clamped
    yaml += `  m_Elasticity: 0.1\n`;
    yaml += `  m_Inertia: 1\n`;
    yaml += `  m_DecelerationRate: 0.135\n`;
    yaml += `  m_ScrollSensitivity: 25\n`;
    yaml += `  m_Viewport: {fileID: ${n.viewportId || 0}}\n`;
    yaml += `  m_HorizontalScrollbar: {fileID: 0}\n`;
    yaml += `  m_VerticalScrollbar: {fileID: ${n.scrollbarId || 0}}\n`;
    yaml += `  m_HorizontalScrollbarVisibility: 0\n`;
    yaml += `  m_VerticalScrollbarVisibility: 0\n`;
    yaml += `  m_HorizontalScrollbarSpacing: 0\n`;
    yaml += `  m_VerticalScrollbarSpacing: 0\n`;
    yaml += `  m_OnValueChanged:\n`;
    yaml += `    m_PersistentCalls:\n`;
    yaml += `      m_Calls: []\n`;
  }

  // 10. RectMask2D
  if (n.maskId) {
    yaml += `--- !u!114 &${n.maskId}\n`;
    yaml += `MonoBehaviour:\n`;
    yaml += `  m_ObjectHideFlags: 0\n`;
    yaml += `  m_CorrespondingSourceObject: {fileID: 0}\n`;
    yaml += `  m_PrefabInstance: {fileID: 0}\n`;
    yaml += `  m_PrefabAsset: {fileID: 0}\n`;
    yaml += `  m_GameObject: {fileID: ${n.goId}}\n`;
    yaml += `  m_Enabled: 1\n`;
    yaml += `  m_EditorHideFlags: 0\n`;
    yaml += `  m_Script: {fileID: 11500000, guid: ${GUID_RECTMASK2D}, type: 3}\n`;
    yaml += `  m_Name: \n`;
    yaml += `  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.RectMask2D\n`;
    yaml += `  m_Padding: {x: 0, y: 0, z: 0, w: 0}\n`;
    yaml += `  m_Softness: {x: 0, y: 0}\n`;
  }

  // 11. VerticalLayoutGroup
  if (n.vlgId) {
    yaml += `--- !u!114 &${n.vlgId}\n`;
    yaml += `MonoBehaviour:\n`;
    yaml += `  m_ObjectHideFlags: 0\n`;
    yaml += `  m_CorrespondingSourceObject: {fileID: 0}\n`;
    yaml += `  m_PrefabInstance: {fileID: 0}\n`;
    yaml += `  m_PrefabAsset: {fileID: 0}\n`;
    yaml += `  m_GameObject: {fileID: ${n.goId}}\n`;
    yaml += `  m_Enabled: 1\n`;
    yaml += `  m_EditorHideFlags: 0\n`;
    yaml += `  m_Script: {fileID: 11500000, guid: ${GUID_VLG}, type: 3}\n`;
    yaml += `  m_Name: \n`;
    yaml += `  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.VerticalLayoutGroup\n`;
    const p = n.vlgPad || { l: 0, r: 0, t: 0, b: 0 };
    yaml += `  m_Padding:\n`;
    yaml += `    m_Left: ${p.l}\n`;
    yaml += `    m_Right: ${p.r}\n`;
    yaml += `    m_Top: ${p.t}\n`;
    yaml += `    m_Bottom: ${p.b}\n`;
    yaml += `  m_ChildAlignment: 0\n`;
    yaml += `  m_Spacing: ${n.vlgSpacing || 0}\n`;
    yaml += `  m_ChildForceExpandWidth: 1\n`;
    yaml += `  m_ChildForceExpandHeight: 0\n`;
    yaml += `  m_ChildControlWidth: 1\n`;
    yaml += `  m_ChildControlHeight: 0\n`;
    yaml += `  m_ChildScaleWidth: 0\n`;
    yaml += `  m_ChildScaleHeight: 0\n`;
    yaml += `  m_ReverseArrangement: 0\n`;
  }

  // 12. HorizontalLayoutGroup
  if (n.hlgId) {
    yaml += `--- !u!114 &${n.hlgId}\n`;
    yaml += `MonoBehaviour:\n`;
    yaml += `  m_ObjectHideFlags: 0\n`;
    yaml += `  m_CorrespondingSourceObject: {fileID: 0}\n`;
    yaml += `  m_PrefabInstance: {fileID: 0}\n`;
    yaml += `  m_PrefabAsset: {fileID: 0}\n`;
    yaml += `  m_GameObject: {fileID: ${n.goId}}\n`;
    yaml += `  m_Enabled: 1\n`;
    yaml += `  m_EditorHideFlags: 0\n`;
    yaml += `  m_Script: {fileID: 11500000, guid: ${GUID_HLG}, type: 3}\n`;
    yaml += `  m_Name: \n`;
    yaml += `  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.HorizontalLayoutGroup\n`;
    yaml += `  m_Padding:\n`;
    yaml += `    m_Left: 0\n`;
    yaml += `    m_Right: 0\n`;
    yaml += `    m_Top: 0\n`;
    yaml += `    m_Bottom: 0\n`;
    yaml += `  m_ChildAlignment: 5\n`; // MiddleRight
    yaml += `  m_Spacing: ${n.hlgSpacing || 0}\n`;
    yaml += `  m_ChildForceExpandWidth: 0\n`;
    yaml += `  m_ChildForceExpandHeight: 0\n`;
    yaml += `  m_ChildControlWidth: 0\n`;
    yaml += `  m_ChildControlHeight: 0\n`;
    yaml += `  m_ChildScaleWidth: 0\n`;
    yaml += `  m_ChildScaleHeight: 0\n`;
    yaml += `  m_ReverseArrangement: 0\n`;
  }

  // 13. GridLayoutGroup
  if (n.glgId) {
    yaml += `--- !u!114 &${n.glgId}\n`;
    yaml += `MonoBehaviour:\n`;
    yaml += `  m_ObjectHideFlags: 0\n`;
    yaml += `  m_CorrespondingSourceObject: {fileID: 0}\n`;
    yaml += `  m_PrefabInstance: {fileID: 0}\n`;
    yaml += `  m_PrefabAsset: {fileID: 0}\n`;
    yaml += `  m_GameObject: {fileID: ${n.goId}}\n`;
    yaml += `  m_Enabled: 1\n`;
    yaml += `  m_EditorHideFlags: 0\n`;
    yaml += `  m_Script: {fileID: 11500000, guid: ${GUID_GLG}, type: 3}\n`;
    yaml += `  m_Name: \n`;
    yaml += `  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.GridLayoutGroup\n`;
    const p = n.glgPad || { l: 0, r: 0, t: 0, b: 0 };
    yaml += `  m_Padding:\n`;
    yaml += `    m_Left: ${p.l}\n`;
    yaml += `    m_Right: ${p.r}\n`;
    yaml += `    m_Top: ${p.t}\n`;
    yaml += `    m_Bottom: ${p.b}\n`;
    yaml += `  m_CellSize: {x: ${n.glgCell.x}, y: ${n.glgCell.y}}\n`;
    yaml += `  m_Spacing: {x: ${n.glgSpacing.x}, y: ${n.glgSpacing.y}}\n`;
    yaml += `  m_StartCorner: 0\n`;
    yaml += `  m_StartAxis: 0\n`;
    yaml += `  m_ChildAlignment: 0\n`;
    yaml += `  m_Constraint: 1\n`; // FixedColumnCount
    yaml += `  m_ConstraintCount: ${n.glgCount || 5}\n`;
  }

  // 14. ContentSizeFitter
  if (n.csfId) {
    yaml += `--- !u!114 &${n.csfId}\n`;
    yaml += `MonoBehaviour:\n`;
    yaml += `  m_ObjectHideFlags: 0\n`;
    yaml += `  m_CorrespondingSourceObject: {fileID: 0}\n`;
    yaml += `  m_PrefabInstance: {fileID: 0}\n`;
    yaml += `  m_PrefabAsset: {fileID: 0}\n`;
    yaml += `  m_GameObject: {fileID: ${n.goId}}\n`;
    yaml += `  m_Enabled: 1\n`;
    yaml += `  m_EditorHideFlags: 0\n`;
    yaml += `  m_Script: {fileID: 11500000, guid: ${GUID_CSF}, type: 3}\n`;
    yaml += `  m_Name: \n`;
    yaml += `  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.ContentSizeFitter\n`;
    yaml += `  m_HorizontalFit: 0\n`;
    yaml += `  m_VerticalFit: 2\n`; // PreferredSize
  }

  // 15. Scrollbar
  if (n.sbId) {
    yaml += `--- !u!114 &${n.sbId}\n`;
    yaml += `MonoBehaviour:\n`;
    yaml += `  m_ObjectHideFlags: 0\n`;
    yaml += `  m_CorrespondingSourceObject: {fileID: 0}\n`;
    yaml += `  m_PrefabInstance: {fileID: 0}\n`;
    yaml += `  m_PrefabAsset: {fileID: 0}\n`;
    yaml += `  m_GameObject: {fileID: ${n.goId}}\n`;
    yaml += `  m_Enabled: 1\n`;
    yaml += `  m_EditorHideFlags: 0\n`;
    yaml += `  m_Script: {fileID: 11500000, guid: ${GUID_SCROLLBAR}, type: 3}\n`;
    yaml += `  m_Name: \n`;
    yaml += `  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Scrollbar\n`;
    yaml += `  m_Navigation:\n`;
    yaml += `    m_Mode: 0\n`;
    yaml += `    m_WrapAround: 0\n`;
    yaml += `    m_SelectOnUp: {fileID: 0}\n`;
    yaml += `    m_SelectOnDown: {fileID: 0}\n`;
    yaml += `    m_SelectOnLeft: {fileID: 0}\n`;
    yaml += `    m_SelectOnRight: {fileID: 0}\n`;
    yaml += `  m_Transition: 1\n`;
    yaml += `  m_Colors:\n`;
    yaml += `    m_NormalColor: {r: 1, g: 1, b: 1, a: 1}\n`;
    yaml += `    m_HighlightedColor: {r: 1.1, g: 1.1, b: 1.1, a: 1}\n`;
    yaml += `    m_PressedColor: {r: 0.8, g: 0.8, b: 0.8, a: 1}\n`;
    yaml += `    m_SelectedColor: {r: 1, g: 1, b: 1, a: 1}\n`;
    yaml += `    m_DisabledColor: {r: 0.5, g: 0.5, b: 0.5, a: 0.5}\n`;
    yaml += `    m_ColorMultiplier: 1\n`;
    yaml += `    m_FadeDuration: 0.1\n`;
    yaml += `  m_SpriteState:\n`;
    yaml += `    m_HighlightedSprite: {fileID: 0}\n`;
    yaml += `    m_PressedSprite: {fileID: 0}\n`;
    yaml += `    m_SelectedSprite: {fileID: 0}\n`;
    yaml += `    m_DisabledSprite: {fileID: 0}\n`;
    yaml += `  m_AnimationTriggers:\n`;
    yaml += `    m_NormalTrigger: Normal\n`;
    yaml += `    m_HighlightedTrigger: Highlighted\n`;
    yaml += `    m_PressedTrigger: Pressed\n`;
    yaml += `    m_SelectedTrigger: Selected\n`;
    yaml += `    m_DisabledTrigger: Disabled\n`;
    yaml += `  m_Interactable: 1\n`;
    yaml += `  m_TargetGraphic: {fileID: ${n.sbGraphic || 0}}\n`;
    yaml += `  m_HandleRect: {fileID: ${n.sbHandle || 0}}\n`;
    yaml += `  m_Direction: 2\n`; // BottomToTop
    yaml += `  m_Value: 0\n`;
    yaml += `  m_Size: 0.2\n`;
    yaml += `  m_NumberOfSteps: 0\n`;
    yaml += `  m_OnValueChanged:\n`;
    yaml += `    m_PersistentCalls:\n`;
    yaml += `      m_Calls: []\n`;
  }
}

// ==========================================
// SCENE INJECTION & CLEANUP
// ==========================================
const scenePath = path.resolve(__dirname, '../Assets/_Scenes/Dungeon.unity');
let sceneText = fs.readFileSync(scenePath, 'utf8');

// 1. Remove dummy RewardUI (1873114120) from CombatCanvas m_Component list
sceneText = sceneText.replace(/  - component: {fileID: 1873114120}\r?\n/, '');

// 2. Remove the block --- !u!114 &1873114120 up to the next block
const rewardBlockPattern = /--- !u!114 &1873114120[\s\S]*?(?=--- !u!)/;
sceneText = sceneText.replace(rewardBlockPattern, '');

// 3. Hook targetReticle to {fileID: 1125566292} in CombatUI (1873114111)
sceneText = sceneText.replace(/  targetReticle: {fileID: 0}/, '  targetReticle: {fileID: 1125566292}');

// 4. Remove any existing LootCanvas, RewardCanvas, FloorVictoryCanvas, DungeonSummaryCanvas if previously added
if (sceneText.includes('m_Name: LootCanvas')) {
  const lootIdx = sceneText.indexOf('m_Name: LootCanvas');
  const lootStart = sceneText.lastIndexOf('--- !u!1 &', lootIdx);
  sceneText = sceneText.substring(0, lootStart);
  console.log('Cleaned up previous LootCanvas blocks.');
}

// 4b. Center ActionBar in CombatCanvas (fileID 1675800097)
sceneText = sceneText.replace(
  /(--- !u!224 &1675800097[\s\S]*?m_AnchoredPosition: )\{x: 120, y: 25\}/,
  '$1{x: 0, y: 25}'
);

// 5. Append all newly generated YAML nodes
sceneText = sceneText.trimEnd() + '\n' + yaml;

fs.writeFileSync(scenePath, sceneText, 'utf8');
console.log(`[build_combat_canvases] SUCCESS: Appended ${allNodes.length} nodes to Dungeon.unity!`);
