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
const GUID_HLG = '30649d3a9faa99c48a7b1166b86bf2a0';
const GUID_VLG = '59f8146938fff824cb5fd77236b75775';
const GUID_GLG = '8a8695521f0d02e499659fee002a26c2';
const GUID_CSF = '3245ec927659c4140ac4f8d17403cc18';
const GUID_RECTMASK2D = '3312d7739989d2b4e91e6319e9a96d76';
const GUID_KITCHEN_UI = '87a8dbca7a7a55e4d86f29d69d88d0f4';
const GUID_RECIPE_CARD_UI = '92c738ef1234567890abcdef01234567';

// Asset GUIDs
const GUID_FONT = '8f586378b4e144a9851e7b34d9b748ee';
const GUID_UI_FRAME = 'e072dcd7c46051040acb6c783602ba8b'; // UI_Frame.png (sliced pixel border 14)
const SPRITE_UI_FRAME = '21300000';
const GUID_UI_SLOT = 'e9725353e3629f046916cf6fcc7048ee'; // UI_Slot.png
const SPRITE_UI_SLOT = '21300000';
const GUID_HEADER_PLAQUE = '719e2e4a28cd1184ab3762dbc4850a4d'; // Header Plaque.png
const SPRITE_HEADER_PLAQUE = '-586511307516372644';
const GUID_CARD_BG = 'dd9c5a3b5d132ea46acdbf6623b45633'; // Món ăn (không chọn).png
const SPRITE_CARD_BG = '-852011609251132011';
const GUID_PREVIEW_BOX = '761ec30b1713dab49a87b858dc16d4ea'; // Món ăn dự kiến.png
const SPRITE_PREVIEW_BOX = '2719065537576319748';
const GUID_PREVIEW_ICON_FRAME = '07a88cfd7508dbf4381f96a64f1e4ce9'; // Ảnh món ăn dự kiến.png
const SPRITE_PREVIEW_ICON_FRAME = '-6235862569765844502';
const GUID_CARD_ICON_FRAME = '7c86d342e0c39c840b2c976a90d5a47c'; // Ảnh món ăn.png
const SPRITE_CARD_ICON_FRAME = '-8094992848256881038';
const GUID_SOUP_ICON = '6e4e39d5b64163843a96a334091b2e80'; // 01_tomato_soup-removebg-preview
const SPRITE_SOUP_ICON = '1226134801195689498';
const GUID_CAMPFIRE = 'c550f469f9e01234a9b12f1c3606b4b6';
const SPRITE_CAMPFIRE_1 = '-780171293';
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
    this.isActive = 1;
    this.layer = 5; // UI layer
    
    // RectTransform defaults
    this.anchorMin = { x: 0.5, y: 0.5 };
    this.anchorMax = { x: 0.5, y: 0.5 };
    this.pivot = { x: 0.5, y: 0.5 };
    this.anchoredPosition = { x: 0, y: 0 };
    this.sizeDelta = { x: 100, y: 100 };
    this.offsetMin = null;
    this.offsetMax = null;
    this.localRotation = { x: 0, y: 0, z: 0, w: 1 };
    this.localScale = { x: 1, y: 1, z: 1 };

    // Component IDs
    this.crId = null;
    this.imgId = null;
    this.tmpId = null;
    this.btnId = null;
    this.sliderId = null;
    this.scrollId = null;
    this.hlgId = null;
    this.vlgId = null;
    this.glgId = null;
    this.csfId = null;
    this.maskId = null;
    this.cardUIId = null;
  }

  addChild(child) {
    if (!this.children.includes(child)) {
      child.father = this;
      this.children.push(child);
    }
    return child;
  }
}

// Global registry
const allNodes = [];
function register(node) {
  allNodes.push(node);
  return node;
}

// ==========================================
// 1. ROOT CANVAS & OVERLAY
// ==========================================
const kitchenCanvas = register(new UnityNode('KitchenCanvas', true));
kitchenCanvas.isActive = 1;

// Fullscreen dark vignette overlay
const overlay = register(new UnityNode('KitchenOverlay'));
overlay.isActive = 0; // Starts closed
overlay.anchorMin = { x: 0, y: 0 };
overlay.anchorMax = { x: 1, y: 1 };
overlay.offsetMin = { x: 0, y: 0 };
overlay.offsetMax = { x: 0, y: 0 };
overlay.sizeDelta = { x: 0, y: 0 };
kitchenCanvas.addChild(overlay);

// ==========================================
// 2. MAIN WINDOW (1260 x 700)
// ==========================================
const windowNode = register(new UnityNode('KitchenWindow'));
windowNode.anchorMin = { x: 0.5, y: 0.5 };
windowNode.anchorMax = { x: 0.5, y: 0.5 };
windowNode.pivot = { x: 0.5, y: 0.5 };
windowNode.anchoredPosition = { x: 0, y: 0 };
windowNode.sizeDelta = { x: 1260, y: 700 };
overlay.addChild(windowNode);

// Inner Border backdrop
const innerBorder = register(new UnityNode('InnerBorder'));
innerBorder.anchorMin = { x: 0, y: 0 };
innerBorder.anchorMax = { x: 1, y: 1 };
innerBorder.offsetMin = { x: 8, y: 8 };
innerBorder.offsetMax = { x: -8, y: -8 };
innerBorder.sizeDelta = { x: -16, y: -16 };
windowNode.addChild(innerBorder);

// Top Header Bar
const header = register(new UnityNode('TopHeaderBar'));
header.anchorMin = { x: 0, y: 1 };
header.anchorMax = { x: 1, y: 1 };
header.pivot = { x: 0.5, y: 1 };
header.anchoredPosition = { x: 0, y: -12 };
header.sizeDelta = { x: -28, y: 46 };
windowNode.addChild(header);

