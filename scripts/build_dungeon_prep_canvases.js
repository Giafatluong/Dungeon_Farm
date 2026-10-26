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
const GUID_CSF = '3245ec927659c4140ac4f8d17403cc18';
const GUID_RECTMASK2D = '3312d7739989d2b4e91e6319e9a96d76';
const GUID_ENTRANCE_ARROW_UI = 'f82b490ce07047cb9e23d856e6539a11';
const GUID_CAMP_UI = 'a0b5932acff90184fb9b7c10bdfe5e51';

// Asset GUIDs
const GUID_FONT = '8f586378b4e144a9851e7b34d9b748ee';
const GUID_FRAME = 'aa6f9a2ecff34c14d84e5bd91727c940';
const SPRITE_FRAME = '-5668518402485374812';

let nextId = 930000000;
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
// 1. BUILD DUNGEON ENTRANCE ARROW CANVAS
// ==========================================
const arrowCanvas = reg(new Node('DungeonEntranceArrowCanvas', true));
arrowCanvas.canvasId = genId();
arrowCanvas.scalerId = genId();
arrowCanvas.raycasterId = genId();
arrowCanvas.uiId = genId();
arrowCanvas.sortingOrder = 105;

const arrowContainer = reg(new Node('EntranceArrowContainer'));
arrowContainer.anchorMin = { x: 1, y: 0.5 };
arrowContainer.anchorMax = { x: 1, y: 0.5 };
arrowContainer.pivot = { x: 1, y: 0.5 };
arrowContainer.anchoredPosition = { x: -28, y: 0 };
arrowContainer.sizeDelta = { x: 210, y: 85 };
arrowContainer.isActive = 0; // inactive initially
arrowContainer.crId = genId();
arrowContainer.imgId = genId();
arrowContainer.btnId = genId();
arrowCanvas.addChild(arrowContainer);

const arrowTMP = reg(new Node('ArrowTMP'));
arrowTMP.anchorMin = { x: 1, y: 0.5 };
arrowTMP.anchorMax = { x: 1, y: 0.5 };
arrowTMP.pivot = { x: 1, y: 0.5 };
arrowTMP.anchoredPosition = { x: -12, y: 0 };
arrowTMP.sizeDelta = { x: 50, y: 50 };
arrowTMP.crId = genId();
arrowTMP.tmpId = genId();
arrowTMP.text = '->';
arrowTMP.fontSize = 32;
arrowTMP.align = 514;
arrowTMP.color = { r: 1, g: 0.85, b: 0.25, a: 1 };
arrowContainer.addChild(arrowTMP);

const arrowTitle = reg(new Node('TitleText'));
arrowTitle.anchorMin = { x: 0, y: 0.5 };
arrowTitle.anchorMax = { x: 0, y: 0.5 };
arrowTitle.pivot = { x: 0, y: 0.5 };
arrowTitle.anchoredPosition = { x: 16, y: 14 };
arrowTitle.sizeDelta = { x: 130, y: 30 };
arrowTitle.crId = genId();
arrowTitle.tmpId = genId();
arrowTitle.text = 'PROCEED';
arrowTitle.fontSize = 20;
arrowTitle.fontStyle = 1; // Bold
arrowTitle.color = { r: 1, g: 1, b: 1, a: 1 };
arrowContainer.addChild(arrowTitle);

const arrowHint = reg(new Node('HintText'));
arrowHint.anchorMin = { x: 0, y: 0.5 };
arrowHint.anchorMax = { x: 0, y: 0.5 };
arrowHint.pivot = { x: 0, y: 0.5 };
arrowHint.anchoredPosition = { x: 16, y: -14 };
arrowHint.sizeDelta = { x: 130, y: 24 };
arrowHint.crId = genId();
arrowHint.tmpId = genId();
arrowHint.text = 'Enter Stage 1 [Space / ->]';
arrowHint.fontSize = 12;
arrowHint.color = { r: 0.75, g: 0.85, b: 1, a: 1 };
arrowContainer.addChild(arrowHint);


