const fs = require('fs');
const path = require('path');

// Component GUIDs
const GUID_TMP = 'f4688fdb7df04437aeb418b961361dc5';
const GUID_IMAGE = 'fe87c0e1cc204ed48ad3b37840f39efc';
const GUID_BUTTON = '4e29b1a8efbd4b44bb3f3716e73f07ff';
const GUID_SCROLLBAR = '2a4db7a114972834c8e4117be1d82ba3';
const GUID_SCROLLRECT = '1aa08ab6e0800fa44ae55d278d1423e3';
const GUID_CANVAS_SCALER = '0cd44c1031e13a943bb63640046fad76';
const GUID_GRAPHIC_RAYCASTER = 'dc42784cf147c0c48a680349fa168899';
const GUID_HLG = '30649d3a9faa99c48a7b1166b86bf2a0';
const GUID_VLG = '59f8146938fff824cb5fd77236b75775';
const GUID_GLG = '8a8695521f0d02e499659fee002a26c2';
const GUID_CSF = '3245ec927659c4140ac4f8d17403cc18';
const GUID_RECTMASK2D = '3312d7739989d2b4e91e6319e9a96d76';
const GUID_STATUE_UI = 'c42878a045405434dad65709712cbf9d';

// Asset GUIDs
const GUID_FONT = '8f586378b4e144a9851e7b34d9b748ee';
const GUID_FRAME = 'aa6f9a2ecff34c14d84e5bd91727c940';
const SPRITE_FRAME = '-5668518402485374812';
const GUID_HEADER_PLAQUE = '719e2e4a28cd1184ab3762dbc4850a4d';
const SPRITE_HEADER_PLAQUE = '-586511307516372644';
const GUID_INVENTORY_SLOT_PREFAB = '02cead57d3ffaca44b4e995473eaea19';
const FILEID_INVENTORY_SLOT = '3631650896398165525';

let nextId = 920000000;
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
    this.offsetMin = null;
    this.offsetMax = null;
    this.localRotation = { x: 0, y: 0, z: 0, w: 1 };
    this.localScale = { x: 1, y: 1, z: 1 };
  }

  addChild(child) {
    if (!this.children.includes(child)) {
      child.father = this;
      this.children.push(child);
    }
    return child;
  }
}

const allNodes = [];
function register(node) {
  allNodes.push(node);
  return node;
}

// 1. Root Canvas
const statueCanvas = register(new UnityNode('StatueCanvas', true));
statueCanvas.isActive = 1;
statueCanvas.canvasId = genId();
statueCanvas.scalerId = genId();
statueCanvas.raycasterId = genId();
statueCanvas.statueUIId = genId();

// 2. Fullscreen Dark Overlay (StatuePanel)
const overlay = register(new UnityNode('StatuePanel'));
overlay.isActive = 0; // Inactive by default
overlay.anchorMin = { x: 0, y: 0 };
overlay.anchorMax = { x: 1, y: 1 };
overlay.offsetMin = { x: 0, y: 0 };
overlay.offsetMax = { x: 0, y: 0 };
overlay.imgId = genId();
statueCanvas.addChild(overlay);

// 3. Central Window Panel (1100 x 660)
const windowNode = register(new UnityNode('Window'));
windowNode.sizeDelta = { x: 1100, y: 660 };
windowNode.imgId = genId();
overlay.addChild(windowNode);

// 4. Header: Title
const titleNode = register(new UnityNode('Title'));
titleNode.anchorMin = { x: 0, y: 1 };
titleNode.anchorMax = { x: 1, y: 1 };
titleNode.pivot = { x: 0.5, y: 1 };
titleNode.anchoredPosition = { x: 0, y: -18 };
titleNode.sizeDelta = { x: -100, y: 32 };
titleNode.tmpId = genId();
windowNode.addChild(titleNode);