const headerTitle = register(new UnityNode('HeaderTitle'));
headerTitle.anchorMin = { x: 0, y: 0 };
headerTitle.anchorMax = { x: 0.75, y: 1 };
headerTitle.pivot = { x: 0, y: 0.5 };
headerTitle.anchoredPosition = { x: 20, y: 0 };
headerTitle.sizeDelta = { x: -20, y: 0 };
header.addChild(headerTitle);

const closeBtn = register(new UnityNode('CloseButton'));
closeBtn.anchorMin = { x: 1, y: 0.5 };
closeBtn.anchorMax = { x: 1, y: 0.5 };
closeBtn.pivot = { x: 1, y: 0.5 };
closeBtn.anchoredPosition = { x: -12, y: 0 };
closeBtn.sizeDelta = { x: 140, y: 34 };
header.addChild(closeBtn);

const closeBtnText = register(new UnityNode('CloseText'));
closeBtnText.anchorMin = { x: 0, y: 0 };
closeBtnText.anchorMax = { x: 1, y: 1 };
closeBtnText.sizeDelta = { x: 0, y: 0 };
closeBtn.addChild(closeBtnText);

// ==========================================
// 3. THREE BALANCED COLUMNS (Height 612px)
// Total Window: 1260px
// Margins: 14px left, 14px right
// Gap: 10px between columns
// Left: 370px, Middle: 482px, Right: 370px (370 + 10 + 482 + 10 + 370 + 28 = 1270, adjust to 365 + 474 + 365 = 1204 + 56 = 1260)
// Let's use:
// Left: 366px (anchorMin 0, anchorMax 0, pivot 0, 0.5, pos: 14, -36, size: 366, 610)
// Mid:  480px (anchorMin 0.5, anchorMax 0.5, pivot 0.5, 0.5, pos: 0, -36, size: 480, 610)
// Right: 366px (anchorMin 1, anchorMax 1, pivot 1, 0.5, pos: -14, -36, size: 366, 610)
// ==========================================

// --- COLUMN 1: LEFT PANEL (SỔ TAY CÔNG THỨC) ---
const leftPanel = register(new UnityNode('LeftPanel_Recipes'));
leftPanel.anchorMin = { x: 0, y: 0 };
leftPanel.anchorMax = { x: 0, y: 1 };
leftPanel.pivot = { x: 0, y: 0.5 };
leftPanel.anchoredPosition = { x: 14, y: -35 };
leftPanel.sizeDelta = { x: 366, y: -94 };
windowNode.addChild(leftPanel);

const leftTag = register(new UnityNode('LeftHeaderTag'));
leftTag.anchorMin = { x: 0.5, y: 1 };
leftTag.anchorMax = { x: 0.5, y: 1 };
leftTag.pivot = { x: 0.5, y: 0.5 };
leftTag.anchoredPosition = { x: 0, y: 0 };
leftTag.sizeDelta = { x: 210, y: 30 };
leftPanel.addChild(leftTag);

const leftTagText = register(new UnityNode('LeftTagText'));
leftTagText.anchorMin = { x: 0, y: 0 };
leftTagText.anchorMax = { x: 1, y: 1 };
leftTagText.sizeDelta = { x: 0, y: 0 };
leftTag.addChild(leftTagText);

const filterTabBar = register(new UnityNode('FilterTabBar'));
filterTabBar.anchorMin = { x: 0, y: 1 };
filterTabBar.anchorMax = { x: 1, y: 1 };
filterTabBar.pivot = { x: 0.5, y: 1 };
filterTabBar.anchoredPosition = { x: 0, y: -22 };
filterTabBar.sizeDelta = { x: -16, y: 32 };
leftPanel.addChild(filterTabBar);

function createTab(name, label) {
  const tNode = register(new UnityNode(name));
  tNode.sizeDelta = { x: 80, y: 32 };
  filterTabBar.addChild(tNode);
  const lNode = register(new UnityNode('Label'));
  lNode.anchorMin = { x: 0, y: 0 };
  lNode.anchorMax = { x: 1, y: 1 };
  lNode.sizeDelta = { x: 0, y: 0 };
  tNode.addChild(lNode);
  return { tab: tNode, label: lNode };
}

const tabAll = createTab('Tab_All', 'TẤT CẢ');
const tabRec = createTab('Tab_Recovery', 'HỒI PHỤC');
const tabSpd = createTab('Tab_Speed', 'TĂNG TỐC');
const tabOth = createTab('Tab_Other', 'KHÁC');

const rScrollView = register(new UnityNode('RecipeScrollView'));
rScrollView.anchorMin = { x: 0, y: 0 };
rScrollView.anchorMax = { x: 1, y: 1 };
rScrollView.pivot = { x: 0.5, y: 0.5 };
rScrollView.anchoredPosition = { x: 0, y: -30 };
rScrollView.sizeDelta = { x: -16, y: -72 };
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

// RecipeCardTemplate (inactive template)
const cardTemplate = register(new UnityNode('RecipeCardTemplate'));
cardTemplate.isActive = 0;
cardTemplate.anchorMin = { x: 0, y: 1 };
cardTemplate.anchorMax = { x: 1, y: 1 };
cardTemplate.pivot = { x: 0.5, y: 1 };
cardTemplate.anchoredPosition = { x: 0, y: 0 };
cardTemplate.sizeDelta = { x: 0, y: 84 };
leftPanel.addChild(cardTemplate);