// ==========================================
// 2. BUILD CAMP CANVAS
// ==========================================
const campCanvas = reg(new Node('CampCanvas', true));
campCanvas.canvasId = genId();
campCanvas.scalerId = genId();
campCanvas.raycasterId = genId();
campCanvas.uiId = genId();
campCanvas.sortingOrder = 120;

// Main Camp Panel
const campMainPanel = reg(new Node('CampMainPanel'));
campMainPanel.sizeDelta = { x: 780, y: 530 };
campMainPanel.isActive = 0; // inactive initially
campMainPanel.crId = genId();
campMainPanel.imgId = genId();
campCanvas.addChild(campMainPanel);

// Camp Icon
const campIcon = reg(new Node('CampIcon'));
campIcon.anchorMin = { x: 0.5, y: 1 };
campIcon.anchorMax = { x: 0.5, y: 1 };
campIcon.pivot = { x: 0.5, y: 1 };
campIcon.anchoredPosition = { x: -190, y: -14 };
campIcon.sizeDelta = { x: 38, y: 38 };
campIcon.crId = genId();
campIcon.imgId = genId();
campMainPanel.addChild(campIcon);

// Camp Title
const campTitle = reg(new Node('CampTitle'));
campTitle.anchorMin = { x: 0, y: 1 };
campTitle.anchorMax = { x: 1, y: 1 };
campTitle.pivot = { x: 0.5, y: 1 };
campTitle.anchoredPosition = { x: 0, y: -18 };
campTitle.sizeDelta = { x: -40, y: 36 };
campTitle.crId = genId();
campTitle.tmpId = genId();
campTitle.text = 'CAMP ENCOUNTER';
campTitle.fontSize = 22;
campTitle.fontStyle = 1;
campTitle.align = 514;
campTitle.color = { r: 1, g: 0.85, b: 0.35, a: 1 };
campMainPanel.addChild(campTitle);

// Camp Desc
const campDesc = reg(new Node('CampDesc'));
campDesc.anchorMin = { x: 0, y: 1 };
campDesc.anchorMax = { x: 1, y: 1 };
campDesc.pivot = { x: 0.5, y: 1 };
campDesc.anchoredPosition = { x: 0, y: -56 };
campDesc.sizeDelta = { x: -60, y: 48 };
campDesc.crId = genId();
campDesc.tmpId = genId();
campDesc.text = 'A warm campfire burns in the dungeon. Rest, eat, or cook to recover.\\n<color=#FFAA55>Warning: Activities create noise and aroma that attract wandering monsters!</color>';
campDesc.fontSize = 12.5;
campDesc.align = 514;
campDesc.color = { r: 0.85, g: 0.88, b: 0.95, a: 1 };
campMainPanel.addChild(campDesc);

// Risk Section
const riskSection = reg(new Node('RiskSection'));
riskSection.anchorMin = { x: 0.5, y: 1 };
riskSection.anchorMax = { x: 0.5, y: 1 };
riskSection.anchoredPosition = { x: 0, y: -135 };
riskSection.sizeDelta = { x: 500, y: 60 };
campMainPanel.addChild(riskSection);

const riskPercentText = reg(new Node('RiskPercentText'));
riskPercentText.anchorMin = { x: 0, y: 1 };
riskPercentText.anchorMax = { x: 1, y: 1 };
riskPercentText.anchoredPosition = { x: 0, y: 0 };
riskPercentText.sizeDelta = { x: 0, y: 20 };
riskPercentText.crId = genId();
riskPercentText.tmpId = genId();
riskPercentText.text = 'Ambush Risk: <b>0%</b>';
riskPercentText.fontSize = 13.5;
riskPercentText.align = 514;
riskPercentText.color = { r: 1, g: 1, b: 1, a: 1 };
riskSection.addChild(riskPercentText);