// 5. Header: Subtitle (placed cleanly at y = -50, below Title which ends at -50)
const subtitleNode = register(new UnityNode('Subtitle'));
subtitleNode.anchorMin = { x: 0, y: 1 };
subtitleNode.anchorMax = { x: 1, y: 1 };
subtitleNode.pivot = { x: 0.5, y: 1 };
subtitleNode.anchoredPosition = { x: 0, y: -50 };
subtitleNode.sizeDelta = { x: -100, y: 20 };
subtitleNode.tmpId = genId();
windowNode.addChild(subtitleNode);

// 6. Sub-Header Bar (Blessing Banner placed at y = -76)
const blessingBanner = register(new UnityNode('BlessingBanner'));
blessingBanner.anchorMin = { x: 0, y: 1 };
blessingBanner.anchorMax = { x: 1, y: 1 };
blessingBanner.pivot = { x: 0.5, y: 1 };
blessingBanner.anchoredPosition = { x: 0, y: -76 };
blessingBanner.sizeDelta = { x: -48, y: 34 };
blessingBanner.imgId = genId();
windowNode.addChild(blessingBanner);

// Blessing Status Text (left side of bar)
const bStatusText = register(new UnityNode('BlessingStatusText'));
bStatusText.anchorMin = { x: 0, y: 0 };
bStatusText.anchorMax = { x: 0.79, y: 1 };
bStatusText.offsetMin = { x: 14, y: 0 };
bStatusText.offsetMax = { x: -6, y: 0 };
bStatusText.tmpId = genId();
blessingBanner.addChild(bStatusText);

// Blessing Selection Button (right side of bar)
const bSelectBtn = register(new UnityNode('BtnSelectBlessing'));
bSelectBtn.anchorMin = { x: 0.80, y: 0 };
bSelectBtn.anchorMax = { x: 1, y: 1 };
bSelectBtn.offsetMin = { x: 4, y: 3 };
bSelectBtn.offsetMax = { x: -6, y: -3 };
bSelectBtn.imgId = genId();
bSelectBtn.btnId = genId();
blessingBanner.addChild(bSelectBtn);

const bBtnText = register(new UnityNode('Text'));
bBtnText.anchorMin = { x: 0, y: 0 };
bBtnText.anchorMax = { x: 1, y: 1 };
bBtnText.sizeDelta = { x: 0, y: 0 };
bBtnText.tmpId = genId();
bSelectBtn.addChild(bBtnText);

// 7. Feedback / Hint Strip (at y = -116)
const feedbackNode = register(new UnityNode('Feedback'));
feedbackNode.anchorMin = { x: 0, y: 1 };
feedbackNode.anchorMax = { x: 1, y: 1 };
feedbackNode.pivot = { x: 0.5, y: 1 };
feedbackNode.anchoredPosition = { x: 0, y: -116 };
feedbackNode.sizeDelta = { x: -48, y: 18 };
feedbackNode.tmpId = genId();
windowNode.addChild(feedbackNode);

// 8. Close Button (X)
const closeBtn = register(new UnityNode('BtnClose'));
closeBtn.anchorMin = { x: 1, y: 1 };
closeBtn.anchorMax = { x: 1, y: 1 };
closeBtn.pivot = { x: 1, y: 1 };
closeBtn.anchoredPosition = { x: -14, y: -14 };
closeBtn.sizeDelta = { x: 30, y: 30 };
closeBtn.imgId = genId();
closeBtn.btnId = genId();
windowNode.addChild(closeBtn);

const xText = register(new UnityNode('X'));
xText.anchorMin = { x: 0, y: 0 };
xText.anchorMax = { x: 1, y: 1 };
xText.sizeDelta = { x: 30, y: 30 };
xText.tmpId = genId();
closeBtn.addChild(xText);

// 9. Left Column: Offerings Column (610 x 460 at y = -140)
const offCol = register(new UnityNode('OfferingsColumn'));
offCol.anchorMin = { x: 0, y: 1 };
offCol.anchorMax = { x: 0, y: 1 };
offCol.pivot = { x: 0, y: 1 };
offCol.anchoredPosition = { x: 24, y: -140 };
offCol.sizeDelta = { x: 610, y: 460 };
offCol.imgId = genId();
windowNode.addChild(offCol);