const cardIconFrame = register(new UnityNode('IconFrame'));
cardIconFrame.anchorMin = { x: 0, y: 0.5 };
cardIconFrame.anchorMax = { x: 0, y: 0.5 };
cardIconFrame.pivot = { x: 0, y: 0.5 };
cardIconFrame.anchoredPosition = { x: 8, y: 0 };
cardIconFrame.sizeDelta = { x: 52, y: 52 };
cardTemplate.addChild(cardIconFrame);

const cardDishIcon = register(new UnityNode('DishIcon'));
cardDishIcon.anchorMin = { x: 0, y: 0 };
cardDishIcon.anchorMax = { x: 1, y: 1 };
cardDishIcon.offsetMin = { x: 4, y: 4 };
cardDishIcon.offsetMax = { x: -4, y: -4 };
cardDishIcon.sizeDelta = { x: -8, y: -8 };
cardIconFrame.addChild(cardDishIcon);

const cardActionBtn = register(new UnityNode('ActionButton'));
cardActionBtn.anchorMin = { x: 1, y: 0.5 };
cardActionBtn.anchorMax = { x: 1, y: 0.5 };
cardActionBtn.pivot = { x: 1, y: 0.5 };
cardActionBtn.anchoredPosition = { x: -8, y: 0 };
cardActionBtn.sizeDelta = { x: 82, y: 32 };
cardTemplate.addChild(cardActionBtn);

const cardActionText = register(new UnityNode('ActionText'));
cardActionText.anchorMin = { x: 0, y: 0 };
cardActionText.anchorMax = { x: 1, y: 1 };
cardActionText.sizeDelta = { x: 0, y: 0 };
cardActionBtn.addChild(cardActionText);

const cardInfo = register(new UnityNode('InfoColumn'));
cardInfo.anchorMin = { x: 0, y: 0 };
cardInfo.anchorMax = { x: 1, y: 1 };
cardInfo.offsetMin = { x: 68, y: 6 };
cardInfo.offsetMax = { x: -96, y: -6 };
cardInfo.sizeDelta = { x: -164, y: -12 };
cardTemplate.addChild(cardInfo);

const cardTitleRow = register(new UnityNode('TitleRow'));
cardTitleRow.anchorMin = { x: 0, y: 1 };
cardTitleRow.anchorMax = { x: 1, y: 1 };
cardTitleRow.pivot = { x: 0.5, y: 1 };
cardTitleRow.anchoredPosition = { x: 0, y: 0 };
cardTitleRow.sizeDelta = { x: 0, y: 22 };
cardInfo.addChild(cardTitleRow);

const cardTitleText = register(new UnityNode('TitleText'));
cardTitleText.anchorMin = { x: 0, y: 0 };
cardTitleText.anchorMax = { x: 1, y: 1 };
cardTitleText.sizeDelta = { x: 0, y: 0 };
cardTitleRow.addChild(cardTitleText);

const cardStarsText = register(new UnityNode('StarsText'));
cardStarsText.isActive = 0;
cardStarsText.anchorMin = { x: 1, y: 0 };
cardStarsText.anchorMax = { x: 1, y: 1 };
cardStarsText.sizeDelta = { x: 0, y: 0 };
cardTitleRow.addChild(cardStarsText);

const cardDetailText = register(new UnityNode('DetailText'));
cardDetailText.anchorMin = { x: 0, y: 0 };
cardDetailText.anchorMax = { x: 1, y: 1 };
cardDetailText.offsetMin = { x: 0, y: 2 };
cardDetailText.offsetMax = { x: 0, y: -24 };
cardDetailText.sizeDelta = { x: 0, y: -26 };
cardInfo.addChild(cardDetailText);

// --- COLUMN 2: MIDDLE PANEL (NỒI NẤU BẾP LỬA) ---
const midPanel = register(new UnityNode('MiddlePanel_Stove'));
midPanel.anchorMin = { x: 0.5, y: 0.5 };
midPanel.anchorMax = { x: 0.5, y: 0.5 };
midPanel.pivot = { x: 0.5, y: 0.5 };
midPanel.anchoredPosition = { x: 0, y: -35 };
midPanel.sizeDelta = { x: 480, y: 606 };
windowNode.addChild(midPanel);

const midTag = register(new UnityNode('HeaderTag'));
midTag.anchorMin = { x: 0.5, y: 1 };
midTag.anchorMax = { x: 0.5, y: 1 };
midTag.pivot = { x: 0.5, y: 0.5 };
midTag.anchoredPosition = { x: 0, y: 0 };
midTag.sizeDelta = { x: 230, y: 30 };
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
stage.anchoredPosition = { x: 0, y: -22 };
stage.sizeDelta = { x: 454, y: 110 };
midPanel.addChild(stage);

const potIcon = register(new UnityNode('PotIcon'));
potIcon.anchorMin = { x: 0.5, y: 0.5 };
potIcon.anchorMax = { x: 0.5, y: 0.5 };
potIcon.anchoredPosition = { x: -36, y: 10 };
potIcon.sizeDelta = { x: 64, y: 64 };
stage.addChild(potIcon);

const fireIcon = register(new UnityNode('FireIcon'));
fireIcon.anchorMin = { x: 0.5, y: 0.5 };
fireIcon.anchorMax = { x: 0.5, y: 0.5 };
fireIcon.anchoredPosition = { x: 36, y: 10 };
fireIcon.sizeDelta = { x: 54, y: 54 };
stage.addChild(fireIcon);

const stageSubtitle = register(new UnityNode('StageSubtitle'));
stageSubtitle.anchorMin = { x: 0, y: 0 };
stageSubtitle.anchorMax = { x: 1, y: 0 };
stageSubtitle.pivot = { x: 0.5, y: 0 };
stageSubtitle.anchoredPosition = { x: 0, y: 6 };
stageSubtitle.sizeDelta = { x: 0, y: 22 };
stage.addChild(stageSubtitle);