const gaugeBg = reg(new Node('GaugeBg'));
gaugeBg.anchoredPosition = { x: 0, y: -16 };
gaugeBg.sizeDelta = { x: 460, y: 14 };
gaugeBg.crId = genId();
gaugeBg.imgId = genId();
gaugeBg.color = { r: 0.2, g: 0.22, b: 0.28, a: 1 };
riskSection.addChild(gaugeBg);

const gaugeFill = reg(new Node('GaugeFill'));
gaugeFill.anchorMin = { x: 0, y: 0 };
gaugeFill.anchorMax = { x: 1, y: 1 };
gaugeFill.sizeDelta = { x: 0, y: 0 };
gaugeFill.crId = genId();
gaugeFill.imgId = genId();
gaugeFill.color = { r: 0.2, g: 0.85, b: 0.4, a: 1 };
gaugeFill.isFilled = true;
gaugeBg.addChild(gaugeFill);

const riskStatusText = reg(new Node('RiskStatusText'));
riskStatusText.anchoredPosition = { x: 0, y: -38 };
riskStatusText.sizeDelta = { x: 460, y: 18 };
riskStatusText.crId = genId();
riskStatusText.tmpId = genId();
riskStatusText.text = 'The campsite is quiet and safe.';
riskStatusText.fontSize = 12;
riskStatusText.align = 514;
riskStatusText.color = { r: 0.7, g: 1, b: 0.7, a: 1 };
riskSection.addChild(riskStatusText);

// Player Status Section
const playerStatusText = reg(new Node('PlayerStatusText'));
playerStatusText.anchorMin = { x: 0.5, y: 1 };
playerStatusText.anchorMax = { x: 0.5, y: 1 };
playerStatusText.anchoredPosition = { x: 0, y: -188 };
playerStatusText.sizeDelta = { x: 600, y: 22 };
playerStatusText.crId = genId();
playerStatusText.tmpId = genId();
playerStatusText.text = 'HP: 100/100   |   Fullness: 100/100';
playerStatusText.fontSize = 13.5;
playerStatusText.align = 514;
playerStatusText.color = { r: 1, g: 1, b: 1, a: 1 };
campMainPanel.addChild(playerStatusText);

const playerBuffsText = reg(new Node('PlayerBuffsText'));
playerBuffsText.anchorMin = { x: 0.5, y: 1 };
playerBuffsText.anchorMax = { x: 0.5, y: 1 };
playerBuffsText.anchoredPosition = { x: 0, y: -210 };
playerBuffsText.sizeDelta = { x: 600, y: 18 };
playerBuffsText.crId = genId();
playerBuffsText.tmpId = genId();
playerBuffsText.text = 'Buffs: None';
playerBuffsText.fontSize = 12;
playerBuffsText.align = 514;
playerBuffsText.color = { r: 1, g: 1, b: 1, a: 1 };
campMainPanel.addChild(playerBuffsText);

// Action Buttons Group
const actionBtns = reg(new Node('ActionButtons'));
actionBtns.anchorMin = { x: 0.5, y: 0 };
actionBtns.anchorMax = { x: 0.5, y: 0 };
actionBtns.anchoredPosition = { x: 0, y: 140 };
actionBtns.sizeDelta = { x: 680, y: 110 };
campMainPanel.addChild(actionBtns);

function makeCampBtn(name, label, pos, size, color, targetMethod) {
  const btnNode = reg(new Node(name));
  btnNode.anchoredPosition = pos;
  btnNode.sizeDelta = size;
  btnNode.crId = genId();
  btnNode.imgId = genId();
  btnNode.btnId = genId();
  btnNode.btnColor = color;
  btnNode.targetMethod = targetMethod;
  actionBtns.addChild(btnNode);

  const tNode = reg(new Node('Text'));
  tNode.anchorMin = { x: 0, y: 0 };
  tNode.anchorMax = { x: 1, y: 1 };
  tNode.sizeDelta = { x: 0, y: 0 };
  tNode.crId = genId();
  tNode.tmpId = genId();
  tNode.text = label;
  tNode.fontSize = 12;
  tNode.fontStyle = 1;
  tNode.align = 514;
  tNode.color = { r: 1, g: 1, b: 1, a: 1 };
  btnNode.addChild(tNode);
  return btnNode;
}