const offHeader = register(new UnityNode('Header'));
offHeader.anchorMin = { x: 0, y: 1 };
offHeader.anchorMax = { x: 1, y: 1 };
offHeader.pivot = { x: 0.5, y: 1 };
offHeader.anchoredPosition = { x: 0, y: -10 };
offHeader.sizeDelta = { x: -20, y: 26 };
offHeader.tmpId = genId();
offCol.addChild(offHeader);

const offScroll = register(new UnityNode('ScrollView'));
offScroll.anchorMin = { x: 0, y: 0 };
offScroll.anchorMax = { x: 1, y: 1 };
offScroll.offsetMin = { x: 10, y: 10 };
offScroll.offsetMax = { x: -10, y: -40 };
offScroll.scrollRectId = genId();
offCol.addChild(offScroll);

const offViewport = register(new UnityNode('Viewport'));
offViewport.anchorMin = { x: 0, y: 0 };
offViewport.anchorMax = { x: 1, y: 1 };
offViewport.offsetMin = { x: 0, y: 0 };
offViewport.offsetMax = { x: -20, y: 0 };
offViewport.maskId = genId();
offScroll.addChild(offViewport);

const offContent = register(new UnityNode('OfferingsContent'));
offContent.anchorMin = { x: 0, y: 1 };
offContent.anchorMax = { x: 1, y: 1 };
offContent.pivot = { x: 0.5, y: 1 };
offContent.anchoredPosition = { x: 0, y: 0 };
offContent.sizeDelta = { x: 0, y: 0 };
offContent.vlgId = genId();
offContent.csfId = genId();
offViewport.addChild(offContent);

const offScrollbar = register(new UnityNode('Scrollbar'));
offScrollbar.anchorMin = { x: 1, y: 0 };
offScrollbar.anchorMax = { x: 1, y: 1 };
offScrollbar.pivot = { x: 1, y: 0.5 };
offScrollbar.anchoredPosition = { x: 0, y: 0 };
offScrollbar.sizeDelta = { x: 14, y: 0 };
offScrollbar.imgId = genId();
offScrollbar.scrollbarId = genId();
offScroll.addChild(offScrollbar);

const offSlideArea = register(new UnityNode('Sliding Area'));
offSlideArea.anchorMin = { x: 0, y: 0 };
offSlideArea.anchorMax = { x: 1, y: 1 };
offSlideArea.offsetMin = { x: 2, y: 4 };
offSlideArea.offsetMax = { x: -2, y: -4 };
offScrollbar.addChild(offSlideArea);

const offHandle = register(new UnityNode('Handle'));
offHandle.anchorMin = { x: 0, y: 0 };
offHandle.anchorMax = { x: 1, y: 1 };
offHandle.offsetMin = { x: 0, y: 0 };
offHandle.offsetMax = { x: 0, y: 0 };
offHandle.imgId = genId();
offSlideArea.addChild(offHandle);

// 10. Right Column: Backpack Column (420 x 460 at y = -140)
const bpCol = register(new UnityNode('BackpackColumn'));
bpCol.anchorMin = { x: 1, y: 1 };
bpCol.anchorMax = { x: 1, y: 1 };
bpCol.pivot = { x: 1, y: 1 };
bpCol.anchoredPosition = { x: -24, y: -140 };
bpCol.sizeDelta = { x: 420, y: 460 };
bpCol.imgId = genId();
windowNode.addChild(bpCol);

const bpHeader = register(new UnityNode('Header'));
bpHeader.anchorMin = { x: 0, y: 1 };
bpHeader.anchorMax = { x: 1, y: 1 };
bpHeader.pivot = { x: 0.5, y: 1 };
bpHeader.anchoredPosition = { x: 0, y: -10 };
bpHeader.sizeDelta = { x: -20, y: 26 };
bpHeader.tmpId = genId();
bpCol.addChild(bpHeader);