// PotSlotsSection
const potSlotsSec = register(new UnityNode('PotSlotsSection'));
potSlotsSec.anchorMin = { x: 0.5, y: 1 };
potSlotsSec.anchorMax = { x: 0.5, y: 1 };
potSlotsSec.pivot = { x: 0.5, y: 1 };
potSlotsSec.anchoredPosition = { x: 0, y: -138 };
potSlotsSec.sizeDelta = { x: 454, y: 100 };
midPanel.addChild(potSlotsSec);

const secTitle = register(new UnityNode('SectionTitle'));
secTitle.anchorMin = { x: 0, y: 1 };
secTitle.anchorMax = { x: 1, y: 1 };
secTitle.pivot = { x: 0.5, y: 1 };
secTitle.anchoredPosition = { x: 0, y: 0 };
secTitle.sizeDelta = { x: 0, y: 20 };
potSlotsSec.addChild(secTitle);

const slotsRow = register(new UnityNode('SlotsRow'));
slotsRow.anchorMin = { x: 0, y: 0 };
slotsRow.anchorMax = { x: 1, y: 0 };
slotsRow.pivot = { x: 0.5, y: 0 };
slotsRow.anchoredPosition = { x: 0, y: 4 };
slotsRow.sizeDelta = { x: 0, y: 72 };
potSlotsSec.addChild(slotsRow);

const potSlotsList = [];
for (let i = 0; i < 4; i++) {
  const sNode = register(new UnityNode(`PotSlot_${i}`));
  sNode.sizeDelta = { x: 64, y: 64 };
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
  icon.sizeDelta = { x: -16, y: -16 };
  sNode.addChild(icon);

  const count = register(new UnityNode('CountText'));
  count.isActive = 0;
  count.anchorMin = { x: 0.35, y: 0 };
  count.anchorMax = { x: 1, y: 0.45 };
  count.anchoredPosition = { x: -3, y: 2 };
  count.sizeDelta = { x: 0, y: 0 };
  sNode.addChild(count);

  potSlotsList.push({ root: sNode, hl, icon, count });
}

// ProgressBarSection
const progSec = register(new UnityNode('ProgressBarSection'));
progSec.anchorMin = { x: 0.5, y: 1 };
progSec.anchorMax = { x: 0.5, y: 1 };
progSec.pivot = { x: 0.5, y: 1 };
progSec.anchoredPosition = { x: 0, y: -244 };
progSec.sizeDelta = { x: 454, y: 52 };
midPanel.addChild(progSec);

const progTitle = register(new UnityNode('ProgressTitle'));
progTitle.anchorMin = { x: 0, y: 1 };
progTitle.anchorMax = { x: 1, y: 1 };
progTitle.pivot = { x: 0.5, y: 1 };
progTitle.anchoredPosition = { x: 0, y: 0 };
progTitle.sizeDelta = { x: 0, y: 18 };
progSec.addChild(progTitle);

const sliderNode = register(new UnityNode('CookingSlider'));
sliderNode.anchorMin = { x: 0, y: 0 };
sliderNode.anchorMax = { x: 1, y: 0 };
sliderNode.pivot = { x: 0.5, y: 0 };
sliderNode.anchoredPosition = { x: 0, y: 2 };
sliderNode.sizeDelta = { x: 0, y: 26 };
progSec.addChild(sliderNode);

const fillArea = register(new UnityNode('Fill Area'));
fillArea.anchorMin = { x: 0, y: 0 };
fillArea.anchorMax = { x: 1, y: 1 };
fillArea.sizeDelta = { x: -6, y: -6 };
sliderNode.addChild(fillArea);

const fill = register(new UnityNode('Fill'));
fill.anchorMin = { x: 0, y: 0 };
fill.anchorMax = { x: 0, y: 1 };
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
prevCard.anchoredPosition = { x: 0, y: -304 };
prevCard.sizeDelta = { x: 454, y: 94 };
midPanel.addChild(prevCard);

const prevIconFrame = register(new UnityNode('PreviewIconFrame'));
prevIconFrame.anchorMin = { x: 0, y: 0.5 };
prevIconFrame.anchorMax = { x: 0, y: 0.5 };
prevIconFrame.pivot = { x: 0, y: 0.5 };
prevIconFrame.anchoredPosition = { x: 12, y: 0 };
prevIconFrame.sizeDelta = { x: 64, y: 64 };
prevCard.addChild(prevIconFrame);

const prevDishIcon = register(new UnityNode('DishIcon'));
prevDishIcon.anchorMin = { x: 0, y: 0 };
prevDishIcon.anchorMax = { x: 1, y: 1 };
prevDishIcon.offsetMin = { x: 6, y: 6 };
prevDishIcon.offsetMax = { x: -6, y: -6 };
prevDishIcon.sizeDelta = { x: -12, y: -12 };
prevIconFrame.addChild(prevDishIcon);

const prevInfo = register(new UnityNode('InfoBlock'));
prevInfo.anchorMin = { x: 0, y: 0 };
prevInfo.anchorMax = { x: 1, y: 1 };
prevInfo.offsetMin = { x: 86, y: 6 };
prevInfo.offsetMax = { x: -12, y: -6 };
prevInfo.sizeDelta = { x: -98, y: -12 };
prevCard.addChild(prevInfo);

const prevHeaderLbl = register(new UnityNode('PreviewHeaderLabel'));
prevHeaderLbl.anchorMin = { x: 0, y: 1 };
prevHeaderLbl.anchorMax = { x: 1, y: 1 };
prevHeaderLbl.pivot = { x: 0, y: 1 };
prevHeaderLbl.anchoredPosition = { x: 0, y: 0 };
prevHeaderLbl.sizeDelta = { x: 0, y: 18 };
prevInfo.addChild(prevHeaderLbl);