const eatBtn = makeCampBtn('EatBtn', 'EAT FOOD\\n(+15% Risk)', { x: -230, y: 26 }, { x: 210, y: 48 }, { r: 0.2, g: 0.6, b: 0.35, a: 1 }, 'OnEatButtonClicked');
const cookBtn = makeCampBtn('CookBtn', 'COOK RECIPE\\n(+20% Risk)', { x: 0, y: 26 }, { x: 210, y: 48 }, { r: 0.8, g: 0.45, b: 0.15, a: 1 }, 'OnCookButtonClicked');
const exerciseBtn = makeCampBtn('ExerciseBtn', 'EXERCISE (-25 Full)\\n(+35% Risk)', { x: 230, y: 26 }, { x: 210, y: 48 }, { r: 0.25, g: 0.5, b: 0.75, a: 1 }, 'OnExerciseButtonClicked');
const restBtn = makeCampBtn('RestBtn', 'REST (+25 HP)\\n(+10% Risk)', { x: -230, y: -30 }, { x: 210, y: 48 }, { r: 0.2, g: 0.55, b: 0.6, a: 1 }, 'OnRestButtonClicked');
const continueBtn = makeCampBtn('ContinueBtn', 'CONTINUE FORWARD\\n(Next Stage)', { x: 0, y: -30 }, { x: 210, y: 48 }, { r: 0.3, g: 0.7, b: 0.3, a: 1 }, 'OnContinueButtonClicked');
const returnBtn = makeCampBtn('ReturnBtn', 'RETURN HOME\\n(Keep All Loot)', { x: 230, y: -30 }, { x: 210, y: 48 }, { r: 0.45, g: 0.3, b: 0.6, a: 1 }, 'OnReturnHomeButtonClicked');

// Ambush Alert Banner
const ambushBanner = reg(new Node('AmbushAlertBanner'));
ambushBanner.anchorMin = { x: 0, y: 0.5 };
ambushBanner.anchorMax = { x: 1, y: 0.5 };
ambushBanner.anchoredPosition = { x: 0, y: 0 };
ambushBanner.sizeDelta = { x: 0, y: 90 };
ambushBanner.isActive = 0;
ambushBanner.crId = genId();
ambushBanner.imgId = genId();
ambushBanner.color = { r: 0.85, g: 0.15, b: 0.15, a: 0.98 };
campMainPanel.addChild(ambushBanner);

const alertText = reg(new Node('AlertText'));
alertText.anchorMin = { x: 0, y: 0 };
alertText.anchorMax = { x: 1, y: 1 };
alertText.sizeDelta = { x: 0, y: 0 };
alertText.crId = genId();
alertText.tmpId = genId();
alertText.text = 'AMBUSH! Monsters detected your campsite!';
alertText.fontSize = 20;
alertText.fontStyle = 1;
alertText.align = 514;
alertText.color = { r: 1, g: 1, b: 1, a: 1 };
ambushBanner.addChild(alertText);


// ==========================================
// Food Modal
// ==========================================
const foodModal = reg(new Node('FoodModal'));
foodModal.sizeDelta = { x: 520, y: 440 };
foodModal.isActive = 0;
foodModal.crId = genId();
foodModal.imgId = genId();
campCanvas.addChild(foodModal);