const bpScroll = register(new UnityNode('ScrollView'));
bpScroll.anchorMin = { x: 0, y: 0 };
bpScroll.anchorMax = { x: 1, y: 1 };
bpScroll.offsetMin = { x: 10, y: 10 };
bpScroll.offsetMax = { x: -10, y: -40 };
bpScroll.scrollRectId = genId();
bpCol.addChild(bpScroll);

const bpViewport = register(new UnityNode('Viewport'));
bpViewport.anchorMin = { x: 0, y: 0 };
bpViewport.anchorMax = { x: 1, y: 1 };
bpViewport.offsetMin = { x: 0, y: 0 };
bpViewport.offsetMax = { x: -20, y: 0 };
bpViewport.maskId = genId();
bpScroll.addChild(bpViewport);

const bpContent = register(new UnityNode('BackpackSlots'));
bpContent.anchorMin = { x: 0, y: 1 };
bpContent.anchorMax = { x: 1, y: 1 };
bpContent.pivot = { x: 0.5, y: 1 };
bpContent.anchoredPosition = { x: 0, y: 0 };
bpContent.sizeDelta = { x: 0, y: 0 };
bpContent.glgId = genId();
bpContent.csfId = genId();
bpViewport.addChild(bpContent);

const bpScrollbar = register(new UnityNode('Scrollbar'));
bpScrollbar.anchorMin = { x: 1, y: 0 };
bpScrollbar.anchorMax = { x: 1, y: 1 };
bpScrollbar.pivot = { x: 1, y: 0.5 };
bpScrollbar.anchoredPosition = { x: 0, y: 0 };
bpScrollbar.sizeDelta = { x: 14, y: 0 };
bpScrollbar.imgId = genId();
bpScrollbar.scrollbarId = genId();
bpScroll.addChild(bpScrollbar);

const bpSlideArea = register(new UnityNode('Sliding Area'));
bpSlideArea.anchorMin = { x: 0, y: 0 };
bpSlideArea.anchorMax = { x: 1, y: 1 };
bpSlideArea.offsetMin = { x: 2, y: 4 };
bpSlideArea.offsetMax = { x: -2, y: -4 };
bpScrollbar.addChild(bpSlideArea);

const bpHandle = register(new UnityNode('Handle'));
bpHandle.anchorMin = { x: 0, y: 0 };
bpHandle.anchorMax = { x: 1, y: 1 };
bpHandle.offsetMin = { x: 0, y: 0 };
bpHandle.offsetMax = { x: 0, y: 0 };
bpHandle.imgId = genId();
bpSlideArea.addChild(bpHandle);

// 11. Footer: Permanent Stats Summary (Docked at bottom y = 12)
const footerNode = register(new UnityNode('Footer'));
footerNode.anchorMin = { x: 0, y: 0 };
footerNode.anchorMax = { x: 1, y: 0 };
footerNode.pivot = { x: 0.5, y: 0 };
footerNode.anchoredPosition = { x: 0, y: 12 };
footerNode.sizeDelta = { x: -48, y: 36 };
footerNode.imgId = genId();
windowNode.addChild(footerNode);

const statsText = register(new UnityNode('StatsText'));
statsText.anchorMin = { x: 0, y: 0 };
statsText.anchorMax = { x: 1, y: 1 };
statsText.sizeDelta = { x: 0, y: 0 };
statsText.tmpId = genId();
footerNode.addChild(statsText);

// Assign CanvasRenderer for graphic nodes
for (const n of allNodes) {
  if (n.imgId || n.tmpId) {
    n.crId = genId();
  }
}

// Build YAML blocks
const out = [];
function emit(text) {
  out.push(text);
}

