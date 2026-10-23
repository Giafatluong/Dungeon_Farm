const fs = require('fs');
const path = require('path');

// Component GUIDs
const GUID_TMP = 'f4688fdb7df04437aeb418b961361dc5';
const GUID_IMAGE = 'fe87c0e1cc204ed48ad3b37840f39efc';
const GUID_BUTTON = '4e29b1a8efbd4b44bb3f3716e73f07ff';
const GUID_SLIDER = '67db9e8f0e2ae9c40bc1e2b64352a6b4';
const GUID_SCROLLRECT = '1aa08ab6e0800fa44ae55d278d1423e3';
const GUID_CANVAS_SCALER = '0cd44c1031e13a943bb63640046fad76';
const GUID_GRAPHIC_RAYCASTER = 'dc42784cf147c0c48a680349fa168899';
const GUID_HLG = '30649d3a9efe99c4e812b5cb27160e08';
const GUID_VLG = '59f8146938c82b347a7d335c0a29d16a';
const GUID_GLG = '8a8695521f0d02e499659fee002a26c2';
const GUID_CSF = '3245ec927659c4140ac4f8d17403cc18';
const GUID_RECTMASK2D = '31a1949cdeb71f84bad4176b3298664f';
const GUID_KITCHEN_UI = '87a8dbca7a7a55e4d86f29d69d88d0f4';
const GUID_RECIPE_CARD_UI = '92c738ef1234567890abcdef01234567';

// Asset GUIDs
const GUID_FONT = '8f586378b4e144a9851e7b34d9b748ee';
const GUID_FRAME = 'aa6f9a2ecff34c14d84e5bd91727c940';
const SPRITE_FRAME = '-5668518402485374812';
const GUID_HEADER_PLAQUE = '719e2e4a28cd1184ab3762dbc4850a4d';
const SPRITE_HEADER_PLAQUE = '-586511307516372644';
const GUID_CARD_BG = 'dd9c5a3b5d132ea46acdbf6623b45633';
const SPRITE_CARD_BG = '-852011609251132011';
const GUID_PREVIEW_BOX = '761ec30b1713dab49a87b858dc16d4ea';
const SPRITE_PREVIEW_BOX = '2719065537576319748';
const GUID_PREVIEW_ICON_FRAME = '07a88cfd7508dbf4381f96a64f1e4ce9';
const SPRITE_PREVIEW_ICON_FRAME = '-6235862569765844502';
const GUID_CARD_ICON_FRAME = '7c86d342e0c39c840b2c976a90d5a47c';
const SPRITE_CARD_ICON_FRAME = '-8094992848256881038';
const GUID_SOUP_ICON = '610a724bf28aa4e44b9a1eb4fcbbcd81'; // 01_tomato_soup-removebg-preview
const GUID_CAMPFIRE = 'c550f469f9e01234a9b12f1c3606b4b6';
const SPRITE_CAMPFIRE_1 = '21300000';
const GUID_INVENTORY_SLOT_PREFAB = '02cead57d3ffaca44b4e995473eaea19';

let nextId = 910000000;
function genId() {
  return nextId++;
}

class UnityNode {
  constructor(name, isRoot = false) {
    this.name = name;
    this.isRoot = isRoot;
    this.goId = genId();
    this.rtId = genId();
    this.children = [];
    this.father = null;
    this.components = [];
    this.isActive = 1;
    this.layer = 5; // UI layer
    
    // RectTransform defaults (centered)
    this.anchorMin = { x: 0.5, y: 0.5 };
    this.anchorMax = { x: 0.5, y: 0.5 };
    this.pivot = { x: 0.5, y: 0.5 };
    this.anchoredPosition = { x: 0, y: 0 };
    this.sizeDelta = { x: 100, y: 100 };
    this.localRotation = { x: 0, y: 0, z: 0, w: 1 };
    this.localScale = { x: 1, y: 1, z: 1 };
  }

  addChild(child) {
    child.father = this;
    this.children.push(child);
    return child;
  }
}

// Global registry of all nodes
const allNodes = [];
const blocks = [];

function register(node) {
  allNodes.push(node);
  return node;
}

// BUILD HIERARCHY
const kitchenCanvas = register(new UnityNode('KitchenCanvas', true));
kitchenCanvas.isActive = 1; // starts active, but KitchenOverlay inside is inactive by default

// KitchenOverlay (fullscreen backdrop)
const overlay = register(new UnityNode('KitchenOverlay'));
overlay.isActive = 0; // Inactive by default until opened by KitchenStation!
overlay.anchorMin = { x: 0, y: 0 };
overlay.anchorMax = { x: 1, y: 1 };
overlay.sizeDelta = { x: 0, y: 0 };
overlay.pivot = { x: 0.5, y: 0.5 };
kitchenCanvas.addChild(overlay);

// KitchenWindow (1440 x 860)
const windowNode = register(new UnityNode('KitchenWindow'));
windowNode.sizeDelta = { x: 1440, y: 860 };
overlay.addChild(windowNode);

// Inner border
const innerBorder = register(new UnityNode('InnerBorder'));
innerBorder.anchorMin = { x: 0, y: 0 };
innerBorder.anchorMax = { x: 1, y: 1 };
innerBorder.anchoredPosition = { x: 0, y: 0 };
innerBorder.sizeDelta = { x: -16, y: -16 };
windowNode.addChild(innerBorder);

// TopHeaderBar
const header = register(new UnityNode('TopHeaderBar'));
header.anchorMin = { x: 0, y: 1 };
header.anchorMax = { x: 1, y: 1 };
header.pivot = { x: 0.5, y: 1 };
header.anchoredPosition = { x: 0, y: -14 };
header.sizeDelta = { x: -36, y: 58 };
windowNode.addChild(header);

const headerTitle = register(new UnityNode('HeaderTitle'));
headerTitle.anchorMin = { x: 0, y: 0 };
headerTitle.anchorMax = { x: 0.75, y: 1 };
headerTitle.anchoredPosition = { x: 24, y: 0 };
headerTitle.sizeDelta = { x: -24, y: 0 };
headerTitle.pivot = { x: 0, y: 0.5 };
header.addChild(headerTitle);

const closeBtn = register(new UnityNode('CloseButton'));
closeBtn.anchorMin = { x: 1, y: 0.5 };
closeBtn.anchorMax = { x: 1, y: 0.5 };
closeBtn.pivot = { x: 1, y: 0.5 };
closeBtn.anchoredPosition = { x: -18, y: 0 };
closeBtn.sizeDelta = { x: 175, y: 42 };
header.addChild(closeBtn);

const closeBtnText = register(new UnityNode('CloseText'));
closeBtnText.anchorMin = { x: 0, y: 0 };
closeBtnText.anchorMax = { x: 1, y: 1 };
closeBtnText.sizeDelta = { x: 0, y: 0 };
closeBtn.addChild(closeBtnText);

// ==========================================
// LEFT PANEL: SỔ TAY CÔNG THỨC (RECIPES)
// ==========================================
const leftPanel = register(new UnityNode('LeftPanel_Recipes'));
leftPanel.anchorMin = { x: 0.016, y: 0.024 };
leftPanel.anchorMax = { x: 0.340, y: 0.900 };
leftPanel.anchoredPosition = { x: 0, y: 0 };
leftPanel.sizeDelta = { x: 0, y: 0 };
windowNode.addChild(leftPanel);