const foodTitle = reg(new Node('Title'));
foodTitle.anchorMin = { x: 0, y: 1 };
foodTitle.anchorMax = { x: 1, y: 1 };
foodTitle.pivot = { x: 0.5, y: 1 };
foodTitle.anchoredPosition = { x: 0, y: -18 };
foodTitle.sizeDelta = { x: 0, y: 30 };
foodTitle.crId = genId();
foodTitle.tmpId = genId();
foodTitle.text = 'SELECT FOOD TO EAT';
foodTitle.fontSize = 17;
foodTitle.fontStyle = 1;
foodTitle.align = 514;
foodTitle.color = { r: 1, g: 1, b: 1, a: 1 };
foodModal.addChild(foodTitle);

const foodScroll = reg(new Node('ScrollView'));
foodScroll.anchorMin = { x: 0, y: 0 };
foodScroll.anchorMax = { x: 1, y: 1 };
foodScroll.offsetMin = { x: 16, y: 60 };
foodScroll.offsetMax = { x: -16, y: -48 };
foodScroll.scrollId = genId();
foodModal.addChild(foodScroll);

const foodVp = reg(new Node('Viewport'));
foodVp.anchorMin = { x: 0, y: 0 };
foodVp.anchorMax = { x: 1, y: 1 };
foodVp.offsetMin = { x: 0, y: 0 };
foodVp.offsetMax = { x: -16, y: 0 };
foodVp.crId = genId();
foodVp.maskId = genId();
foodScroll.addChild(foodVp);

const foodContent = reg(new Node('Content'));
foodContent.anchorMin = { x: 0, y: 1 };
foodContent.anchorMax = { x: 1, y: 1 };
foodContent.pivot = { x: 0.5, y: 1 };
foodContent.anchoredPosition = { x: 0, y: 0 };
foodContent.sizeDelta = { x: 0, y: 0 };
foodContent.vlgId = genId();
foodContent.csfId = genId();
foodVp.addChild(foodContent);

const foodSb = reg(new Node('VerticalScrollbar'));
foodSb.anchorMin = { x: 1, y: 0 };
foodSb.anchorMax = { x: 1, y: 1 };
foodSb.pivot = { x: 1, y: 0.5 };
foodSb.anchoredPosition = { x: 0, y: 0 };
foodSb.sizeDelta = { x: 12, y: 0 };
foodSb.crId = genId();
foodSb.imgId = genId();
foodSb.sbId = genId();
foodScroll.addChild(foodSb);

const foodSa = reg(new Node('SlidingArea'));
foodSa.anchorMin = { x: 0, y: 0 };
foodSa.anchorMax = { x: 1, y: 1 };
foodSa.sizeDelta = { x: 0, y: 0 };
foodSb.addChild(foodSa);

const foodHandle = reg(new Node('Handle'));
foodHandle.sizeDelta = { x: 0, y: 0 };
foodHandle.crId = genId();
foodHandle.imgId = genId();
foodSa.addChild(foodHandle);

const noFoodText = reg(new Node('NoFoodText'));
noFoodText.anchoredPosition = { x: 0, y: 10 };
noFoodText.sizeDelta = { x: 400, y: 60 };
noFoodText.crId = genId();
noFoodText.tmpId = genId();
noFoodText.text = 'No food in backpack!\\n(Cook meals or harvest food from farm)';
noFoodText.fontSize = 13.5;
noFoodText.align = 514;
noFoodText.color = { r: 0.75, g: 0.75, b: 0.8, a: 1 };
foodModal.addChild(noFoodText);

const closeFoodBtn = reg(new Node('CloseBtn'));
closeFoodBtn.anchoredPosition = { x: 0, y: -188 };
closeFoodBtn.sizeDelta = { x: 130, y: 36 };
closeFoodBtn.crId = genId();
closeFoodBtn.imgId = genId();
closeFoodBtn.btnId = genId();
closeFoodBtn.btnColor = { r: 0.4, g: 0.4, b: 0.45, a: 1 };
closeFoodBtn.targetMethod = 'CloseFoodModal';
foodModal.addChild(closeFoodBtn);