const prevDishName = register(new UnityNode('PreviewDishName'));
prevDishName.anchorMin = { x: 0, y: 1 };
prevDishName.anchorMax = { x: 1, y: 1 };
prevDishName.pivot = { x: 0, y: 1 };
prevDishName.anchoredPosition = { x: 0, y: -20 };
prevDishName.sizeDelta = { x: 0, y: 26 };
prevInfo.addChild(prevDishName);

const prevStats = register(new UnityNode('PreviewDishStats'));
prevStats.anchorMin = { x: 0, y: 0 };
prevStats.anchorMax = { x: 1, y: 0 };
prevStats.pivot = { x: 0, y: 0 };
prevStats.anchoredPosition = { x: 0, y: 2 };
prevStats.sizeDelta = { x: 0, y: 22 };
prevInfo.addChild(prevStats);

// ActionButtonsRow
const actRow = register(new UnityNode('ActionButtonsRow'));
actRow.anchorMin = { x: 0.5, y: 0 };
actRow.anchorMax = { x: 0.5, y: 0 };
actRow.pivot = { x: 0.5, y: 0 };
actRow.anchoredPosition = { x: 0, y: 16 };
actRow.sizeDelta = { x: 454, y: 44 };
midPanel.addChild(actRow);

const cookBtn = register(new UnityNode('CookButton'));
cookBtn.sizeDelta = { x: 220, y: 44 };
actRow.addChild(cookBtn);
const cookBtnText = register(new UnityNode('ActionText'));
cookBtnText.anchorMin = { x: 0, y: 0 };
cookBtnText.anchorMax = { x: 1, y: 1 };
cookBtnText.sizeDelta = { x: 0, y: 0 };
cookBtn.addChild(cookBtnText);

const cancelBtn = register(new UnityNode('CancelButton'));
cancelBtn.sizeDelta = { x: 220, y: 44 };
actRow.addChild(cancelBtn);
const cancelBtnText = register(new UnityNode('ActionText'));
cancelBtnText.anchorMin = { x: 0, y: 0 };
cancelBtnText.anchorMax = { x: 1, y: 1 };
cancelBtnText.sizeDelta = { x: 0, y: 0 };
cancelBtn.addChild(cancelBtnText);


// --- COLUMN 3: RIGHT PANEL (BALO NGUYÊN LIỆU) ---
const rightPanel = register(new UnityNode('RightPanel_Backpack'));
rightPanel.anchorMin = { x: 1, y: 0 };
rightPanel.anchorMax = { x: 1, y: 1 };
rightPanel.pivot = { x: 1, y: 0.5 };
rightPanel.anchoredPosition = { x: -14, y: -35 };
rightPanel.sizeDelta = { x: 366, y: -94 };
windowNode.addChild(rightPanel);

const rightTag = register(new UnityNode('HeaderTag'));
rightTag.anchorMin = { x: 0.5, y: 1 };
rightTag.anchorMax = { x: 0.5, y: 1 };
rightTag.pivot = { x: 0.5, y: 0.5 };
rightTag.anchoredPosition = { x: 0, y: 0 };
rightTag.sizeDelta = { x: 210, y: 30 };
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
bpSubtitle.anchoredPosition = { x: 0, y: -22 };
bpSubtitle.sizeDelta = { x: -20, y: 20 };
rightPanel.addChild(bpSubtitle);

const bpScrollView = register(new UnityNode('BackpackScrollView'));
bpScrollView.anchorMin = { x: 0, y: 0 };
bpScrollView.anchorMax = { x: 1, y: 1 };
bpScrollView.pivot = { x: 0.5, y: 0.5 };
bpScrollView.anchoredPosition = { x: 0, y: -4 };
bpScrollView.sizeDelta = { x: -16, y: -108 };
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
bpActRow.sizeDelta = { x: 338, y: 38 };
rightPanel.addChild(bpActRow);

const sortBtn = register(new UnityNode('SortButton'));
sortBtn.sizeDelta = { x: 164, y: 38 };
bpActRow.addChild(sortBtn);
const sortBtnText = register(new UnityNode('ActionText'));
sortBtnText.anchorMin = { x: 0, y: 0 };
sortBtnText.anchorMax = { x: 1, y: 1 };
sortBtnText.sizeDelta = { x: 0, y: 0 };
sortBtn.addChild(sortBtnText);

const clearBtn = register(new UnityNode('ClearButton'));
clearBtn.sizeDelta = { x: 164, y: 38 };
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
toast.sizeDelta = { x: 500, y: 38 };
overlay.addChild(toast);

const toastText = register(new UnityNode('ToastText'));
toastText.anchorMin = { x: 0, y: 0 };
toastText.anchorMax = { x: 1, y: 1 };
toastText.sizeDelta = { x: -20, y: 0 };
toast.addChild(toastText);

console.log(`Hierarchy prepared: ${allNodes.length} nodes registered.`);

// ==========================================
// 4. COMPONENT ASSIGNMENTS
// ==========================================
// Nodes that should NOT have Images (pure containers)
const NO_IMAGE_NODES = new Set([
  'FilterTabBar', 'RecipeScrollView', 'Viewport', 'Content',
  'InfoColumn', 'TitleRow', 'PotSlotsSection', 'SlotsRow',
  'ProgressBarSection', 'Fill Area', 'OutputPreviewCard_InfoBlock', 'InfoBlock',
  'ActionButtonsRow', 'BackpackScrollView', 'BackpackActionButtonsRow'
]);