const filterTabBar = register(new UnityNode('FilterTabBar'));
filterTabBar.anchorMin = { x: 0, y: 1 };
filterTabBar.anchorMax = { x: 1, y: 1 };
filterTabBar.pivot = { x: 0.5, y: 1 };
filterTabBar.anchoredPosition = { x: 0, y: -12 };
filterTabBar.sizeDelta = { x: -20, y: 42 };
leftPanel.addChild(filterTabBar);

function createTab(name, label) {
  const tNode = register(new UnityNode(name));
  filterTabBar.addChild(tNode);
  const lNode = register(new UnityNode('Label'));
  lNode.anchorMin = { x: 0, y: 0 };
  lNode.anchorMax = { x: 1, y: 1 };
  lNode.sizeDelta = { x: 0, y: 0 };
  tNode.addChild(lNode);
  return { tab: tNode, label: lNode };
}

const tabAll = createTab('Tab_All', 'TẤT CẢ (12)');
const tabRec = createTab('Tab_Recovery', 'HỒI PHỤC');
const tabSpd = createTab('Tab_Speed', 'TĂNG TỐC');
const tabOth = createTab('Tab_Other', 'KHÁC');

const rScrollView = register(new UnityNode('RecipeScrollView'));
rScrollView.anchorMin = { x: 0, y: 0 };
rScrollView.anchorMax = { x: 1, y: 1 };
rScrollView.anchoredPosition = { x: 0, y: -25 };
rScrollView.sizeDelta = { x: -20, y: -74 };
leftPanel.addChild(rScrollView);

const rViewport = register(new UnityNode('Viewport'));
rViewport.anchorMin = { x: 0, y: 0 };
rViewport.anchorMax = { x: 1, y: 1 };
rViewport.sizeDelta = { x: 0, y: 0 };
rScrollView.addChild(rViewport);

const rContent = register(new UnityNode('Content'));
rContent.anchorMin = { x: 0, y: 1 };
rContent.anchorMax = { x: 1, y: 1 };
rContent.pivot = { x: 0.5, y: 1 };
rContent.anchoredPosition = { x: 0, y: 0 };
rContent.sizeDelta = { x: 0, y: 0 };
rViewport.addChild(rContent);

// RecipeCardTemplate (Inactive template inside LeftPanel)
const cardTemplate = register(new UnityNode('RecipeCardTemplate'));
cardTemplate.isActive = 0;
cardTemplate.sizeDelta = { x: 418, y: 92 };
leftPanel.addChild(cardTemplate);

const cardIconFrame = register(new UnityNode('IconFrame'));
cardIconFrame.anchorMin = { x: 0, y: 0.5 };
cardIconFrame.anchorMax = { x: 0, y: 0.5 };
cardIconFrame.pivot = { x: 0, y: 0.5 };
cardIconFrame.anchoredPosition = { x: 12, y: 0 };
cardIconFrame.sizeDelta = { x: 68, y: 68 };
cardTemplate.addChild(cardIconFrame);

const cardDishIcon = register(new UnityNode('DishIcon'));
cardDishIcon.anchorMin = { x: 0, y: 0 };
cardDishIcon.anchorMax = { x: 1, y: 1 };
cardDishIcon.sizeDelta = { x: -12, y: -12 };
cardIconFrame.addChild(cardDishIcon);

const cardInfo = register(new UnityNode('InfoColumn'));
cardInfo.anchorMin = { x: 0, y: 0 };
cardInfo.anchorMax = { x: 1, y: 1 };
cardInfo.anchoredPosition = { x: -8, y: 0 };
cardInfo.sizeDelta = { x: -210, y: -16 };
cardTemplate.addChild(cardInfo);

const cardTitleRow = register(new UnityNode('TitleRow'));
cardTitleRow.anchorMin = { x: 0, y: 1 };
cardTitleRow.anchorMax = { x: 1, y: 1 };
cardTitleRow.pivot = { x: 0, y: 1 };
cardTitleRow.anchoredPosition = { x: 92, y: -10 };
cardTitleRow.sizeDelta = { x: 200, y: 26 };
cardTemplate.addChild(cardTitleRow);

const cardTitleText = register(new UnityNode('TitleText'));
cardTitleText.anchorMin = { x: 0, y: 0 };
cardTitleText.anchorMax = { x: 0.72, y: 1 };
cardTitleText.sizeDelta = { x: 0, y: 0 };
cardTitleRow.addChild(cardTitleText);

const cardStarsText = register(new UnityNode('StarsText'));
cardStarsText.anchorMin = { x: 0.72, y: 0 };
cardStarsText.anchorMax = { x: 1, y: 1 };
cardStarsText.sizeDelta = { x: 0, y: 0 };
cardTitleRow.addChild(cardStarsText);

const cardDetailText = register(new UnityNode('DetailText'));
cardDetailText.anchorMin = { x: 0, y: 0 };
cardDetailText.anchorMax = { x: 1, y: 0.65 };
cardDetailText.anchoredPosition = { x: 92, y: 8 };
cardDetailText.sizeDelta = { x: 200, y: 40 };
cardTemplate.addChild(cardDetailText);

const cardActionBtn = register(new UnityNode('ActionButton'));
cardActionBtn.anchorMin = { x: 1, y: 0.5 };
cardActionBtn.anchorMax = { x: 1, y: 0.5 };
cardActionBtn.pivot = { x: 1, y: 0.5 };
cardActionBtn.anchoredPosition = { x: -12, y: 0 };
cardActionBtn.sizeDelta = { x: 104, y: 38 };
cardTemplate.addChild(cardActionBtn);

const cardActionText = register(new UnityNode('ActionText'));
cardActionText.anchorMin = { x: 0, y: 0 };
cardActionText.anchorMax = { x: 1, y: 1 };
cardActionText.sizeDelta = { x: 0, y: 0 };
cardActionBtn.addChild(cardActionText);

// ==========================================
// MIDDLE PANEL: NỒI NẤU BẾP LỬA (STOVE)
// ==========================================
const midPanel = register(new UnityNode('MiddlePanel_Stove'));
midPanel.anchorMin = { x: 0.352, y: 0.024 };
midPanel.anchorMax = { x: 0.675, y: 0.900 };
midPanel.anchoredPosition = { x: 0, y: 0 };
midPanel.sizeDelta = { x: 0, y: 0 };
windowNode.addChild(midPanel);

const midTag = register(new UnityNode('HeaderTag'));
midTag.anchorMin = { x: 0.5, y: 1 };
midTag.anchorMax = { x: 0.5, y: 1 };
midTag.pivot = { x: 0.5, y: 0.5 };
midTag.anchoredPosition = { x: 0, y: 0 };
midTag.sizeDelta = { x: 230, y: 36 };
midPanel.addChild(midTag);

const midTagText = register(new UnityNode('TagText'));
midTagText.anchorMin = { x: 0, y: 0 };
midTagText.anchorMax = { x: 1, y: 1 };
midTagText.sizeDelta = { x: 0, y: 0 };
midTag.addChild(midTagText);