for (const n of allNodes) {
  // 1. GameObject
  const compRefs = [`  - component: {fileID: ${n.rtId}}`];
  if (n.canvasId) compRefs.push(`  - component: {fileID: ${n.canvasId}}`);
  if (n.scalerId) compRefs.push(`  - component: {fileID: ${n.scalerId}}`);
  if (n.raycasterId) compRefs.push(`  - component: {fileID: ${n.raycasterId}}`);
  if (n.statueUIId) compRefs.push(`  - component: {fileID: ${n.statueUIId}}`);
  if (n.crId) compRefs.push(`  - component: {fileID: ${n.crId}}`);
  if (n.imgId) compRefs.push(`  - component: {fileID: ${n.imgId}}`);
  if (n.tmpId) compRefs.push(`  - component: {fileID: ${n.tmpId}}`);
  if (n.btnId) compRefs.push(`  - component: {fileID: ${n.btnId}}`);
  if (n.scrollRectId) compRefs.push(`  - component: {fileID: ${n.scrollRectId}}`);
  if (n.maskId) compRefs.push(`  - component: {fileID: ${n.maskId}}`);
  if (n.scrollbarId) compRefs.push(`  - component: {fileID: ${n.scrollbarId}}`);
  if (n.vlgId) compRefs.push(`  - component: {fileID: ${n.vlgId}}`);
  if (n.glgId) compRefs.push(`  - component: {fileID: ${n.glgId}}`);
  if (n.csfId) compRefs.push(`  - component: {fileID: ${n.csfId}}`);

  emit(`--- !u!1 &${n.goId}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
${compRefs.join('\n')}
  m_Layer: ${n.layer}
  m_Name: ${n.name}
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: ${n.isActive}`);

  // 2. RectTransform
  const childrenLines = n.children.map(c => `  - {fileID: ${c.rtId}}`).join('\n');
  const fatherRef = n.father ? `{fileID: ${n.father.rtId}}` : `{fileID: 0}`;
  const scale = n.isRoot ? `{x: 0, y: 0, z: 0}` : `{x: 1, y: 1, z: 1}`;
  const anchorMin = n.isRoot ? `{x: 0, y: 0}` : `{x: ${n.anchorMin.x}, y: ${n.anchorMin.y}}`;
  const anchorMax = n.isRoot ? `{x: 0, y: 0}` : `{x: ${n.anchorMax.x}, y: ${n.anchorMax.y}}`;
  const pivot = n.isRoot ? `{x: 0, y: 0}` : `{x: ${n.pivot.x}, y: ${n.pivot.y}}`;
  const sizeDelta = n.isRoot ? `{x: 0, y: 0}` : `{x: ${n.sizeDelta.x}, y: ${n.sizeDelta.y}}`;
  const anchoredPos = n.isRoot ? `{x: 0, y: 0}` : `{x: ${n.anchoredPosition.x}, y: ${n.anchoredPosition.y}}`;

  let offsetLines = '';
  if (n.offsetMin && n.offsetMax) {
    offsetLines = `\n  m_OffsetMin: {x: ${n.offsetMin.x}, y: ${n.offsetMin.y}}\n  m_OffsetMax: {x: ${n.offsetMax.x}, y: ${n.offsetMax.y}}`;
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
  m_LocalScale: ${scale}
  m_ConstrainProportionsScale: 0
  m_Children: ${childrenLines ? '\n' + childrenLines : '[]'}
  m_Father: ${fatherRef}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: ${anchorMin}
  m_AnchorMax: ${anchorMax}
  m_AnchoredPosition: ${anchoredPos}
  m_SizeDelta: ${sizeDelta}
  m_Pivot: ${pivot}${offsetLines}`);

  // Canvas components on Root
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
  m_SortingOrder: 22
  m_TargetDisplay: 0`);
  }

  if (n.scalerId) {
    emit(`--- !u!114 &${n.scalerId}
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
  m_PresetInfoIsWorld: 0`);
  }

  if (n.raycasterId) {
    emit(`--- !u!114 &${n.raycasterId}
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

  if (n.statueUIId) {
    emit(`--- !u!114 &${n.statueUIId}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${n.goId}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${GUID_STATUE_UI}, type: 3}
  m_Name: 
  m_EditorClassIdentifier: Assembly-CSharp::StatueUI
  statuePanel: {fileID: ${overlay.goId}}
  offeringsContainer: {fileID: ${offContent.rtId}}
  backpackSlotsParent: {fileID: ${bpContent.rtId}}
  canvas: {fileID: ${statueCanvas.canvasId}}
  slotPrefab: {fileID: ${FILEID_INVENTORY_SLOT}, guid: ${GUID_INVENTORY_SLOT_PREFAB}, type: 3}
  titleText: {fileID: ${titleNode.tmpId}}
  subtitleText: {fileID: ${subtitleNode.tmpId}}
  feedbackText: {fileID: ${feedbackNode.tmpId}}
  permanentStatsText: {fileID: ${statsText.tmpId}}
  closeButton: {fileID: ${closeBtn.btnId}}
  blessingStatusText: {fileID: ${bStatusText.tmpId}}
  blessingSelectButton: {fileID: ${bSelectBtn.btnId}}
  blessingButtonText: {fileID: ${bBtnText.tmpId}}`);
  }

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
    let r = 1, g = 1, b = 1, a = 1;
    let spriteRef = '{fileID: 0}';
    let type = 0; // Simple

    if (n.name === 'StatuePanel') {
      r = 0.04; g = 0.05; b = 0.08; a = 0.85;
    } else if (n.name === 'Window') {
      spriteRef = `{fileID: ${SPRITE_FRAME}, guid: ${GUID_FRAME}, type: 3}`;
      type = 1; // Sliced
      r = 0.28; g = 0.22; b = 0.16; a = 0.98;
    } else if (n.name === 'BlessingBanner') {
      spriteRef = `{fileID: ${SPRITE_HEADER_PLAQUE}, guid: ${GUID_HEADER_PLAQUE}, type: 3}`;
      type = 1;
      r = 0.95; g = 0.82; b = 0.45; a = 1;
    } else if (n.name === 'BtnSelectBlessing') {
      spriteRef = `{fileID: ${SPRITE_FRAME}, guid: ${GUID_FRAME}, type: 3}`;
      type = 1;
      r = 0.18; g = 0.52; b = 0.32; a = 1;
    } else if (n.name === 'BtnClose') {
      spriteRef = `{fileID: ${SPRITE_FRAME}, guid: ${GUID_FRAME}, type: 3}`;
      type = 1;
      r = 0.45; g = 0.18; b = 0.18; a = 1;
    } else if (n.name === 'OfferingsColumn' || n.name === 'BackpackColumn') {
      spriteRef = `{fileID: ${SPRITE_FRAME}, guid: ${GUID_FRAME}, type: 3}`;
      type = 1;
      r = 0.16; g = 0.18; b = 0.22; a = 0.95;
    } else if (n.name === 'Scrollbar') {
      r = 0.06; g = 0.08; b = 0.12; a = 0.9;
    } else if (n.name === 'Handle') {
      if (n.father.father.father.name === 'OfferingsColumn') {
        r = 0.85; g = 0.65; b = 0.2; a = 1;
      } else {
        r = 0.25; g = 0.75; b = 0.95; a = 1;
      }
    } else if (n.name === 'Footer') {
      spriteRef = `{fileID: ${SPRITE_HEADER_PLAQUE}, guid: ${GUID_HEADER_PLAQUE}, type: 3}`;
      type = 1;
      r = 0.28; g = 0.22; b = 0.16; a = 0.98;
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
    let cr = 1, cg = 1, cb = 1, ca = 1;

    if (n.name === 'Title') {
      text = 'ANCIENT SHRINE (DIVINE BLESSING STATUE)';
      size = 20; align = 514; cr = 1; cg = 0.86; cb = 0.35;
    } else if (n.name === 'Subtitle') {
      text = 'Make sacred offerings to gain permanent stats and unlock tactical build paths.';
      size = 12; align = 514; cr = 0.75; cg = 0.82; cb = 0.92; bold = 0;
    } else if (n.name === 'BlessingStatusText') {
      text = '<b><color=#FFCC44>[NO BLESSING CHOSEN]</color></b> Choose a blessing to empower your run before entering the Dungeon!';
      size = 11.5; align = 513; cr = 0.9; cg = 0.95; cb = 1; bold = 0;
    } else if (n.name === 'Text' && n.father.name === 'BtnSelectBlessing') {
      text = 'Change Build'; size = 12; align = 514; cr = 1; cg = 1; cb = 1;
    } else if (n.name === 'Feedback') {
      text = 'Drag and drop items from your backpack or click requirement slots to offer.';
      size = 12; align = 514; cr = 0.85; cg = 0.92; cb = 1; bold = 0;
    } else if (n.name === 'X') {
      text = 'X'; size = 16; align = 514; cr = 1; cg = 1; cb = 1;
    } else if (n.name === 'Header') {
      if (n.father.name === 'OfferingsColumn') {
        text = 'DIVINE OFFERINGS LIST'; size = 14; align = 514; cr = 0.85; cg = 0.7; cb = 0.25;
      } else {
        text = 'PLAYER INVENTORY (BACKPACK)'; size = 14; align = 514; cr = 0.2; cg = 0.7; cb = 0.95;
      }
    } else if (n.name === 'StatsText') {
      text = '<b>Blessed Permanent Stats:</b>   <color=#FF7777>ATK: 10 (+0)</color>   |   <color=#77AAFF>DEF: 0 (+0)</color>   |   <color=#FFFF77>Speed: 10 (+0)</color>';
      size = 13; align = 514; cr = 0.9; cg = 0.95; cb = 1;
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
  m_fontSizeMin: 18
  m_fontSizeMax: 72
  m_fontStyle: ${bold}
  m_HorizontalAlignment: 2
  m_VerticalAlignment: 512
  m_textAlignment: ${align}
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
    let callMethod = 'Close';
    if (n.name === 'BtnSelectBlessing') callMethod = 'ShowBlessingSelectionModal';

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
  m_Material: {fileID: 0}
  m_Color: {r: 1, g: 1, b: 1, a: 1}
  m_RaycastTarget: 1
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Navigation:
    m_Mode: 0
    m_WrapAround: 0
    m_SelectOnUp: {fileID: 0}
    m_SelectOnDown: {fileID: 0}
    m_SelectOnLeft: {fileID: 0}
    m_SelectOnRight: {fileID: 0}
  m_Transition: 1
  m_Colors:
    m_NormalColor: {r: 1, g: 1, b: 1, a: 1}
    m_HighlightedColor: {r: 0.95, g: 0.95, b: 0.95, a: 1}
    m_PressedColor: {r: 0.78, g: 0.78, b: 0.78, a: 1}
    m_SelectedColor: {r: 0.95, g: 0.95, b: 0.95, a: 1}
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
  m_TargetGraphic: {fileID: ${n.imgId}}
  m_OnClick:
    m_PersistentCalls:
      m_Calls:
      - m_Target: {fileID: ${statueCanvas.statueUIId}}
        m_TargetAssemblyTypeName: StatueUI, Assembly-CSharp
        m_MethodName: ${callMethod}
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

  if (n.scrollRectId) {
    const isOff = n.father.name === 'OfferingsColumn';
    const contentRef = isOff ? offContent.rtId : bpContent.rtId;
    const vpRef = isOff ? offViewport.rtId : bpViewport.rtId;
    const sbRef = isOff ? offScrollbar.scrollbarId : bpScrollbar.scrollbarId;

    emit(`--- !u!114 &${n.scrollRectId}
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
  m_Content: {fileID: ${contentRef}}
  m_Horizontal: 0
  m_Vertical: 1
  m_MovementType: 1
  m_Elasticity: 0.1
  m_Inertia: 1
  m_DecelerationRate: 0.135
  m_ScrollSensitivity: 30
  m_Viewport: {fileID: ${vpRef}}
  m_HorizontalScrollbar: {fileID: 0}
  m_VerticalScrollbar: {fileID: ${sbRef}}
  m_HorizontalScrollbarVisibility: 2
  m_VerticalScrollbarVisibility: 1
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

  if (n.scrollbarId) {
    const isOff = n.father.father.name === 'OfferingsColumn';
    const handleRef = isOff ? offHandle.rtId : bpHandle.rtId;

    emit(`--- !u!114 &${n.scrollbarId}
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
  m_Material: {fileID: 0}
  m_Color: {r: 1, g: 1, b: 1, a: 1}
  m_RaycastTarget: 1
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Navigation:
    m_Mode: 0
    m_WrapAround: 0
    m_SelectOnUp: {fileID: 0}
    m_SelectOnDown: {fileID: 0}
    m_SelectOnLeft: {fileID: 0}
    m_SelectOnRight: {fileID: 0}
  m_Transition: 1
  m_Colors:
    m_NormalColor: {r: 1, g: 1, b: 1, a: 1}
    m_HighlightedColor: {r: 0.95, g: 0.95, b: 0.95, a: 1}
    m_PressedColor: {r: 0.78, g: 0.78, b: 0.78, a: 1}
    m_SelectedColor: {r: 0.95, g: 0.95, b: 0.95, a: 1}
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
  m_TargetGraphic: {fileID: ${n.imgId}}
  m_HandleRect: {fileID: ${handleRef}}
  m_Direction: 2
  m_Value: 1
  m_Size: 0.2
  m_NumberOfSteps: 0
  m_OnValueChanged:
    m_PersistentCalls:
      m_Calls: []`);
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
    m_Left: 6
    m_Right: 6
    m_Top: 8
    m_Bottom: 8
  m_ChildAlignment: 0
  m_Spacing: 10
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
    m_Top: 8
    m_Bottom: 8
  m_CellSize: {x: 66, y: 66}
  m_Spacing: {x: 8, y: 8}
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
}

// Write to Base.unity cleanly
const scenePath = 'Assets/_Scenes/Base.unity';
let sceneContent = fs.readFileSync(scenePath, 'utf8');

const blocks = sceneContent.split('--- !u!');
const cleanBlocks = [];
for (let i = 0; i < blocks.length; i++) {
  const b = blocks[i];
  if (i === 0) { cleanBlocks.push(b); continue; }
  const headerMatch = b.match(/^(\d+) &(\d+)/);
  if (!headerMatch) { cleanBlocks.push(b); continue; }
  const fileId = parseInt(headerMatch[2]);
  
  // Strip any old 920000000..920000999 series block
  if (fileId >= 920000000 && fileId <= 920000999) {
    continue;
  }
  // Strip any dangling CanvasRenderer pointing to 920000...
  if (headerMatch[1] === '222' && b.includes('m_GameObject: {fileID: 920000')) {
    continue;
  }
  cleanBlocks.push(b);
}

let cleanedScene = cleanBlocks.join('--- !u!');

// Ensure StatueCanvas root (920000001) is in SceneRoots
const rootsRegex = /(--- !u!1660057539 &9223372036854775807[\s\S]*?m_Roots:)([\s\S]*?)(?=(--- !u!|$))/;
cleanedScene = cleanedScene.replace(rootsRegex, (m, header, roots) => {
  let rootLines = roots.trim().split('\n').map(l => l.trim()).filter(l => l.length > 0);
  if (!rootLines.includes('- {fileID: 920000001}')) {
    rootLines.push('- {fileID: 920000001}');
  }
  rootLines = Array.from(new Set(rootLines));
  return header + '\n  ' + rootLines.join('\n  ') + '\n';
});

const sceneBlocks = out.join('\n') + '\n';
fs.writeFileSync(scenePath, cleanedScene.trimEnd() + '\n' + sceneBlocks, 'utf8');
console.log(`Cleanly rebuilt and inserted StatueCanvas into ${scenePath}!`);