for (const n of allNodes) {
  if (n === kitchenCanvas) continue;
  n.crId = genId(); // CanvasRenderer

  const isText = n.name.includes('Title') || n.name.includes('Text') || n.name.includes('Subtitle') || 
                 n.name.includes('Label') || n.name.includes('Stats') || n.name.includes('Name');

  if (isText) {
    n.tmpId = genId();
  } else if (!NO_IMAGE_NODES.has(n.name)) {
    n.imgId = genId();
  }

  if (n.name.includes('Button') || n.name.includes('Tab_') || n.name.includes('PotSlot_')) {
    n.btnId = genId();
  }

  if (n.name === 'CookingSlider') {
    n.sliderId = genId();
    n.imgId = genId(); // background track
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

// Canvas-level component IDs
kitchenCanvas.canvasId = genId();
kitchenCanvas.scalerId = genId();
kitchenCanvas.raycasterId = genId();
kitchenCanvas.uiId = genId();

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
  - component: {fileID: ${kitchenCanvas.canvasId}}
  - component: {fileID: ${kitchenCanvas.scalerId}}
  - component: {fileID: ${kitchenCanvas.raycasterId}}
  - component: {fileID: ${kitchenCanvas.uiId}}
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

// KitchenUI Component
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

// Emit GameObjects & Components for all other nodes
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
  m_Children: ${n.children.length === 0 ? '[]' : '\n' + n.children.map(c => `  - {fileID: ${c.rtId}}`).join('\n')}
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
    let type = 0; // 0 = Simple, 1 = Sliced
    let preserveAspect = 0;
    let r = 1, g = 1, b = 1, a = 1;

    if (n.name === 'KitchenOverlay') {
      r = 0.05; g = 0.04; b = 0.04; a = 0.85;
    } else if (n.name === 'KitchenWindow') {
      spriteRef = `{fileID: ${SPRITE_UI_FRAME}, guid: ${GUID_UI_FRAME}, type: 3}`;
      type = 1;
      r = 0.22; g = 0.16; b = 0.12; a = 1;
    } else if (n.name === 'InnerBorder') {
      r = 0.09; g = 0.08; b = 0.07; a = 0.98;
    } else if (n.name === 'TopHeaderBar' || n.name === 'HeaderTag' || n.name === 'LeftHeaderTag' || n.name === 'FeedbackToast') {
      spriteRef = `{fileID: ${SPRITE_HEADER_PLAQUE}, guid: ${GUID_HEADER_PLAQUE}, type: 3}`;
      type = 1;
      r = 0.35; g = 0.22; b = 0.15; a = 1;
    } else if (n.name === 'LeftPanel_Recipes' || n.name === 'MiddlePanel_Stove' || n.name === 'RightPanel_Backpack') {
      spriteRef = `{fileID: ${SPRITE_UI_FRAME}, guid: ${GUID_UI_FRAME}, type: 3}`;
      type = 1;
      r = 0.13; g = 0.14; b = 0.19; a = 0.96;
    } else if (n.name === 'CookingStage') {
      spriteRef = `{fileID: ${SPRITE_UI_FRAME}, guid: ${GUID_UI_FRAME}, type: 3}`;
      type = 1;
      r = 0.11; g = 0.09; b = 0.08; a = 0.96;
    } else if (n.name === 'PotIcon') {
      spriteRef = `{fileID: ${SPRITE_SOUP_ICON}, guid: ${GUID_SOUP_ICON}, type: 3}`;
      preserveAspect = 1;
      r = 1; g = 1; b = 1; a = 1;
    } else if (n.name === 'FireIcon') {
      spriteRef = `{fileID: ${SPRITE_CAMPFIRE_1}, guid: ${GUID_CAMPFIRE}, type: 3}`;
      preserveAspect = 1;
      r = 1; g = 1; b = 1; a = 1;
    } else if (n.name.includes('PotSlot_')) {
      spriteRef = `{fileID: ${SPRITE_UI_SLOT}, guid: ${GUID_UI_SLOT}, type: 3}`;
      type = 1;
      r = 1; g = 1; b = 1; a = 1;
    } else if (n.name === 'HighlightBorder') {
      r = 0.22; g = 0.74; b = 0.97; a = 0.9;
    } else if (n.name === 'CookingSlider') {
      spriteRef = `{fileID: ${SPRITE_UI_FRAME}, guid: ${GUID_UI_FRAME}, type: 3}`;
      type = 1;
      r = 0.10; g = 0.10; b = 0.13; a = 1;
    } else if (n.name === 'Fill') {
      r = 1.0; g = 0.55; b = 0.08; a = 1;
    } else if (n.name === 'OutputPreviewCard') {
      spriteRef = `{fileID: ${SPRITE_UI_FRAME}, guid: ${GUID_UI_FRAME}, type: 3}`;
      type = 1;
      r = 0.15; g = 0.16; b = 0.22; a = 0.98;
    } else if (n.name === 'PreviewIconFrame') {
      spriteRef = `{fileID: ${SPRITE_PREVIEW_ICON_FRAME}, guid: ${GUID_PREVIEW_ICON_FRAME}, type: 3}`;
      type = 1;
      r = 0.18; g = 0.20; b = 0.26; a = 1;
    } else if (n.name === 'RecipeCardTemplate') {
      spriteRef = `{fileID: ${SPRITE_UI_FRAME}, guid: ${GUID_UI_FRAME}, type: 3}`;
      type = 1;
      r = 0.17; g = 0.19; b = 0.26; a = 0.96;
    } else if (n.name === 'IconFrame') {
      spriteRef = `{fileID: ${SPRITE_CARD_ICON_FRAME}, guid: ${GUID_CARD_ICON_FRAME}, type: 3}`;
      type = 1;
      r = 0.22; g = 0.24; b = 0.32; a = 1;
    } else if (n.name === 'DishIcon' || n.name === 'ItemIcon') {
      preserveAspect = 1;
      r = 1; g = 1; b = 1; a = 1;
    } else if (n.name === 'CloseButton') {
      spriteRef = `{fileID: ${SPRITE_UI_FRAME}, guid: ${GUID_UI_FRAME}, type: 3}`;
      type = 1;
      r = 0.55; g = 0.15; b = 0.15; a = 1;
    } else if (n.name === 'Tab_All') {
      spriteRef = `{fileID: ${SPRITE_UI_FRAME}, guid: ${GUID_UI_FRAME}, type: 3}`;
      type = 1;
      r = 0.18; g = 0.45; b = 0.20; a = 1;
    } else if (n.name.includes('Tab_')) {
      spriteRef = `{fileID: ${SPRITE_UI_FRAME}, guid: ${GUID_UI_FRAME}, type: 3}`;
      type = 1;
      r = 0.18; g = 0.20; b = 0.26; a = 0.90;
    } else if (n.name === 'CookButton') {
      spriteRef = `{fileID: ${SPRITE_UI_FRAME}, guid: ${GUID_UI_FRAME}, type: 3}`;
      type = 1;
      r = 0.18; g = 0.55; b = 0.22; a = 1;
    } else if (n.name === 'CancelButton') {
      spriteRef = `{fileID: ${SPRITE_UI_FRAME}, guid: ${GUID_UI_FRAME}, type: 3}`;
      type = 1;
      r = 0.65; g = 0.18; b = 0.18; a = 1;
    } else if (n.name === 'SortButton') {
      spriteRef = `{fileID: ${SPRITE_UI_FRAME}, guid: ${GUID_UI_FRAME}, type: 3}`;
      type = 1;
      r = 0.75; g = 0.45; b = 0.12; a = 1;
    } else if (n.name === 'ClearButton') {
      spriteRef = `{fileID: ${SPRITE_UI_FRAME}, guid: ${GUID_UI_FRAME}, type: 3}`;
      type = 1;
      r = 0.28; g = 0.32; b = 0.40; a = 1;
    } else if (n.name === 'ActionButton') {
      spriteRef = `{fileID: ${SPRITE_UI_FRAME}, guid: ${GUID_UI_FRAME}, type: 3}`;
      type = 1;
      r = 0.18; g = 0.52; b = 0.20; a = 1;
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
  m_Color: {r: ${r}, g: ${g}, b: ${b}, a: ${a}}
  m_RaycastTarget: 1
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: ${spriteRef}
  m_Type: ${type}
  m_PreserveAspect: ${preserveAspect}
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
    let size = 13;
    let bold = 1;
    let hAlign = 2; // Center default
    let vAlign = 512; // Middle default
    let wrap = 1;
    let overflow = 0;
    let cr = 1, cg = 1, cb = 1, ca = 1;

    if (n.name === 'HeaderTitle') {
      text = 'BẾP NẤU GIA ĐÌNH & SỔ TAY CÔNG THỨC ẨM THỰC';
      size = 18; cr = 1; cg = 0.88; cb = 0.51; // #FFE082
    } else if (n.name === 'CloseText') {
      text = '✖ [ESC] Đóng Bếp'; size = 13; align = 514; cr = 1; cg = 1; cb = 1;
    } else if (n.name === 'LeftTagText') {
      text = 'SỔ TAY CÔNG THỨC'; size = 13; align = 514; cr = 1; cg = 0.85; cb = 0.35;
    } else if (n.name === 'TagText' && n.father.father.name === 'MiddlePanel_Stove') {
      text = 'NỒI NẤU BẾP LỬA'; size = 13; align = 514; cr = 1; cg = 0.85; cb = 0.35;
    } else if (n.name === 'TagText' && n.father.father.name === 'RightPanel_Backpack') {
      text = 'BALO NGUYÊN LIỆU'; size = 13; align = 514; cr = 1; cg = 0.85; cb = 0.35;
    } else if (n.name === 'StageSubtitle') {
      text = 'Bếp Lửa Sôi Sùng Sục • Sẵn Sàng Chế Biến'; size = 11; align = 514; cr = 0.82; cg = 0.72; cb = 0.58;
    } else if (n.name === 'SectionTitle') {
      text = 'BỎ NGUYÊN LIỆU VÀO NỒI (TỐI ĐA 4)'; size = 11; align = 514; cr = 1; cg = 0.88; cb = 0.51;
    } else if (n.name === 'ProgressTitle') {
      text = 'TIẾN TRÌNH NẤU ĂN'; size = 11; align = 514; cr = 1; cg = 0.88; cb = 0.51;
    } else if (n.name === 'StatusText') {
      text = 'SẴN SÀNG NẤU'; size = 11; align = 514; cr = 1; cg = 1; cb = 1;
    } else if (n.name === 'PreviewHeaderLabel') {
      text = 'MÓN THÀNH PHẨM DỰ KIẾN:'; size = 11; hAlign = 1; cr = 1; cg = 0.85; cb = 0.35;
    } else if (n.name === 'PreviewDishName') {
      text = 'Súp Cà Chua Bổ Dưỡng'; size = 14; hAlign = 1; wrap = 0; cr = 0.51; cg = 0.78; cb = 0.52; // #81C784
    } else if (n.name === 'PreviewDishStats') {
      text = 'Hồi 22 HP & 25 Độ No • Cần: Cà Chua x2, Bắp Cải x1'; size = 11; hAlign = 1; wrap = 1; cr = 0.72; cg = 0.78; cb = 0.85;
    } else if (n.name === 'BackpackSubtitle') {
      text = 'Nhấp vào nguyên liệu để bỏ vào nồi nấu'; size = 11; align = 514; cr = 0.82; cg = 0.72; cb = 0.58;
    } else if (n.name === 'TitleText' && n.father.name === 'TitleRow') {
      text = 'Súp Cà Chua'; size = 13; hAlign = 1; wrap = 0; overflow = 0; cr = 1; cg = 0.88; cb = 0.51;
    } else if (n.name === 'StarsText' && n.father.name === 'TitleRow') {
      text = ''; size = 11; hAlign = 1; cr = 1; cg = 0.84; cb = 0.31;
    } else if (n.name === 'DetailText' && n.father.name === 'InfoColumn') {
      text = 'Hồi 22 HP & 25 Độ No | Cần: Cà Chuax2 + Bắp Cảix1'; size = 10; hAlign = 1; wrap = 1; overflow = 0; cr = 0.75; cg = 0.78; cb = 0.84;
    } else if (n.name === 'CountText') {
      text = 'x2'; size = 12; hAlign = 4; vAlign = 1024; wrap = 0; cr = 1; cg = 1; cb = 1;
    } else if (n.name === 'ToastText') {
      text = 'Thông báo hệ thống!'; size = 13; align = 514; cr = 1; cg = 0.90; cb = 0.60;
    } else if (n.father.name === 'CookButton') {
      text = 'BẮT ĐẦU NẤU'; size = 14; align = 514; cr = 1; cg = 1; cb = 1;
    } else if (n.father.name === 'CancelButton') {
      text = '✖ HỦY NGUYÊN LIỆU'; size = 12; align = 514; cr = 1; cg = 1; cb = 1;
    } else if (n.father.name === 'SortButton') {
      text = 'SẮP XẾP'; size = 12; align = 514; cr = 1; cg = 1; cb = 1;
    } else if (n.father.name === 'ClearButton') {
      text = 'LÀM TRỐNG Ô'; size = 12; align = 514; cr = 1; cg = 1; cb = 1;
    } else if (n.father.name === 'ActionButton') {
      text = 'TỰ ĐIỀN'; size = 11; align = 514; cr = 1; cg = 1; cb = 1;
    } else if (n.father.name === 'Tab_All') {
      text = 'TẤT CẢ'; size = 10; cr = 1; cg = 1; cb = 1;
    } else if (n.father.name === 'Tab_Recovery') {
      text = 'HỒI PHỤC'; size = 10; cr = 0.75; cg = 0.78; cb = 0.85;
    } else if (n.father.name === 'Tab_Speed') {
      text = 'TĂNG TỐC'; size = 10; cr = 0.75; cg = 0.78; cb = 0.85;
    } else if (n.father.name === 'Tab_Other') {
      text = 'KHÁC'; size = 10; cr = 0.75; cg = 0.78; cb = 0.85;
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
  m_HorizontalAlignment: ${hAlign}
  m_VerticalAlignment: ${vAlign}
  m_textAlignment: 65535
  m_characterSpacing: 0
  m_wordSpacing: 0
  m_lineSpacing: 0
  m_lineSpacingMax: 0
  m_paragraphSpacing: 0
  m_charWidthMaxAdj: 0
  m_enableWordWrapping: ${wrap}
  m_wordWrappingRatios: 0.4
  m_overflowMode: ${overflow}
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
  m_baseMaterial: {fileID: 0}`);
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
    m_Mode: 0
  m_Transition: 1
  m_Colors:
    m_NormalColor: {r: 1, g: 1, b: 1, a: 1}
    m_HighlightedColor: {r: 1.15, g: 1.15, b: 1.15, a: 1}
    m_PressedColor: {r: 0.8, g: 0.8, b: 0.8, a: 1}
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
    const spacing = n.name === 'FilterTabBar' ? 4 : (n.name === 'SlotsRow' ? 12 : 12);
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
    m_Left: 2
    m_Right: 2
    m_Top: 4
    m_Bottom: 4
  m_ChildAlignment: 0
  m_Spacing: 6
  m_ChildForceExpandWidth: 1
  m_ChildForceExpandHeight: 0
  m_ChildControlWidth: 1
  m_ChildControlHeight: 0
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
  m_CellSize: {x: 52, y: 52}
  m_Spacing: {x: 6, y: 6}
  m_StartCorner: 0
  m_StartAxis: 0
  m_ChildAlignment: 1
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

// 2. Safely Update Base.unity Scene
const scenePath = 'Assets/_Scenes/Base.unity';
let sceneContent = fs.readFileSync(scenePath, 'utf8');

const sceneBlocks = out.join('\n') + '\n';

const first910 = sceneContent.search(/--- !u!\d+ &910000\d+/);
const endBoundary = sceneContent.indexOf('--- !u!1001 &921133217');

if (first910 !== -1 && endBoundary !== -1 && first910 < endBoundary) {
  sceneContent = sceneContent.substring(0, first910) + sceneBlocks + sceneContent.substring(endBoundary);
  fs.writeFileSync(scenePath, sceneContent, 'utf8');
  console.log(`Successfully replaced old KitchenCanvas block in ${scenePath} (indices ${first910}..${endBoundary})!`);
} else {
  // If boundary not found, append safely
  sceneContent = sceneContent.trimEnd() + '\n' + sceneBlocks;
  fs.writeFileSync(scenePath, sceneContent, 'utf8');
  console.log(`Appended KitchenCanvas blocks to ${scenePath}!`);
}

console.log('KitchenCanvas build complete!');