// CookingStage
const stage = register(new UnityNode('CookingStage'));
stage.anchorMin = { x: 0.5, y: 1 };
stage.anchorMax = { x: 0.5, y: 1 };
stage.pivot = { x: 0.5, y: 1 };
stage.anchoredPosition = { x: 0, y: -32 };
stage.sizeDelta = { x: 430, y: 160 };
midPanel.addChild(stage);

const potIcon = register(new UnityNode('PotIcon'));
potIcon.anchorMin = { x: 0.5, y: 0.5 };
potIcon.anchorMax = { x: 0.5, y: 0.5 };
potIcon.anchoredPosition = { x: -44, y: 14 };
potIcon.sizeDelta = { x: 82, y: 82 };
stage.addChild(potIcon);

const fireIcon = register(new UnityNode('FireIcon'));
fireIcon.anchorMin = { x: 0.5, y: 0.5 };
fireIcon.anchorMax = { x: 0.5, y: 0.5 };
fireIcon.anchoredPosition = { x: 44, y: 14 };
fireIcon.sizeDelta = { x: 70, y: 70 };
stage.addChild(fireIcon);

const stageSubtitle = register(new UnityNode('StageSubtitle'));
stageSubtitle.anchorMin = { x: 0, y: 0 };
stageSubtitle.anchorMax = { x: 1, y: 0 };
stageSubtitle.pivot = { x: 0.5, y: 0 };
stageSubtitle.anchoredPosition = { x: 0, y: 10 };
stageSubtitle.sizeDelta = { x: 0, y: 30 };
stage.addChild(stageSubtitle);

// PotSlotsSection
const potSlotsSec = register(new UnityNode('PotSlotsSection'));
potSlotsSec.anchorMin = { x: 0.5, y: 1 };
potSlotsSec.anchorMax = { x: 0.5, y: 1 };
potSlotsSec.pivot = { x: 0.5, y: 1 };
potSlotsSec.anchoredPosition = { x: 0, y: -204 };
potSlotsSec.sizeDelta = { x: 430, y: 115 };
midPanel.addChild(potSlotsSec);

const secTitle = register(new UnityNode('SectionTitle'));
secTitle.anchorMin = { x: 0, y: 1 };
secTitle.anchorMax = { x: 1, y: 1 };
secTitle.pivot = { x: 0.5, y: 1 };
secTitle.anchoredPosition = { x: 0, y: 0 };
secTitle.sizeDelta = { x: 0, y: 26 };
potSlotsSec.addChild(secTitle);

const slotsRow = register(new UnityNode('SlotsRow'));
slotsRow.anchorMin = { x: 0, y: 0 };
slotsRow.anchorMax = { x: 1, y: 0 };
slotsRow.pivot = { x: 0.5, y: 0 };
slotsRow.anchoredPosition = { x: 0, y: 0 };
slotsRow.sizeDelta = { x: 0, y: 84 };
potSlotsSec.addChild(slotsRow);

const potSlotsList = [];
for (let i = 0; i < 4; i++) {
  const sNode = register(new UnityNode(`PotSlot_${i}`));
  sNode.sizeDelta = { x: 84, y: 84 };
  slotsRow.addChild(sNode);

  const hl = register(new UnityNode('HighlightBorder'));
  hl.isActive = 0;
  hl.anchorMin = { x: 0, y: 0 };
  hl.anchorMax = { x: 1, y: 1 };
  hl.sizeDelta = { x: 6, y: 6 };
  sNode.addChild(hl);

  const icon = register(new UnityNode('ItemIcon'));
  icon.isActive = 0;
  icon.anchorMin = { x: 0, y: 0 };
  icon.anchorMax = { x: 1, y: 1 };
  icon.sizeDelta = { x: -20, y: -20 };
  sNode.addChild(icon);

  const count = register(new UnityNode('CountText'));
  count.isActive = 0;
  count.anchorMin = { x: 0.4, y: 0 };
  count.anchorMax = { x: 1, y: 0.45 };
  count.anchoredPosition = { x: -4, y: 2 };
  count.sizeDelta = { x: 0, y: 0 };
  sNode.addChild(count);

  potSlotsList.push({ root: sNode, hl, icon, count });
}

// ProgressBarSection
const progSec = register(new UnityNode('ProgressBarSection'));
progSec.anchorMin = { x: 0.5, y: 1 };
progSec.anchorMax = { x: 0.5, y: 1 };
progSec.pivot = { x: 0.5, y: 1 };
progSec.anchoredPosition = { x: 0, y: -328 };
progSec.sizeDelta = { x: 430, y: 60 };
midPanel.addChild(progSec);

const progTitle = register(new UnityNode('ProgressTitle'));
progTitle.anchorMin = { x: 0, y: 1 };
progTitle.anchorMax = { x: 1, y: 1 };
progTitle.pivot = { x: 0.5, y: 1 };
progTitle.anchoredPosition = { x: 0, y: 0 };
progTitle.sizeDelta = { x: 0, y: 22 };
progSec.addChild(progTitle);

const sliderNode = register(new UnityNode('CookingSlider'));
sliderNode.anchorMin = { x: 0, y: 0 };
sliderNode.anchorMax = { x: 1, y: 0 };
sliderNode.pivot = { x: 0.5, y: 0 };
sliderNode.anchoredPosition = { x: 0, y: 0 };
sliderNode.sizeDelta = { x: 0, y: 32 };
progSec.addChild(sliderNode);

const fillArea = register(new UnityNode('Fill Area'));
fillArea.anchorMin = { x: 0, y: 0 };
fillArea.anchorMax = { x: 1, y: 1 };
fillArea.sizeDelta = { x: -6, y: -6 };
sliderNode.addChild(fillArea);

const fill = register(new UnityNode('Fill'));
fill.anchorMin = { x: 0, y: 0 };
fill.anchorMax = { x: 1, y: 1 };
fill.sizeDelta = { x: 0, y: 0 };
fillArea.addChild(fill);

const statusText = register(new UnityNode('StatusText'));
statusText.anchorMin = { x: 0, y: 0 };
statusText.anchorMax = { x: 1, y: 1 };
statusText.sizeDelta = { x: 0, y: 0 };
sliderNode.addChild(statusText);

// OutputPreviewCard
const prevCard = register(new UnityNode('OutputPreviewCard'));
prevCard.anchorMin = { x: 0.5, y: 1 };
prevCard.anchorMax = { x: 0.5, y: 1 };
prevCard.pivot = { x: 0.5, y: 1 };
prevCard.anchoredPosition = { x: 0, y: -400 };
prevCard.sizeDelta = { x: 430, y: 115 };
midPanel.addChild(prevCard);

const prevIconFrame = register(new UnityNode('PreviewIconFrame'));
prevIconFrame.anchorMin = { x: 0, y: 0.5 };
prevIconFrame.anchorMax = { x: 0, y: 0.5 };
prevIconFrame.pivot = { x: 0, y: 0.5 };
prevIconFrame.anchoredPosition = { x: 16, y: 0 };
prevIconFrame.sizeDelta = { x: 80, y: 80 };
prevCard.addChild(prevIconFrame);