const closeFoodText = reg(new Node('Text'));
closeFoodText.anchorMin = { x: 0, y: 0 };
closeFoodText.anchorMax = { x: 1, y: 1 };
closeFoodText.sizeDelta = { x: 0, y: 0 };
closeFoodText.crId = genId();
closeFoodText.tmpId = genId();
closeFoodText.text = 'CLOSE';
closeFoodText.fontSize = 12;
closeFoodText.fontStyle = 1;
closeFoodText.align = 514;
closeFoodText.color = { r: 1, g: 1, b: 1, a: 1 };
closeFoodBtn.addChild(closeFoodText);


// ==========================================
// Cook Modal
// ==========================================
const cookModal = reg(new Node('CookModal'));
cookModal.sizeDelta = { x: 520, y: 440 };
cookModal.isActive = 0;
cookModal.crId = genId();
cookModal.imgId = genId();
campCanvas.addChild(cookModal);

const cookTitle = reg(new Node('Title'));
cookTitle.anchorMin = { x: 0, y: 1 };
cookTitle.anchorMax = { x: 1, y: 1 };
cookTitle.pivot = { x: 0.5, y: 1 };
cookTitle.anchoredPosition = { x: 0, y: -18 };
cookTitle.sizeDelta = { x: 0, y: 30 };
cookTitle.crId = genId();
cookTitle.tmpId = genId();
cookTitle.text = 'SELECT RECIPE TO COOK';
cookTitle.fontSize = 17;
cookTitle.fontStyle = 1;
cookTitle.align = 514;
cookTitle.color = { r: 1, g: 1, b: 1, a: 1 };
cookModal.addChild(cookTitle);

const cookScroll = reg(new Node('ScrollView'));
cookScroll.anchorMin = { x: 0, y: 0 };
cookScroll.anchorMax = { x: 1, y: 1 };
cookScroll.offsetMin = { x: 16, y: 60 };
cookScroll.offsetMax = { x: -16, y: -48 };
cookScroll.scrollId = genId();
cookModal.addChild(cookScroll);

const cookVp = reg(new Node('Viewport'));
cookVp.anchorMin = { x: 0, y: 0 };
cookVp.anchorMax = { x: 1, y: 1 };
cookVp.offsetMin = { x: 0, y: 0 };
cookVp.offsetMax = { x: -16, y: 0 };
cookVp.crId = genId();
cookVp.maskId = genId();
cookScroll.addChild(cookVp);

const cookContent = reg(new Node('Content'));
cookContent.anchorMin = { x: 0, y: 1 };
cookContent.anchorMax = { x: 1, y: 1 };
cookContent.pivot = { x: 0.5, y: 1 };
cookContent.anchoredPosition = { x: 0, y: 0 };
cookContent.sizeDelta = { x: 0, y: 0 };
cookContent.vlgId = genId();
cookContent.csfId = genId();
cookVp.addChild(cookContent);

const cookSb = reg(new Node('VerticalScrollbar'));
cookSb.anchorMin = { x: 1, y: 0 };
cookSb.anchorMax = { x: 1, y: 1 };
cookSb.pivot = { x: 1, y: 0.5 };
cookSb.anchoredPosition = { x: 0, y: 0 };
cookSb.sizeDelta = { x: 12, y: 0 };
cookSb.crId = genId();
cookSb.imgId = genId();
cookSb.sbId = genId();
cookScroll.addChild(cookSb);

const cookSa = reg(new Node('SlidingArea'));
cookSa.anchorMin = { x: 0, y: 0 };
cookSa.anchorMax = { x: 1, y: 1 };
cookSa.sizeDelta = { x: 0, y: 0 };
cookSb.addChild(cookSa);

const cookHandle = reg(new Node('Handle'));
cookHandle.sizeDelta = { x: 0, y: 0 };
cookHandle.crId = genId();
cookHandle.imgId = genId();
cookSa.addChild(cookHandle);