const prevDishIcon = register(new UnityNode('DishIcon'));
prevDishIcon.anchorMin = { x: 0, y: 0 };
prevDishIcon.anchorMax = { x: 1, y: 1 };
prevDishIcon.sizeDelta = { x: -16, y: -16 };
prevIconFrame.addChild(prevDishIcon);

const prevInfo = register(new UnityNode('InfoBlock'));
prevInfo.anchorMin = { x: 0, y: 0 };
prevInfo.anchorMax = { x: 1, y: 1 };
prevInfo.anchoredPosition = { x: 110, y: 0 };
prevInfo.sizeDelta = { x: -125, y: -20 };
prevCard.addChild(prevInfo);

const prevHeaderLbl = register(new UnityNode('PreviewHeaderLabel'));
prevHeaderLbl.anchorMin = { x: 0, y: 1 };
prevHeaderLbl.anchorMax = { x: 1, y: 1 };
prevHeaderLbl.pivot = { x: 0, y: 1 };
prevHeaderLbl.anchoredPosition = { x: 0, y: 0 };
prevHeaderLbl.sizeDelta = { x: 0, y: 20 };
prevInfo.addChild(prevHeaderLbl);

const prevDishName = register(new UnityNode('PreviewDishName'));
prevDishName.anchorMin = { x: 0, y: 1 };
prevDishName.anchorMax = { x: 1, y: 1 };
prevDishName.pivot = { x: 0, y: 1 };
prevDishName.anchoredPosition = { x: 0, y: -22 };
prevDishName.sizeDelta = { x: 0, y: 28 };
prevInfo.addChild(prevDishName);

const prevStats = register(new UnityNode('PreviewDishStats'));
prevStats.anchorMin = { x: 0, y: 0 };
prevStats.anchorMax = { x: 1, y: 0 };
prevStats.pivot = { x: 0, y: 0 };
prevStats.anchoredPosition = { x: 0, y: 4 };
prevStats.sizeDelta = { x: 0, y: 24 };
prevInfo.addChild(prevStats);

// ActionButtonsRow
const actRow = register(new UnityNode('ActionButtonsRow'));
actRow.anchorMin = { x: 0.5, y: 0 };
actRow.anchorMax = { x: 0.5, y: 0 };
actRow.pivot = { x: 0.5, y: 0 };
actRow.anchoredPosition = { x: 0, y: 18 };
actRow.sizeDelta = { x: 430, y: 52 };
midPanel.addChild(actRow);

const cookBtn = register(new UnityNode('CookButton'));
actRow.addChild(cookBtn);
const cookBtnText = register(new UnityNode('ActionText'));
cookBtnText.anchorMin = { x: 0, y: 0 };
cookBtnText.anchorMax = { x: 1, y: 1 };
cookBtnText.sizeDelta = { x: 0, y: 0 };
cookBtn.addChild(cookBtnText);

const cancelBtn = register(new UnityNode('CancelButton'));
actRow.addChild(cancelBtn);
const cancelBtnText = register(new UnityNode('ActionText'));
cancelBtnText.anchorMin = { x: 0, y: 0 };
cancelBtnText.anchorMax = { x: 1, y: 1 };
cancelBtnText.sizeDelta = { x: 0, y: 0 };
cancelBtn.addChild(cancelBtnText);

// ==========================================
// RIGHT PANEL: BALO NGUYÊN LIỆU (BACKPACK)
// ==========================================
const rightPanel = register(new UnityNode('RightPanel_Backpack'));
rightPanel.anchorMin = { x: 0.686, y: 0.024 };
rightPanel.anchorMax = { x: 0.984, y: 0.900 };
rightPanel.anchoredPosition = { x: 0, y: 0 };
rightPanel.sizeDelta = { x: 0, y: 0 };
windowNode.addChild(rightPanel);

const rightTag = register(new UnityNode('HeaderTag'));
rightTag.anchorMin = { x: 0.5, y: 1 };
rightTag.anchorMax = { x: 0.5, y: 1 };
rightTag.pivot = { x: 0.5, y: 0.5 };
rightTag.anchoredPosition = { x: 0, y: 0 };
rightTag.sizeDelta = { x: 230, y: 36 };
rightPanel.addChild(rightTag);

const rightTagText = register(new UnityNode('TagText'));
rightTagText.anchorMin = { x: 0, y: 0 };
rightTagText.anchorMax = { x: 1, y: 1 };
rightTagText.sizeDelta = { x: 0, y: 0 };
rightTag.addChild(rightTagText);

const bpSubtitle = register(new UnityNode('BackpackSubtitle'));
bpSubtitle.anchorMin = { x: 0, y: 1 };
bpSubtitle.anchorMax = { x: 1, y: 1 };
bpSubtitle.pivot = { x: 0.5, y: 1 };
bpSubtitle.anchoredPosition = { x: 12, y: -26 };
bpSubtitle.sizeDelta = { x: -24, y: 26 };
rightPanel.addChild(bpSubtitle);

const bpScrollView = register(new UnityNode('BackpackScrollView'));
bpScrollView.anchorMin = { x: 0, y: 0 };
bpScrollView.anchorMax = { x: 1, y: 1 };
bpScrollView.anchoredPosition = { x: 0, y: 10 };
bpScrollView.sizeDelta = { x: -24, y: -120 };
rightPanel.addChild(bpScrollView);

const bpViewport = register(new UnityNode('Viewport'));
bpViewport.anchorMin = { x: 0, y: 0 };
bpViewport.anchorMax = { x: 1, y: 1 };
bpViewport.sizeDelta = { x: 0, y: 0 };
bpScrollView.addChild(bpViewport);

const bpContent = register(new UnityNode('Content'));
bpContent.anchorMin = { x: 0, y: 1 };
bpContent.anchorMax = { x: 1, y: 1 };
bpContent.pivot = { x: 0.5, y: 1 };
bpContent.anchoredPosition = { x: 0, y: 0 };
bpContent.sizeDelta = { x: 0, y: 0 };
bpViewport.addChild(bpContent);

const bpActRow = register(new UnityNode('BackpackActionButtonsRow'));
bpActRow.anchorMin = { x: 0.5, y: 0 };
bpActRow.anchorMax = { x: 0.5, y: 0 };
bpActRow.pivot = { x: 0.5, y: 0 };
bpActRow.anchoredPosition = { x: 0, y: 16 };
bpActRow.sizeDelta = { x: 390, y: 46 };
rightPanel.addChild(bpActRow);

const sortBtn = register(new UnityNode('SortButton'));
bpActRow.addChild(sortBtn);
const sortBtnText = register(new UnityNode('ActionText'));
sortBtnText.anchorMin = { x: 0, y: 0 };
sortBtnText.anchorMax = { x: 1, y: 1 };
sortBtnText.sizeDelta = { x: 0, y: 0 };
sortBtn.addChild(sortBtnText);

const clearBtn = register(new UnityNode('ClearButton'));
bpActRow.addChild(clearBtn);
const clearBtnText = register(new UnityNode('ActionText'));
clearBtnText.anchorMin = { x: 0, y: 0 };
clearBtnText.anchorMax = { x: 1, y: 1 };
clearBtnText.sizeDelta = { x: 0, y: 0 };
clearBtn.addChild(clearBtnText);

// Feedback Toast
const toast = register(new UnityNode('FeedbackToast'));
toast.isActive = 0;
toast.anchorMin = { x: 0.5, y: 1 };
toast.anchorMax = { x: 0.5, y: 1 };
toast.pivot = { x: 0.5, y: 1 };
toast.anchoredPosition = { x: 0, y: -25 };
toast.sizeDelta = { x: 560, y: 46 };
overlay.addChild(toast);

const toastText = register(new UnityNode('ToastText'));
toastText.anchorMin = { x: 0, y: 0 };
toastText.anchorMax = { x: 1, y: 1 };
toastText.sizeDelta = { x: -20, y: 0 };
toast.addChild(toastText);

console.log(`Generated ${allNodes.length} nodes in hierarchy.`);

// Generate YAML blocks
const out = [];

function emit(str) {
  out.push(str);
}

// 1. Root GameObject
emit(`--- !u!1 &${kitchenCanvas.goId}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: ${kitchenCanvas.rtId}}
  - component: {fileID: ${kitchenCanvas.canvasId = genId()}}
  - component: {fileID: ${kitchenCanvas.scalerId = genId()}}
  - component: {fileID: ${kitchenCanvas.raycasterId = genId()}}
  - component: {fileID: ${kitchenCanvas.uiId = genId()}}
  m_Layer: 5
  m_Name: KitchenCanvas
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &${kitchenCanvas.rtId}
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${kitchenCanvas.goId}}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 0, y: 0, z: 0}
  m_ConstrainProportionsScale: 0
  m_Children:
  - {fileID: ${overlay.rtId}}
  m_Father: {fileID: 0}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 0}
  m_AnchorMax: {x: 0, y: 0}
  m_AnchoredPosition: {x: 0, y: 0}
  m_SizeDelta: {x: 0, y: 0}
  m_Pivot: {x: 0, y: 0}
--- !u!223 &${kitchenCanvas.canvasId}
Canvas:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${kitchenCanvas.goId}}
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
  m_SortingOrder: 20
  m_TargetDisplay: 0
--- !u!114 &${kitchenCanvas.scalerId}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${kitchenCanvas.goId}}
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
--- !u!114 &${kitchenCanvas.raycasterId}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${kitchenCanvas.goId}}
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

// Pre-assign component IDs for all other nodes
for (const n of allNodes) {
  if (n === kitchenCanvas) continue;
  n.crId = genId(); // CanvasRenderer
  n.imgId = null;
  n.tmpId = null;
  n.btnId = null;
  n.sliderId = null;
  n.scrollId = null;
  n.hlgId = null;
  n.vlgId = null;
  n.glgId = null;
  n.csfId = null;
  n.maskId = null;
  n.cardUIId = null;

  // Assign component IDs based on name / type
  if (n.name.includes('Title') || n.name.includes('Text') || n.name.includes('Subtitle') || n.name.includes('Label') || n.name.includes('Stats') || n.name.includes('Name')) {
    n.tmpId = genId();
  } else {
    n.imgId = genId();
  }

  if (n.name.includes('Button') || n.name.includes('Tab_') || n.name.includes('PotSlot_')) {
    n.btnId = genId();
  }

  if (n.name === 'CookingSlider') {
    n.sliderId = genId();
    n.imgId = genId(); // track
  }

  if (n.name.includes('ScrollView')) {
    n.scrollId = genId();
  }

  if (n.name === 'Viewport') {
    n.maskId = genId();
  }

  if (n.name === 'FilterTabBar' || n.name === 'SlotsRow' || n.name.includes('ActionButtonsRow')) {
    n.hlgId = genId();
  }

  if (n.name === 'Content' && n.father.name === 'Viewport' && n.father.father.name === 'RecipeScrollView') {
    n.vlgId = genId();
    n.csfId = genId();
  }

  if (n.name === 'Content' && n.father.name === 'Viewport' && n.father.father.name === 'BackpackScrollView') {
    n.glgId = genId();
    n.csfId = genId();
  }

  if (n.name === 'RecipeCardTemplate') {
    n.cardUIId = genId();
  }
}

// KitchenUI Component on Root Canvas
emit(`--- !u!114 &${kitchenCanvas.uiId}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${kitchenCanvas.goId}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${GUID_KITCHEN_UI}, type: 3}
  m_Name: 
  m_EditorClassIdentifier: Assembly-CSharp::KitchenUI
  canvas: {fileID: ${kitchenCanvas.canvasId}}
  kitchenPanel: {fileID: ${overlay.goId}}
  windowRect: {fileID: ${windowNode.rtId}}
  titleText: {fileID: ${headerTitle.tmpId}}
  closeButton: {fileID: ${closeBtn.btnId}}
  tabAllBtn: {fileID: ${tabAll.tab.btnId}}
  tabRecoveryBtn: {fileID: ${tabRec.tab.btnId}}
  tabSpeedBtn: {fileID: ${tabSpd.tab.btnId}}
  tabOtherBtn: {fileID: ${tabOth.tab.btnId}}
  tabAllText: {fileID: ${tabAll.label.tmpId}}
  tabRecoveryText: {fileID: ${tabRec.label.tmpId}}
  tabSpeedText: {fileID: ${tabSpd.label.tmpId}}
  tabOtherText: {fileID: ${tabOth.label.tmpId}}
  recipeScrollRect: {fileID: ${rScrollView.scrollId}}
  recipeContent: {fileID: ${rContent.rtId}}
  recipeCardTemplate: {fileID: ${cardTemplate.goId}}
  stoveHeaderTag: {fileID: ${midTagText.tmpId}}
  stovePotIcon: {fileID: ${potIcon.imgId}}
  stoveFireIcon: {fileID: ${fireIcon.imgId}}
  stoveSubtitleText: {fileID: ${stageSubtitle.tmpId}}
  potSlotUIs:
  - rootObject: {fileID: ${potSlotsList[0].root.goId}}
    slotButton: {fileID: ${potSlotsList[0].root.btnId}}
    slotBackground: {fileID: ${potSlotsList[0].root.imgId}}
    itemIcon: {fileID: ${potSlotsList[0].icon.imgId}}
    countText: {fileID: ${potSlotsList[0].count.tmpId}}
    highlightBorder: {fileID: ${potSlotsList[0].hl.goId}}
  - rootObject: {fileID: ${potSlotsList[1].root.goId}}
    slotButton: {fileID: ${potSlotsList[1].root.btnId}}
    slotBackground: {fileID: ${potSlotsList[1].root.imgId}}
    itemIcon: {fileID: ${potSlotsList[1].icon.imgId}}
    countText: {fileID: ${potSlotsList[1].count.tmpId}}
    highlightBorder: {fileID: ${potSlotsList[1].hl.goId}}
  - rootObject: {fileID: ${potSlotsList[2].root.goId}}
    slotButton: {fileID: ${potSlotsList[2].root.btnId}}
    slotBackground: {fileID: ${potSlotsList[2].root.imgId}}
    itemIcon: {fileID: ${potSlotsList[2].icon.imgId}}
    countText: {fileID: ${potSlotsList[2].count.tmpId}}
    highlightBorder: {fileID: ${potSlotsList[2].hl.goId}}
  - rootObject: {fileID: ${potSlotsList[3].root.goId}}
    slotButton: {fileID: ${potSlotsList[3].root.btnId}}
    slotBackground: {fileID: ${potSlotsList[3].root.imgId}}
    itemIcon: {fileID: ${potSlotsList[3].icon.imgId}}
    countText: {fileID: ${potSlotsList[3].count.tmpId}}
    highlightBorder: {fileID: ${potSlotsList[3].hl.goId}}
  progressLabelText: {fileID: ${progTitle.tmpId}}
  progressSlider: {fileID: ${sliderNode.sliderId}}
  progressFillImage: {fileID: ${fill.imgId}}
  progressStatusText: {fileID: ${statusText.tmpId}}
  previewCardGO: {fileID: ${prevCard.goId}}
  previewFoodIcon: {fileID: ${prevDishIcon.imgId}}
  previewHeaderLabel: {fileID: ${prevHeaderLbl.tmpId}}
  previewFoodTitle: {fileID: ${prevDishName.tmpId}}
  previewFoodStats: {fileID: ${prevStats.tmpId}}
  cookButton: {fileID: ${cookBtn.btnId}}
  cancelPotButton: {fileID: ${cancelBtn.btnId}}
  cookButtonText: {fileID: ${cookBtnText.tmpId}}
  backpackHeaderTag: {fileID: ${rightTagText.tmpId}}
  backpackSubtitleText: {fileID: ${bpSubtitle.tmpId}}
  backpackScrollRect: {fileID: ${bpScrollView.scrollId}}
  backpackGridContent: {fileID: ${bpContent.rtId}}
  backpackSlotPrefab: {fileID: 3631650896398165525, guid: ${GUID_INVENTORY_SLOT_PREFAB}, type: 3}
  sortBackpackButton: {fileID: ${sortBtn.btnId}}
  clearSlotsButton: {fileID: ${clearBtn.btnId}}
  toastObject: {fileID: ${toast.goId}}
  feedbackToastText: {fileID: ${toastText.tmpId}}`);

// Emit GameObjects & Components for all children
for (const n of allNodes) {
  if (n === kitchenCanvas) continue;

  const compList = [`  - component: {fileID: ${n.rtId}}`];
  if (n.crId) compList.push(`  - component: {fileID: ${n.crId}}`);
  if (n.imgId) compList.push(`  - component: {fileID: ${n.imgId}}`);
  if (n.tmpId) compList.push(`  - component: {fileID: ${n.tmpId}}`);
  if (n.btnId) compList.push(`  - component: {fileID: ${n.btnId}}`);
  if (n.sliderId) compList.push(`  - component: {fileID: ${n.sliderId}}`);
  if (n.scrollId) compList.push(`  - component: {fileID: ${n.scrollId}}`);
  if (n.maskId) compList.push(`  - component: {fileID: ${n.maskId}}`);
  if (n.hlgId) compList.push(`  - component: {fileID: ${n.hlgId}}`);
  if (n.vlgId) compList.push(`  - component: {fileID: ${n.vlgId}}`);
  if (n.glgId) compList.push(`  - component: {fileID: ${n.glgId}}`);
  if (n.csfId) compList.push(`  - component: {fileID: ${n.csfId}}`);
  if (n.cardUIId) compList.push(`  - component: {fileID: ${n.cardUIId}}`);

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
  m_IsActive: ${n.isActive}
--- !u!224 &${n.rtId}
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${n.goId}}
  m_LocalRotation: {x: ${n.localRotation.x}, y: ${n.localRotation.y}, z: ${n.localRotation.z}, w: ${n.localRotation.w}}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: ${n.localScale.x}, y: ${n.localScale.y}, z: ${n.localScale.z}}
  m_ConstrainProportionsScale: 0
  m_Children:
${n.children.map(c => `  - {fileID: ${c.rtId}}`).join('\n') || '  []'}
  m_Father: {fileID: ${n.father ? n.father.rtId : 0}}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: ${n.anchorMin.x}, y: ${n.anchorMin.y}}
  m_AnchorMax: {x: ${n.anchorMax.x}, y: ${n.anchorMax.y}}
  m_AnchoredPosition: {x: ${n.anchoredPosition.x}, y: ${n.anchoredPosition.y}}
  m_SizeDelta: {x: ${n.sizeDelta.x}, y: ${n.sizeDelta.y}}
  m_Pivot: {x: ${n.pivot.x}, y: ${n.pivot.y}}`);

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

  if (n.imgId) {
    let spriteRef = '{fileID: 0}';
    let type = 0;
    let r = 1, g = 1, b = 1, a = 1;

    if (n.name === 'KitchenOverlay') { r = 0.08; g = 0.05; b = 0.04; a = 0.82; }
    else if (n.name === 'KitchenWindow') {
      r = 0.24; g = 0.14; b = 0.09; a = 1;
      spriteRef = `{fileID: ${SPRITE_FRAME}, guid: ${GUID_FRAME}, type: 3}`;
      type = 1;
    }
    else if (n.name === 'InnerBorder') { r = 0.16; g = 0.09; b = 0.06; a = 0.95; }
    else if (n.name === 'TopHeaderBar' || n.name === 'HeaderTag' || n.name === 'FeedbackToast') {
      spriteRef = `{fileID: ${SPRITE_HEADER_PLAQUE}, guid: ${GUID_HEADER_PLAQUE}, type: 3}`;
      type = 1;
      r = 0.38; g = 0.23; b = 0.15;
    }
    else if (n.name.includes('Panel')) { r = 0.98; g = 0.95; b = 0.92; a = 1; }
    else if (n.name === 'CookingStage') { r = 0.24; g = 0.14; b = 0.09; a = 1; }
    else if (n.name.includes('PotSlot_')) {
      spriteRef = `{fileID: ${SPRITE_PREVIEW_ICON_FRAME}, guid: ${GUID_PREVIEW_ICON_FRAME}, type: 3}`;
      type = 1;
      r = 0.96; g = 0.93; b = 0.87;
    }
    else if (n.name === 'HighlightBorder') { r = 0.24; g = 0.60; b = 0.96; a = 0.85; }
    else if (n.name === 'OutputPreviewCard') {
      spriteRef = `{fileID: ${SPRITE_PREVIEW_BOX}, guid: ${GUID_PREVIEW_BOX}, type: 3}`;
      type = 1;
      r = 1; g = 0.98; b = 0.92;
    }
    else if (n.name === 'PreviewIconFrame') {
      spriteRef = `{fileID: ${SPRITE_PREVIEW_ICON_FRAME}, guid: ${GUID_PREVIEW_ICON_FRAME}, type: 3}`;
      type = 1;
    }
    else if (n.name === 'IconFrame') {
      spriteRef = `{fileID: ${SPRITE_CARD_ICON_FRAME}, guid: ${GUID_CARD_ICON_FRAME}, type: 3}`;
      type = 1;
    }
    else if (n.name === 'RecipeCardTemplate') {
      spriteRef = `{fileID: ${SPRITE_CARD_BG}, guid: ${GUID_CARD_BG}, type: 3}`;
      type = 1;
    }
    else if (n.name === 'Tab_All') { r = 0.36; g = 0.70; b = 0.16; }
    else if (n.name.includes('Tab_')) { r = 0.92; g = 0.86; b = 0.78; }
    else if (n.name === 'CookButton') { r = 0.32; g = 0.65; b = 0.16; }
    else if (n.name === 'CancelButton') { r = 0.82; g = 0.22; b = 0.22; }
    else if (n.name === 'SortButton') { r = 0.68; g = 0.45; b = 0.16; }
    else if (n.name === 'ClearButton') { r = 0.94; g = 0.90; b = 0.84; }
    else if (n.name === 'ActionButton') { r = 0.48; g = 0.32; b = 0.22; }
    else if (n.name === 'CookingSlider') { r = 0.25; g = 0.16; b = 0.10; }
    else if (n.name === 'Fill') { r = 1; g = 0.58; b = 0.06; }
    else if (n.name === 'CloseButton') { r = 0.24; g = 0.14; b = 0.08; a = 0.75; }

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
  m_Color: {r: ${r}, g: ${g}, b: ${b}, a: ${a}}
  m_RaycastTarget: 1
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: ${spriteRef}
  m_Type: ${type}
  m_PreserveAspect: 0
  m_FillCenter: 1
  m_FillMethod: 4
  m_FillAmount: 1
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseSpriteMesh: 0
  m_PixelsPerUnitMultiplier: 1`);
  }

  if (n.tmpId) {
    let text = 'Text';
    let size = 16;
    let bold = 1;
    let align = 514; // Midline Left
    let cr = 0.24, cg = 0.14, cb = 0.08, ca = 1;

    if (n.name === 'HeaderTitle') {
      text = '🍲 BẾP NẤU GIA ĐÌNH & SỔ TAY CÔNG THỨC ẨM THỰC';
      size = 22; cr = 1; cg = 0.95; cb = 0.88;
    } else if (n.name === 'CloseText') {
      text = '✕ [ESC] Đóng Bếp';
      size = 17; align = 514; cr = 1; cg = 0.72; cb = 0.22;
    } else if (n.name === 'TagText') {
      text = n.father.father.name.includes('Middle') ? 'NỒI NẤU BẾP LỬA' : 'BALO NGUYÊN LIỆU';
      size = 15; align = 514; cr = 1; cg = 0.96; cb = 0.88;
    } else if (n.name === 'StageSubtitle') {
      text = 'Bếp Lửa Sôi Sùng Sục • Sẵn Sàng Chế Biến';
      size = 15; align = 514; cr = 1; cg = 0.90; cb = 0.55;
    } else if (n.name === 'SectionTitle') {
      text = 'BỎ NGUYÊN LIỆU VÀO 4 Ô DƯỚI ĐÂY:';
      size = 15; cr = 0.32; cg = 0.20; cb = 0.14;
    } else if (n.name === 'CountText') {
      text = 'x1'; size = 15; align = 1028; cr = 0; cg = 0; cb = 0;
    } else if (n.name === 'ProgressTitle') {
      text = '⏱️ TIẾN TRÌNH NẤU (5 GIÂY) ⏱️';
      size = 14; align = 514; cr = 0.24; cg = 0.44; cb = 0.64;
    } else if (n.name === 'StatusText') {
      text = 'SẴN SÀNG'; size = 14; align = 514; cr = 1; cg = 1; cb = 1;
    } else if (n.name === 'PreviewHeaderLabel') {
      text = 'MÓN THÀNH PHẨM DỰ KIẾN:';
      size = 13; cr = 0.48; cg = 0.32; cb = 0.22;
    } else if (n.name === 'PreviewDishName') {
      text = 'Súp Cà Chua Hương Thảo';
      size = 19; cr = 0.24; cg = 0.14; cb = 0.08;
    } else if (n.name === 'PreviewDishStats') {
      text = '❤️ +45 HP | 🍗 +60 Độ No | ⚡ +10% Tốc Độ';
      size = 13; cr = 0.35; cg = 0.25; cb = 0.20;
    } else if (n.name === 'BackpackSubtitle') {
      text = 'Kéo thả nguyên liệu từ Balo vào 4 ô nấu ăn:';
      size = 14; cr = 0.35; cg = 0.22; cb = 0.16;
    } else if (n.name === 'TitleText') {
      text = 'Súp Cà Chua Đậm Đà'; size = 16; cr = 0.24; cg = 0.14; cb = 0.08;
    } else if (n.name === 'StarsText') {
      text = '⭐⭐⭐'; size = 14; align = 516; cr = 1; cg = 0.78; cb = 0.18;
    } else if (n.name === 'DetailText') {
      text = 'Hồi 45 HP & 60 Độ No | Cần: Cà Chua x2 + Nước x1';
      size = 13; cr = 0.42; cg = 0.30; cb = 0.22; bold = 0;
    } else if (n.name === 'ToastText') {
      text = 'Đã nấu thành công món ăn!';
      size = 16; align = 514; cr = 1; cg = 0.95; cb = 0.85;
    } else if (n.father.name === 'CookButton') {
      text = '🔥 BẮT ĐẦU NẤU'; size = 18; align = 514; cr = 1; cg = 1; cb = 1;
    } else if (n.father.name === 'CancelButton') {
      text = '✕ HỦY NGUYÊN LIỆU'; size = 18; align = 514; cr = 1; cg = 1; cb = 1;
    } else if (n.father.name === 'SortButton') {
      text = '⚡ SẮP XẾP TÚI'; size = 15; align = 514; cr = 1; cg = 1; cb = 1;
    } else if (n.father.name === 'ClearButton') {
      text = '🧹 LÀM TRỐNG Ô'; size = 15; align = 514; cr = 0.36; cg = 0.22; cb = 0.14;
    } else if (n.father.name === 'ActionButton') {
      text = 'TỰ ĐIỀN'; size = 14; align = 514; cr = 1; cg = 1; cb = 1;
    } else if (n.father.name === 'Tab_All') {
      text = 'TẤT CẢ (12)'; size = 14; align = 514; cr = 1; cg = 1; cb = 1;
    } else if (n.father.name === 'Tab_Recovery') {
      text = 'HỒI PHỤC'; size = 14; align = 514; cr = 0.36; cg = 0.25; cb = 0.20;
    } else if (n.father.name === 'Tab_Speed') {
      text = 'TĂNG TỐC'; size = 14; align = 514; cr = 0.36; cg = 0.25; cb = 0.20;
    } else if (n.father.name === 'Tab_Other') {
      text = 'KHÁC'; size = 14; align = 514; cr = 0.36; cg = 0.25; cb = 0.20;
    }

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
  m_RaycastTarget: 1
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_text: "${text}"
  m_isRightToLeft: 0
  m_fontAsset: {fileID: 11400000, guid: ${GUID_FONT}, type: 2}
  m_sharedMaterial: {fileID: 0}
  m_fontSharedMaterials: []
  m_fontMaterial: {fileID: 0}
  m_fontMaterials: []
  m_fontColor32:
    serializedVersion: 2
    rgba: 4294967295
  m_fontColor: {r: ${cr}, g: ${cg}, b: ${cb}, a: ${ca}}
  m_enableVertexGradient: 0
  m_colorMode: 3
  m_fontColorGradient:
    topLeft: {r: 1, g: 1, b: 1, a: 1}
    topRight: {r: 1, g: 1, b: 1, a: 1}
    bottomLeft: {r: 1, g: 1, b: 1, a: 1}
    bottomRight: {r: 1, g: 1, b: 1, a: 1}
  m_fontColorGradientPreset: {fileID: 0}
  m_spriteAsset: {fileID: 0}
  m_tintAllSprites: 0
  m_StyleSheet: {fileID: 0}
  m_TextStyleHashCode: -1183493901
  m_overrideHtmlColors: 0
  m_faceColor:
    serializedVersion: 2
    rgba: 4294967295
  m_fontSize: ${size}
  m_fontSizeBase: ${size}
  m_fontWeight: 400
  m_enableAutoSizing: 0
  m_fontSizeMin: 10
  m_fontSizeMax: 72
  m_fontStyle: ${bold ? 1 : 0}
  m_HorizontalAlignment: 1
  m_VerticalAlignment: 256
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

  if (n.btnId) {
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
  m_TargetGraphic: {fileID: ${n.imgId || 0}}
  m_OnClick:
    m_PersistentCalls:
      m_Calls: []`);
  }

  if (n.sliderId) {
    emit(`--- !u!114 &${n.sliderId}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${n.goId}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${GUID_SLIDER}, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Slider
  m_Navigation:
    m_Mode: 0
  m_Transition: 0
  m_Colors:
    m_NormalColor: {r: 1, g: 1, b: 1, a: 1}
  m_Interactable: 0
  m_TargetGraphic: {fileID: 0}
  m_FillRect: {fileID: ${fill.rtId}}
  m_HandleRect: {fileID: 0}
  m_Direction: 0
  m_MinValue: 0
  m_MaxValue: 1
  m_WholeNumbers: 0
  m_Value: 0
  m_OnValueChanged:
    m_PersistentCalls:
      m_Calls: []`);
  }

  if (n.scrollId) {
    const isRecipe = n.name.includes('Recipe');
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
  m_Content: {fileID: ${isRecipe ? rContent.rtId : bpContent.rtId}}
  m_Horizontal: 0
  m_Vertical: 1
  m_MovementType: 1
  m_Elasticity: 0.1
  m_Inertia: 1
  m_DecelerationRate: 0.135
  m_ScrollSensitivity: 30
  m_Viewport: {fileID: ${isRecipe ? rViewport.rtId : bpViewport.rtId}}
  m_HorizontalScrollbar: {fileID: 0}
  m_VerticalScrollbar: {fileID: 0}
  m_HorizontalScrollbarVisibility: 2
  m_VerticalScrollbarVisibility: 2
  m_HorizontalScrollbarSpacing: 0
  m_VerticalScrollbarSpacing: 0
  m_OnValueChanged:
    m_PersistentCalls:
      m_Calls: []`);
  }

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

  if (n.hlgId) {
    const spacing = n.name === 'FilterTabBar' ? 6 : (n.name === 'SlotsRow' ? 14 : 14);
    emit(`--- !u!114 &${n.hlgId}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${n.goId}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${GUID_HLG}, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.HorizontalLayoutGroup
  m_Padding:
    m_Left: 0
    m_Right: 0
    m_Top: 0
    m_Bottom: 0
  m_ChildAlignment: 4
  m_Spacing: ${spacing}
  m_ChildForceExpandWidth: ${n.name === 'SlotsRow' ? 0 : 1}
  m_ChildForceExpandHeight: ${n.name === 'SlotsRow' ? 0 : 1}
  m_ChildControlWidth: ${n.name === 'SlotsRow' ? 0 : 1}
  m_ChildControlHeight: ${n.name === 'SlotsRow' ? 0 : 1}
  m_ChildScaleWidth: 0
  m_ChildScaleHeight: 0
  m_ReverseArrangement: 0`);
  }

  if (n.vlgId) {
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
    m_Left: 0
    m_Right: 0
    m_Top: 0
    m_Bottom: 0
  m_ChildAlignment: 0
  m_Spacing: 8
  m_ChildForceExpandWidth: 1
  m_ChildForceExpandHeight: 0
  m_ChildControlWidth: 1
  m_ChildControlHeight: 1
  m_ChildScaleWidth: 0
  m_ChildScaleHeight: 0
  m_ReverseArrangement: 0`);
  }

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
  m_CellSize: {x: 66, y: 66}
  m_Spacing: {x: 8, y: 8}
  m_StartCorner: 0
  m_StartAxis: 0
  m_ChildAlignment: 0
  m_Constraint: 1
  m_ConstraintCount: 5`);
  }

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

  if (n.cardUIId) {
    emit(`--- !u!114 &${n.cardUIId}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${n.goId}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${GUID_RECIPE_CARD_UI}, type: 3}
  m_Name: 
  m_EditorClassIdentifier: Assembly-CSharp::RecipeCardUI
  cardBackground: {fileID: ${cardTemplate.imgId}}
  iconFrame: {fileID: ${cardIconFrame.imgId}}
  dishIcon: {fileID: ${cardDishIcon.imgId}}
  titleText: {fileID: ${cardTitleText.tmpId}}
  starsText: {fileID: ${cardStarsText.tmpId}}
  detailText: {fileID: ${cardDetailText.tmpId}}
  actionButton: {fileID: ${cardActionBtn.btnId}}
  actionButtonBackground: {fileID: ${cardActionBtn.imgId}}
  actionButtonText: {fileID: ${cardActionText.tmpId}}
  lockIcon: {fileID: 0}`);
  }
}

const fullYaml = `%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n` + out.join('\n') + '\n';

// 1. Write Prefab
const prefabPath = 'Assets/_Assets/_Prefabs/KitchenCanvas.prefab';
fs.writeFileSync(prefabPath, fullYaml, 'utf8');
console.log(`Saved ${prefabPath} (${fullYaml.length} bytes).`);

// 2. Insert into Base.unity scene!
const scenePath = 'Assets/_Scenes/Base.unity';
let sceneContent = fs.readFileSync(scenePath, 'utf8');

// Check if KitchenCanvas is already in Base.unity
if (sceneContent.includes('m_Name: KitchenCanvas')) {
  console.log('KitchenCanvas already in Base.unity, updating...');
  // Find where KitchenCanvas starts and remove it
  const kStart = sceneContent.indexOf(`m_Name: KitchenCanvas`);
  const blockStart = sceneContent.lastIndexOf('--- !u!1 &', kStart);
  // Find the end or remove
  // For safety, replace old block
}

// Append the new blocks to Base.unity
// In Unity scenes, GameObjects are placed as YAML documents separated by '--- !u!...'
// We append the body (without the header %YAML ...) to the end of Base.unity!
const sceneBlocks = out.join('\n') + '\n';

// Ensure the scene doesn't already have duplicate KitchenCanvas
if (!sceneContent.includes('m_Name: KitchenCanvas')) {
  fs.writeFileSync(scenePath, sceneContent.trimEnd() + '\n' + sceneBlocks, 'utf8');
  console.log(`Inserted KitchenCanvas directly into ${scenePath}!`);
} else {
  console.log(`KitchenCanvas was already present in ${scenePath}`);
}