const noRecipeText = reg(new Node('NoRecipeText'));
noRecipeText.anchoredPosition = { x: 0, y: 10 };
noRecipeText.sizeDelta = { x: 400, y: 60 };
noRecipeText.crId = genId();
noRecipeText.tmpId = genId();
noRecipeText.text = 'No recipes discovered yet!\\n(Meet Merchants or offer gifts to unlock recipes)';
noRecipeText.fontSize = 13.5;
noRecipeText.align = 514;
noRecipeText.color = { r: 0.75, g: 0.75, b: 0.8, a: 1 };
cookModal.addChild(noRecipeText);

const closeCookBtn = reg(new Node('CloseBtn'));
closeCookBtn.anchoredPosition = { x: 0, y: -188 };
closeCookBtn.sizeDelta = { x: 130, y: 36 };
closeCookBtn.crId = genId();
closeCookBtn.imgId = genId();
closeCookBtn.btnId = genId();
closeCookBtn.btnColor = { r: 0.4, g: 0.4, b: 0.45, a: 1 };
closeCookBtn.targetMethod = 'CloseCookModal';
cookModal.addChild(closeCookBtn);

const closeCookText = reg(new Node('Text'));
closeCookText.anchorMin = { x: 0, y: 0 };
closeCookText.anchorMax = { x: 1, y: 1 };
closeCookText.sizeDelta = { x: 0, y: 0 };
closeCookText.crId = genId();
closeCookText.tmpId = genId();
closeCookText.text = 'CLOSE';
closeCookText.fontSize = 12;
closeCookText.fontStyle = 1;
closeCookText.align = 514;
closeCookText.color = { r: 1, g: 1, b: 1, a: 1 };
closeCookBtn.addChild(closeCookText);


// ==========================================
// YAML EMISSION
// ==========================================
const out = [];
function emit(s) { out.push(s); }

// Generate YAML blocks for all nodes
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
    let col = n.btnColor || n.color || { r: 1, g: 1, b: 1, a: 1 };
    let spriteId = SPRITE_FRAME;
    let spriteGuid = GUID_FRAME;
    let imgType = 1; // Sliced
    if (n.isFilled) {
      imgType = 3; // Filled
      spriteId = '0';
      spriteGuid = '00000000000000000000000000000000';
    } else if (n.name === 'GaugeBg' || n.name === 'AmbushAlertBanner' || n.name === 'VerticalScrollbar') {
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
    let targetUI = n.father.father === campCanvas || n.father === campCanvas || n.name.includes('Modal') ? campCanvas.uiId : arrowCanvas.uiId;
    let actionMethod = n.targetMethod || 'OnArrowClicked';
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
    let align = n.align || 257; // 257 = TopLeft, 514 = Center
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
    let vp = n === foodScroll ? foodVp : cookVp;
    let content = n === foodScroll ? foodContent : cookContent;
    let sb = n === foodScroll ? foodSb : cookSb;
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
  m_Spacing: 6
  m_ChildForceExpandWidth: 1
  m_ChildForceExpandHeight: 0
  m_ChildControlWidth: 1
  m_ChildControlHeight: 0
  m_ChildScaleWidth: 0
  m_ChildScaleHeight: 0
  m_ReverseArrangement: 0`);
  }

  // 11. ContentSizeFitter
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

  // 12. Scrollbar
  if (n.sbId) {
    let handle = n === foodSb ? foodHandle : cookHandle;
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
// 1. DungeonEntranceArrowUI
emit(`--- !u!114 &${arrowCanvas.uiId}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${arrowCanvas.goId}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${GUID_ENTRANCE_ARROW_UI}, type: 3}
  m_Name: 
  m_EditorClassIdentifier: Assembly-CSharp::DungeonEntranceArrowUI
  container: {fileID: ${arrowContainer.goId}}
  arrowButton: {fileID: ${arrowContainer.btnId}}
  arrowIconRT: {fileID: ${arrowTMP.rtId}}
  titleText: {fileID: ${arrowTitle.tmpId}}
  hintText: {fileID: ${arrowHint.tmpId}}
  arrowTMP: {fileID: ${arrowTMP.tmpId}}`);

// 2. CampUI
emit(`--- !u!114 &${campCanvas.uiId}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${campCanvas.goId}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${GUID_CAMP_UI}, type: 3}
  m_Name: 
  m_EditorClassIdentifier: Assembly-CSharp::CampUI
  camp: {fileID: 1067987254}
  playerStats: {fileID: 1429561004}
  waveManager: {fileID: 1363399724}
  mainCampPanel: {fileID: ${campMainPanel.goId}}
  campTitleText: {fileID: ${campTitle.tmpId}}
  campDescText: {fileID: ${campDesc.tmpId}}
  ambushRiskPercentText: {fileID: ${riskPercentText.tmpId}}
  ambushRiskStatusText: {fileID: ${riskStatusText.tmpId}}
  ambushRiskFillImage: {fileID: ${gaugeFill.imgId}}
  ambushAlertBanner: {fileID: ${ambushBanner.goId}}
  ambushAlertText: {fileID: ${alertText.tmpId}}
  playerStatusText: {fileID: ${playerStatusText.tmpId}}
  playerBuffsText: {fileID: ${playerBuffsText.tmpId}}
  eatButton: {fileID: ${eatBtn.btnId}}
  cookButton: {fileID: ${cookBtn.btnId}}
  exerciseButton: {fileID: ${exerciseBtn.btnId}}
  restButton: {fileID: ${restBtn.btnId}}
  continueButton: {fileID: ${continueBtn.btnId}}
  returnHomeButton: {fileID: ${returnBtn.btnId}}
  foodModal: {fileID: ${foodModal.goId}}
  foodListContainer: {fileID: ${foodContent.rtId}}
  noFoodText: {fileID: ${noFoodText.tmpId}}
  closeFoodModalButton: {fileID: ${closeFoodBtn.btnId}}
  cookModal: {fileID: ${cookModal.goId}}
  recipeListContainer: {fileID: ${cookContent.rtId}}
  noRecipeText: {fileID: ${noRecipeText.tmpId}}
  closeCookModalButton: {fileID: ${closeCookBtn.btnId}}`);

// ==========================================
// APPLY TO SCENE
// ==========================================
const scenePath = 'Assets/_Scenes/Dungeon.unity';
let sceneContent = fs.readFileSync(scenePath, 'utf8');

// 1. Remove empty CampUI component from CombatCanvas if present
if (sceneContent.includes('- component: {fileID: 1873114117}')) {
  sceneContent = sceneContent.replace('  - component: {fileID: 1873114117}\r\n', '');
  sceneContent = sceneContent.replace('  - component: {fileID: 1873114117}\n', '');
  console.log('Removed empty CampUI component reference from CombatCanvas.');
}

// 2. Remove old unassigned CampUI block if present
const campUIBlockRegex = /--- !u!114 &1873114117[\s\S]*?(?=--- !u!|\Z)/;
if (campUIBlockRegex.test(sceneContent)) {
  sceneContent = sceneContent.replace(campUIBlockRegex, '');
  console.log('Removed old empty CampUI block 1873114117 from scene.');
}

// 3. Remove any previous DungeonEntranceArrowCanvas or CampCanvas if script re-runs
if (sceneContent.includes('m_Name: DungeonEntranceArrowCanvas')) {
  const arrowIdx = sceneContent.indexOf('m_Name: DungeonEntranceArrowCanvas');
  const arrowStart = sceneContent.lastIndexOf('--- !u!1 &', arrowIdx);
  sceneContent = sceneContent.substring(0, arrowStart);
  console.log('Cleaned up previous DungeonEntranceArrowCanvas blocks.');
}

const blocks = out.join('\n') + '\n';
sceneContent = sceneContent.trimEnd() + '\n' + blocks;

fs.writeFileSync(scenePath, sceneContent, 'utf8');
console.log(`Successfully inserted DungeonEntranceArrowCanvas and CampCanvas into ${scenePath}!`);
