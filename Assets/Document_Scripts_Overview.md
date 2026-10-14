# 18. TỔNG HỢP KIẾN TRÚC MÃ NGUỒN (SCRIPTS) & CHI TIẾT TẤT CẢ CÁC HÀM TRONG DỰ ÁN

Tài liệu kỹ thuật hệ thống hóa toàn bộ 71 Script cốt lõi trong dự án Unity 2D Farming (`Assets/_Scripts/`), phân loại chi tiết theo 10 phân hệ nghiệp vụ, bao gồm mục đích của từng script và giải thích chi tiết chức năng của từng hàm (methods).

---

## 18.1. Phân hệ Root / Cốt lõi hệ thống (Assets/_Scripts/)

### 1. CameraController.cs
* **Tác dụng**: Điều khiển Camera 2D bám theo nhân vật chính (Player) mượt mà với độ trễ (smoothing) và khoảng bù trừ (offset).
  - *Start()*: Khởi tạo, tự động tìm kiếm đối tượng người chơi (Player) nếu chưa được gán target.
  - *FixedUpdate()*: Tính toán vị trí mục tiêu kèm offset và di chuyển camera mượt mà bằng Vector3.Lerp.

### 2. DayManager.cs
* **Tác dụng**: Quản lý vòng lặp ngày đêm (Day-Night cycle), đếm số ngày, kích hoạt sự kiện sang ngày mới cho toàn bộ các hệ thống nông trại, hồi phục và thương nhân.
  - *ResetStatics()*: Đặt lại các biến tĩnh khi nạp lại scene / restart game.
  - *Awake()*: Thiết lập Singleton (DayManager.Instance).
  - *Start()*: Bắt đầu coroutine đếm thời gian trong ngày.
  - *DayTimer()*: Coroutine đếm thời gian thực của một ngày (dayDuration), tự động kích hoạt sang ngày mới khi hết giờ.
  - *Sleep()*: Xử lý hành động người chơi đi ngủ trên giường để lập tức kết thúc ngày cũ và chuyển sang ngày mới.
  - *AdvanceDay()*: Tăng số đếm ngày (currentDay++) và kích hoạt sự kiện OnNewDay cho toàn bộ hệ thống.
  - *GetCurrentDay()*: Trả về số thứ tự ngày hiện tại.
  - *OnApplicationQuit()*: Dọn dẹp tài nguyên và sự kiện khi thoát ứng dụng.
  - *OnDestroy()*: Hủy đăng ký sự kiện và giải phóng Singleton.

### 3. DungeonEntranceUI.cs
* **Tác dụng**: Giao diện Modal chọn tầng hầm ngục (Floor Selection UI) trước khi vào Dungeon, hiển thị trạng thái mở/khóa tầng, Boss và chuyển cảnh.
  - *Awake() / Start()*: Khởi tạo, liên kết các thành phần giao diện, tự dựng modal UI nếu chưa có sẵn.
  - *OnDestroy()*: Hủy đăng ký sự kiện các nút bấm.
  - *Update()*: Bắt phím tắt (như ESC) để đóng cửa sổ chọn tầng.
  - *InitializeFloorData()*: Nạp dữ liệu cấu hình các tầng hầm ngục (FloorData) trong game.
  - *Open()*: Mở modal chọn tầng và làm mới danh sách hiển thị.
  - *Close()*: Đóng modal chọn tầng.
  - *EnterDungeon()*: Bắt đầu tiến vào tầng đã chọn hoặc tầng mặc định.
  - *EnterSelectedFloor()*: Kiểm tra điều kiện mở khóa tầng, gọi chuyển cảnh sang Dungeon nếu hợp lệ.
  - *SelectFloor(int floor)*: Chọn một tầng cụ thể để xem chi tiết thông số/Boss.
  - *IsFloorUnlocked(int floor)*: Kiểm tra tầng chỉ định đã được mở khóa qua ProgressionManager hay chưa.
  - *IsBossDefeated(int floor)*: Kiểm tra Boss của tầng đã bị đánh bại hay chưa.
  - *GetFloorData(int floor)*: Lấy dữ liệu chi tiết của một tầng theo số thứ tự.
  - *RefreshUI()*: Làm mới màu sắc và trạng thái mở khóa của toàn bộ thẻ tầng (floor cards).
  - *ShowWarning(string warning)*: Hiển thị thông báo cảnh báo tạm thời khi người chơi chọn tầng chưa mở.
  - *EnsureUIConstructed()*: Đảm bảo toàn bộ cấu trúc UI của modal được khởi tạo đầy đủ.
  - *BuildModalUI()*: Dựng động hệ thống nền mờ, khung cuộn và các nút bấm.
  - *CreateFloorCard(Transform parent, int floorNum)*: Tạo một thẻ hiển thị tầng gồm số tầng, tên, trạng thái và nút chọn.

### 4. FarmPlot.cs
* **Tác dụng**: Quản lý từng ô đất trồng trọt (trạng thái đất, gieo hạt giống, sinh trưởng cây qua các ngày, thu hoạch nông sản).
  - *Planted()*: Kiểm tra ô đất hiện tại đã được gieo hạt hay chưa.
  - *EnsureAvailableCrops()*: Tự động nạp danh sách dữ liệu các loại cây trồng hợp lệ.
  - *Harvest()*: Xử lý thu hoạch khi cây đã chín: sinh ra item rơi ra đất và đưa ô đất về trạng thái ban đầu.
  - *SetDefault()*: Đặt lại ô đất về trạng thái đất trống chưa trồng.
  - *NextDay()*: Xử lý khi bước sang ngày mới: tăng ngày sinh trưởng của cây, cập nhật Sprite hiển thị theo giai đoạn lớn lên.
  - *Start()*: Đăng ký lắng nghe sự kiện chuyển ngày từ DayManager.
  - *OnDestroy()*: Hủy đăng ký lắng nghe sự kiện sang ngày mới.

### 5. GameAssetHelper.cs
* **Tác dụng**: Lớp tiện ích tĩnh trung tâm nạp và cache an toàn các ScriptableObject (ItemData, CropData, RecipeData, FloorData, v.v.) trong Unity Editor và Runtime.
  - *LoadAll<T>()*: Nạp toàn bộ tài nguyên thuộc kiểu T có trong dự án với bộ nhớ đệm cache hiệu năng cao.
  - *ClearCache()*: Xóa sạch bộ nhớ đệm cache tài nguyên.

### 6. HealthBar.cs
* **Tác dụng**: Thanh máu hiển thị trên giao diện thế giới cho Player hoặc Enemy, hỗ trợ hiệu ứng rút máu trễ mượt mà (Ghost Fill) và tương tác click chọn mục tiêu.
  - *Awake() / OnDisable()*: Khởi tạo các thành phần Image lấp đầy và dừng coroutine khi tắt.
  - *EnsureGhostHealthBar()*: Tự động tạo thanh ghost bar tụt chậm đằng sau thanh máu chính.
  - *OnPointerClick(PointerEventData)*: Bắt sự kiện click chuột vào thanh máu của quái để chọn mục tiêu tấn công.
  - *SetHealthBar(int currentHealth, int maxHealth)*: Cập nhật chỉ số máu và thanh fill chính.
  - *GhostDrainRoutine(float targetFill)*: Coroutine hạ thanh ghost bar từ từ tạo cảm giác mượt mà và trực quan.
  - *SetEnemyTarget(EnemyStats enemy)*: Gán kẻ địch mà thanh máu này đang theo dõi.
  - *SetPlayerTarget(PlayerStats player)*: Gán người chơi mà thanh máu này đang theo dõi.
  - *Update()*: Đồng bộ vị trí thanh máu bay theo vị trí đầu của nhân vật/quái trong thế giới 2D.

### 7. HealthBarManager.cs
* **Tác dụng**: Quản lý tập trung vòng đời sinh ra, cập nhật và dọn dẹp các thanh máu của người chơi và danh sách quái trong trận đánh.
  - *Awake() / Update()*: Khởi tạo và quản lý vị trí hiển thị thanh máu.
  - *SetEnemies(EnemyStats[] newEnemies)*: Cập nhật danh sách kẻ địch hiện diện trong đợt chiến đấu.
  - *CreateHealthBars()*: Khởi tạo thanh máu cho người chơi và toàn bộ kẻ địch còn sống.
  - *SpawnHealthBar(PlayerStats player)*: Khởi tạo thanh máu riêng cho Player.
  - *SpawnHealthBar(EnemyStats enemy)*: Khởi tạo thanh máu riêng cho từng kẻ địch.
  - *ClearHealthBars()*: Dọn dẹp tất cả thanh máu hiện có.
  - *ClearEnemyHealthBars()*: Chỉ xóa các thanh máu của kẻ địch (giữ lại thanh máu người chơi).
  - *ClearAllHealthBars()*: Xóa sạch hoàn toàn tất cả thanh máu trên màn hình.

### 8. IInteractable.cs
* **Tác dụng**: Interface định nghĩa cho các đối tượng có khả năng tương tác phím E hoặc click chuột trong thế giới (Rương, Bếp, Giường, Tượng thần).
  - *Interact()*: Phương thức thực thi logic tương tác chính khi người chơi kích hoạt.

### 9. ItemRequirement.cs
* **Tác dụng**: Lớp dữ liệu cấu hình nguyên liệu yêu cầu (ItemData item, int amount) dùng cho công thức nấu ăn và cống phẩm dâng tượng thần. Không chứa hàm xử lý logic.

### 10. Offering.cs
* **Tác dụng**: ScriptableObject cấu hình cống phẩm cho Tượng Thần (vật phẩm yêu cầu, loại chỉ số vĩnh viễn nhận được: ATK/DEF/Speed, số lượng cộng thêm, trạng thái hoàn thành). Không chứa hàm xử lý logic.

### 11. SceneTransitionManager.cs
* **Tác dụng**: Quản lý chuyển đổi scene giữa Nông trại/Căn cứ (Base) và Hầm ngục (Dungeon).
  - *Awake()*: Thiết lập Singleton chuyển cảnh.
  - *LoadDungeon(int floor)*: Thiết lập tầng hầm ngục chỉ định và tải scene Dungeon.
  - *LoadBase()*: Tải scene trở về Căn cứ.
  - *LoadScene(string sceneName)*: Tải scene bất kỳ theo tên chuỗi.

### 12. Statue.cs
* **Tác dụng**: Quản lý Tượng Thần tại Căn cứ, tiếp nhận nông sản/vật phẩm cống nạp để mở khóa các chỉ số vĩnh viễn (Meta Progression) cho nhân vật.
  - *NotifyOfferingSuccess(Offering offering)*: Thông báo hoàn thành cống phẩm và cập nhật chỉ số thưởng.
  - *Start() / Update()*: Quản lý khoảng cách tiếp cận của người chơi và hiển thị gợi ý phím tương tác.
  - *OnMouseDown() / Interact()*: Bấm chuột hoặc nhấn phím tương tác để mở giao diện StatueUI.
  - *GetStatueCenter()*: Lấy tọa độ trung tâm của Tượng Thần trong không gian thế giới.
  - *FindPlayer() / EnsureReferences()*: Tìm kiếm người chơi và thiết lập các thành phần cần thiết.
  - *CanOffer(Offering offering, ItemContainer container)*: Kiểm tra túi đồ người chơi có đủ nguyên liệu để nộp cống phẩm hay không.
  - *MakeOffering(Offering offering, ItemContainer container)*: Trừ nguyên liệu trong ba lô, hoàn thành cống phẩm và cộng vĩnh viễn chỉ số vào ProgressionManager.
  - *TryOfferAny()*: Tự động tìm và nộp cống phẩm hợp lệ đầu tiên mà người chơi có đủ đồ.
  - *GetItemCount(ItemContainer container, ItemData item)*: Đếm số lượng một item trong túi đồ.
  - *OnDrawGizmosSelected()*: Vẽ vòng tròn bán kính tương tác trong Scene view.

### 13. WaveManager.cs
* **Tác dụng**: Điều phối toàn bộ tiến trình các ải trong một tầng Dungeon (Phòng chờ ban đầu, Quái thường, Elite, Boss, Trại nghỉ, Sự kiện, Thương nhân, Rương thưởng, Quái phục kích).
  - *GetActiveFloor()*: Lấy dữ liệu cấu hình tầng hiện tại (FloorData).
  - *Awake() / Start() / OnDestroy()*: Quản lý vòng đời khởi tạo và dọn dẹp các sự kiện tiến trình.
  - *SetupFloorAndEnterEntrance()*: Cài đặt dữ liệu tầng và đưa người chơi vào phòng chờ ban đầu (Entrance Room).
  - *EnterEntranceRoom()*: Kích hoạt hiển thị phòng chờ và mũi tên chỉ dẫn tiến lên.
  - *ProceedFromEntrance()*: Xử lý khi người chơi bấm mũi tên để rời phòng chờ và bắt đầu ải đầu tiên.
  - *TransitionFromEntranceToFirstWave()*: Coroutine di chuyển nhân vật mượt mà vào ải đầu tiên.
  - *StartFirstWave()*: Bắt đầu ải đầu tiên của tầng.
  - *ApplyRoguelikeMerchantEncounter()*: Tính toán ngẫu nhiên chèn ải gặp Thương nhân vào tầng theo cơ chế Roguelike.
  - *Continue()*: Tiếp tục di chuyển sang ải tiếp theo sau khi hoàn thành ải hiện tại.
  - *HandleFloorVictory()*: Xử lý khi đánh bại Boss và hoàn thành toàn bộ tầng (mở màn hình FloorVictoryUI).
  - *ProceedToNextFloor(int nextFloorNum)*: Chuyển sang tầng tiếp theo trong hầm ngục.
  - *NextWave()*: Coroutine hoạt ảnh bước chân chuyển sang ải kế tiếp.
  - *StartWave()*: Xác định loại ải hiện tại (Combat, Camp, Event, Merchant, Reward, Boss) và kích hoạt logic tương ứng.
  - *HandleCampStage()*: Mở giao diện Trại nghỉ ngơi (CampUI).
  - *HandlePostAmbushCamp()*: Mở lại Trại nghỉ ngơi sau khi đánh bại đợt quái phục kích.
  - *HandleEventStage()*: Mở giao diện Sự kiện ngẫu nhiên (DungeonEventUI).
  - *HandleMerchantStage()*: Mở giao diện Thương nhân (MerchantUI).
  - *HandleRewardStage()*: Mở giao diện Rương báu vật (RewardUI).
  - *TriggerAmbush()*: Kích hoạt đợt quái phục kích bất ngờ khi người chơi đang nghỉ ngơi tại Trại.
  - *GetFallbackCombatWave() / FindFirstValidCombatWave()*: Tìm hoặc tạo ải chiến đấu dự phòng an toàn nếu thiếu cấu hình.
  - *SpawnEnemies(WaveData wave, bool isAmbush)*: Sinh các GameObject quái vật tại vị trí chiến đấu chỉ định.
  - *AnimateEnemySlideIn(...)*: Coroutine hoạt ảnh quái vật trượt mượt mà vào sân đấu.

---

## 18.2. Phân hệ Căn cứ (Assets/_Scripts/Base/)

### 14. ChestUI.cs
* **Tác dụng**: Giao diện Rương đồ tại nhà (Home Chest) cho phép chuyển đổi vật phẩm hai chiều giữa Rương và Ba lô (Backpack).
  - *Awake() / Start() / Update()*: Quản lý vòng đời UI, bắt phím tắt ESC/I.
  - *AutoBindReferences()*: Tự động tìm và liên kết các nút bấm, lưới ô đồ nếu chưa gán trong Inspector.
  - *SubscribeEvents() / UnsubscribeEvents()*: Đăng ký và hủy lắng nghe sự kiện thay đổi đồ từ ItemContainer.
  - *Open(ItemContainer chest, ItemContainer backpack)*: Mở giao diện rương đồ và nạp dữ liệu hai túi đồ.
  - *Close()*: Đóng giao diện rương đồ.
  - *GetCanvas()*: Tìm kiếm hoặc lấy Canvas phù hợp.
  - *BuildSlots(...)*: Tạo động danh sách các ô đồ UI.
  - *RefreshAll() / RefreshPanel(...)*: Làm mới hình ảnh và số lượng trong các ô đồ của rương và ba lô.

### 15. HomeChest.cs
* **Tác dụng**: Đối tượng Rương chứa đồ đặt trong nhà (MonoBehaviour tương tác được).
  - *Awake() / Start() / Update()*: Quản lý khởi tạo, tìm kiếm Player và hiển thị phím tương tác khi lại gần.
  - *FindPlayer()*: Tìm Transform của người chơi.
  - *OnMouseDown() / Interact()*: Bấm chuột hoặc nhấn phím tương tác để mở rương.
  - *OpenChest() / CloseChest()*: Mở hoặc đóng UI rương đồ thông qua ChestUI.
  - *StoreItem(ItemData item, int amount)*: Cất vật phẩm từ người chơi vào rương chứa.
  - *RetrieveItem(ItemData item, int amount)*: Lấy vật phẩm từ rương ra ba lô.
  - *OnTriggerEnter2D(...) / OnTriggerExit2D(...)*: Bật/tắt gợi ý phím E khi người chơi đến gần hoặc đi xa.

### 16. KitchenStation.cs
* **Tác dụng**: Điểm đặt Bếp nấu ăn trong căn cứ để người chơi tiếp cận và tương tác nấu nướng.
  - *Awake() / Start() / Update()*: Khởi tạo, hiển thị prompt phím E khi người chơi tới gần.
  - *SetupVisuals() / SetupCollider() / SetupPrompt()*: Tự động tạo hình ảnh sprite, collider vật lý và chữ hướng dẫn nổi nếu chưa có sẵn.
  - *FindPlayer() / FindContainers()*: Tìm kiếm Player và các vùng chứa đồ (ba lô, rương nhà).
  - *OnMouseDown() / Interact()*: Bấm chuột hoặc kích hoạt tương tác để mở giao diện KitchenUI.
  - *OpenKitchen() / CloseKitchen() / OnUIClosed()*: Mở/đóng trạng thái giao diện nấu nướng.
  - *FindSprite(string spriteName)*: Tiện ích tìm sprite hình ảnh nội thất bếp.

### 17. KitchenUI.cs
* **Tác dụng**: Giao diện chế biến món ăn (Nồi nấu tự do thử nghiệm với 4 nguyên liệu + Sổ công thức Recipe Notebook).
  - *Awake() / Start() / Update() / OnDestroy()*: Quản lý vòng đời UI, đóng bằng phím ESC.
  - *Open(...)*: Mở giao diện nấu ăn với dữ liệu ba lô và rương.
  - *Close()*: Đóng giao diện và hoàn trả các nguyên liệu đang đặt trên nồi về lại ba lô.
  - *SubscribeContainerEvents() / UnsubscribeContainerEvents()*: Lắng nghe cập nhật kho đồ.
  - *AddToPot(ItemData item)*: Thêm một nguyên liệu từ túi đồ vào nồi nấu (tối đa 4 nguyên liệu).
  - *RemoveFromPot(int slotIndex)*: Rút nguyên liệu khỏi nồi trả về ba lô.
  - *ReturnAllPotItemsToBackpack()*: Hoàn trả toàn bộ nguyên liệu trong nồi về lại ba lô.
  - *ExecuteCook()*: Kích hoạt nấu: gọi CookingManager.TryCookPot, nhận món ăn mới, mở khóa công thức và phát hiệu ứng.
  - *RefreshUI()*: Làm mới toàn bộ các tab, nồi nấu, danh sách nguyên liệu và sổ tay công thức.
  - *RefreshPotVisuals()*: Làm mới hiển thị các ô trong nồi nấu.
  - *CreatePotSlotUI(...)*: Tạo ô UI hiển thị vật phẩm đang đặt trong nồi.
  - *RefreshIngredientsGrid()*: Hiển thị lưới các nguyên liệu hợp lệ đang có trong ba lô.
  - *CreateIngredientCard(...)*: Tạo thẻ hiển thị nguyên liệu có thể thêm vào nồi.
  - *RefreshNotebookList()*: Hiển thị sổ tay công thức (các công thức đã khám phá hoặc còn ẩn).
  - *CreateNotebookCard(...)*: Tạo thẻ hiển thị thông tin công thức món ăn.
  - *ShowCookResult(...)*: Hiển thị popup kết quả nấu nướng thành công hay thất bại.
  - *UpdateTabButtonsState()*: Đổi màu tab đang chọn (Chế biến / Sổ tay).
  - *ShowToast(string message)*: Hiển thị thông báo nhỏ tạm thời.
  - *GetFoodBuffSummary(FoodData food)*: Tạo chuỗi tóm tắt các hiệu ứng buff (ATK, DEF, Speed, Hồi máu) của món ăn.
  - *BuildUI()*: Dựng toàn bộ cấu trúc giao diện nấu ăn nếu chưa có sẵn trong Scene.

### 18. OfferingSlot.cs
* **Tác dụng**: Ô hiển thị và tiếp nhận một nguyên liệu cống phẩm trên bàn Tượng Thần (kéo thả hoặc click nộp nhanh).
  - *Setup(...)*: Thiết lập dữ liệu yêu cầu cống phẩm cho ô.
  - *UpdateVisuals()*: Cập nhật icon, tên, số lượng tiến độ (đã nộp / tổng cần).
  - *OnDrop(PointerEventData)*: Tiếp nhận sự kiện thả vật phẩm từ ba lô vào ô cống phẩm.
  - *TryContributeFromDrag(...)*: Kiểm tra và nộp vật phẩm từ ô vừa kéo thả vào.
  - *OnPointerClick(PointerEventData)*: Click chuột trực tiếp để nộp nhanh vật phẩm nếu trong ba lô có sẵn.
  - *ApplyContribution(int amount)*: Áp dụng đóng góp số lượng vật phẩm vào tiến trình của ProgressionManager.
  - *GetTotalItemCount(...)*: Đếm số lượng vật phẩm có trong túi đồ.
  - *OnPointerEnter(...) / OnPointerExit(...)*: Hiệu ứng highlight đổi màu khi rê chuột vào/ra ô cống phẩm.

### 19. StatueUI.cs
* **Tác dụng**: Giao diện Tượng Thần toàn diện (Cống phẩm, Ban phước Blessing, Bảng chỉ số vĩnh viễn, Túi đồ người chơi).
  - *Awake() / Start() / Update() / OnDestroy()*: Quản lý vòng đời UI.
  - *SubscribeEvents() / UnsubscribeEvents()*: Đăng ký lắng nghe sự kiện thay đổi tiến trình và ba lô.
  - *OnBlessingStateChanged(...)*: Cập nhật khi người chơi đổi Ban phước (Blessing).
  - *Open(...)*: Mở giao diện Tượng Thần.
  - *Close()*: Đóng giao diện Tượng Thần.
  - *SetFeedback(string message)*: Hiển thị dòng chữ thông báo phản hồi cho người chơi.
  - *CheckOfferingCompletion(Offering offering)*: Kiểm tra xem cống phẩm đã đủ tất cả yêu cầu chưa để trao thưởng chỉ số vĩnh viễn.
  - *QuickOffer(Offering offering)*: Nộp nhanh toàn bộ nguyên liệu có thể cho cống phẩm chỉ định.
  - *RefreshAllOfferingSlots()*: Làm mới toàn bộ các ô cống phẩm.
  - *UpdatePermanentStatsDisplay()*: Cập nhật số điểm ATK, DEF, Speed vĩnh viễn đã tích lũy từ trước đến nay.
  - *UpdateBlessingDisplay()*: Hiển thị Ban phước (Blessing) hiện tại đang kích hoạt cho run hầm ngục.
  - *ShowBlessingSelectionModal() / CloseBlessingSelectionModal()*: Mở/đóng popup danh sách chọn Ban phước.
  - *CreateBlessingCard(...)*: Tạo thẻ thông tin Ban phước (tên, hiệu ứng, trạng thái khóa/mở).
  - *BuildOfferingsList()*: Xây dựng danh sách các cống phẩm từ dữ liệu cấu hình.
  - *CreateOfferingCard(...)*: Tạo thẻ cho từng cống phẩm gồm tên, chỉ số thưởng và các ô nguyên liệu yêu cầu.
  - *CreateOfferingSlot(...)*: Tạo từng ô nguyên liệu cống phẩm.
  - *BuildBackpackSlots(...) / RefreshBackpackSlots()*: Xây dựng và làm mới hiển thị các ô đồ trong ba lô bên cạnh.
  - *CreateFallbackBackpackSlot(...)*: Tạo ô đồ dự phòng nếu thiếu prefab.
  - *EnsureSlotPrefab() / GetCanvas() / EnsureUIBuilt()*: Đảm bảo giao diện UI được khởi tạo đầy đủ.
  - *CreateColumnFrame(...) / CreateScrollableList(...) / CreateScrollableGrid(...) / CreateScrollbar(...)*: Các hàm tiện ích dựng layout UI.
  - *GetOfferingDisplayName(...) / GetStatBadgeColor(...) / GetTotalItemCount(...)*: Tiện ích lấy màu sắc, tên và đếm vật phẩm.

---

## 18.3. Phân hệ Chiến đấu theo lượt (Assets/_Scripts/Combat/)

### 20. CombatJuice.cs
* **Tác dụng**: Quản lý hiệu ứng thị giác và cảm giác đòn đánh (Game Juice): rung màn hình (Screen Shake), dừng hình (Hit Stop), quay chậm kết liễu (Slow Motion) và hạt tia lửa (Hit Sparks).
  - *Awake() / OnDisable() / LateUpdate()*: Quản lý camera và hoàn vị trí gốc sau khi rung.
  - *Shake(float intensity, float duration)*: Kích hoạt hiệu ứng rung màn hình với cường độ và thời gian chỉ định.
  - *HitStop(float durationRealtime, float frozenScale)*: Làm đứng hình trong một khoảnh khắc rất ngắn khi trúng đòn nặng.
  - *HitStopRoutine(...)*: Coroutine điều chỉnh Time.timeScale cho hiệu ứng Hit Stop.
  - *PlayFinisherSlowMo(...)*: Kích hoạt quay chậm khi tung đòn kết liễu quái hoặc Boss.
  - *FinisherSlowMoRoutine(...)*: Coroutine giảm tốc độ game rồi phục hồi về 1.0f.
  - *PlayHitSparks(Vector3 worldPos, Color color, int count)*: Sinh hiệu ứng tia lửa va chạm tại điểm trúng đòn.
  - *GetOrCreateSparkSystem()*: Tạo hoặc tái sử dụng đối tượng Particle System tia lửa.
  - *OnEnemyDamaged(...)*: Tự động chọn hiệu ứng rung màn hình, tia lửa và slow-mo tương ứng với lượng sát thương gây ra lên quái.
  - *OnPlayerDamaged(...)*: Phát hiệu ứng rung màn hình đỏ khi người chơi nhận sát thương.

### 21. CombatManager.cs
* **Tác dụng**: Trọng tài điều phối toàn bộ trận đấu theo lượt: đếm ngược chuẩn bị, lượt đi người chơi, lượt đi quái vật, tấn công, phòng thủ, ăn thức ăn, phản đòn và điều kiện thắng/thua.
  - *Awake() / Start() / OnDestroy()*: Quản lý vòng đời khởi tạo và đăng ký các sự kiện tử trận.
  - *HandlePlayerDeath()*: Xử lý khi người chơi hết máu: kết thúc trận đấu và báo thất bại.
  - *SetEnemies(EnemyStats[] newEnemies)*: Thiết lập danh sách quái trong trận chiến hiện tại.
  - *StartCombat(bool isAmbush)*: Khởi động trận đấu (có tính đến trường hợp bị phục kích Ambush).
  - *PreparationCountdown(bool isAmbush)*: Coroutine đếm ngược thời gian chuẩn bị trước khi vào lượt đầu.
  - *StartPlayerTurn()*: Kích hoạt lượt đi của người chơi, hồi phục trạng thái và bật nút hành động.
  - *StartEnemyTurn(EnemyStats enemy)*: Kích hoạt lượt đi của một quái cụ thể.
  - *EnemyTurnRoutine(EnemyStats enemy)*: Coroutine thực hiện hành vi theo Ý định (Intent) của quái.
  - *EndTurnPlayer()*: Kết thúc lượt của người chơi và chuyển quyền sang quái kế tiếp.
  - *StartPlayerActionCooldown(float delay) / PlayerActionDelayRoutine(...)*: Khóa tạm thời các nút bấm trong lúc hoạt ảnh hành động đang diễn ra.
  - *SetPlayerActing(bool acting)*: Cập nhật trạng thái đang hành động của người chơi.
  - *PlayerAttack()*: Thực hiện hành động tấn công mục tiêu kẻ địch đang chọn (tính ATK, trừ DEF, kích hoạt Juice).
  - *PlayerDefend()*: Thực hiện hành động phòng thủ (tăng phòng ngự và kích hoạt phản đòn nếu có ban phước).
  - *PlayerEat(FoodData food)*: Tiêu thụ món ăn trong ba lô để hồi máu, tăng no và nhận các hiệu ứng buff.
  - *ReflectDamageToCurrentEnemy(int reflectedDamage)*: Phản lại sát thương cho kẻ địch (từ Ban phước Gai nhọn/Thorn).
  - *HandleEnemyDeath(EnemyStats deadEnemy)*: Xử lý khi một quái bị tiêu diệt: cộng thưởng, xóa thanh máu và kiểm tra điều kiện kết thúc.
  - *CheckAllEnemiesDead()*: Kiểm tra toàn bộ quái trong đợt đã chết hết chưa.
  - *DelayedVictoryRoutine()*: Coroutine chờ hoạt ảnh tan biến của quái trước khi hiển thị chiến thắng.
  - *EndCombat(bool victory)*: Kết thúc trận chiến, phân phối chiến lợi phẩm hoặc mở màn hình thất bại.
  - *LogMessage(string message)*: Ghi nhận thông điệp lịch sử chiến đấu lên Combat UI.

### 22. CombatUI.cs
* **Tác dụng**: Quản lý giao diện chiến đấu (Thanh HP/Hunger, nút Tấn công/Thủ/Ăn/Hết lượt, Ý định của quái, bảng chọn đồ ăn, màn hình Thắng/Thua).
  - *Awake() / Start() / OnDestroy() / Update()*: Quản lý vòng đời UI và bắt các phím tắt bàn phím (1-4, Space, Tab).
  - *FindReferences() / BindButtons() / BindButton(...)*: Tìm và gán sự kiện cho các nút bấm hành động.
  - *SubscribeEvents() / UnsubscribeEvents()*: Đăng ký lắng nghe sự kiện từ CombatManager, PlayerStats, EnemyStats.
  - *UpdateReticle()*: Cập nhật tâm ngắm mục tiêu hiển thị trên đầu kẻ địch đang chọn.
  - *HandleKeyboardShortcuts()*: Xử lý phím tắt số 1: Tấn công, 2: Thủ, 3: Ăn, 4/Space: Hết lượt, Tab: Đổi mục tiêu.
  - *CheckMouseClickEnemy()*: Xử lý click chuột trực tiếp vào quái trên màn hình để chọn mục tiêu.
  - *InvalidateHUDCache()*: Xóa cache trạng thái HUD để ép cập nhật lại ngay lập tức.
  - *RefreshAll()*: Làm mới toàn bộ thông số người chơi, nút bấm và biểu ngữ lượt.
  - *UpdatePlayerHUD()*: Cập nhật thanh HP, thanh Đói (Hunger), các buff đang có của Player.
  - *UpdateActionButtons()*: Khóa hoặc mở các nút hành động tùy theo lượt đi và trạng thái hiện tại.
  - *UpdateTurnBanner()*: Cập nhật biểu ngữ hiển thị đang là lượt của ai (Player Turn hay Enemy Turn).
  - *HandleTargetIntentChanged(...) / UpdateTargetInfo(...)*: Cập nhật icon ý định và lượng sát thương dự kiến của quái đang ngắm.
  - *UpdateStageProgress()*: Hiển thị tiến độ ải/đợt hiện tại trong tầng.
  - *LogMessage(string message)*: Hiển thị thông báo chiến đấu vào khung thông tin.
  - *HandleTurnChanged(...) / HandleCombatPreparationCountdown(...) / HandleCombatStarted(...) / HandlePlayerActingChanged(...)*: Các hàm phản hồi sự kiện từ CombatManager.
  - *OnAttackClicked()*: Xử lý khi bấm nút Tấn công.
  - *OnDefendClicked()*: Xử lý khi bấm nút Phòng thủ.
  - *OnEatClicked()*: Xử lý khi bấm nút Ăn: bật/tắt bảng chọn thức ăn.
  - *OnEndTurnClicked()*: Xử lý khi bấm nút Kết thúc lượt.
  - *PopulateFoodPanel()*: Nạp danh sách các món ăn có trong ba lô vào bảng chọn thức ăn.
  - *ShowVictoryScreen(...) / ShowDefeatScreen(...) / HandleRunEnded(...)*: Hiển thị bảng tổng kết Thắng hoặc Thua.
  - *OnReturnToBaseClicked() / OnReturnToBaseAfterDefeatClicked()*: Xử lý nút quay về căn cứ.
  - *ShowRewardScreen(...) / ShowWaveVictoryScreen(...) / OnRewardClaimClicked()*: Hiển thị và nhận chiến lợi phẩm rơi ra sau trận đấu.

### 23. DamagePopupManager.cs
* **Tác dụng**: Quản lý hiển thị chữ số sát thương bay (Damage Popup) nổi trên đầu mục tiêu (sát thương thường, chí mạng, hồi máu, miss).
  - *Awake()*: Thiết lập Singleton quản lý popup.
  - *ShowDamage(...)*: Sinh chữ số sát thương bay lên (màu đỏ cho quái, màu vàng cam cho chí mạng, hỗ trợ cỡ chữ to theo tỉ lệ sát thương).
  - *ShowHeal(...)*: Sinh chữ số màu xanh lá báo lượng máu hồi phục.
  - *ShowText(...)*: Sinh chuỗi chữ thông báo tự do (như Block, Miss, Buff).
  - *SpawnPopup(...)*: Khởi tạo prefab popup tại tọa độ thế giới với nội dung chuỗi chỉ định.

### 24. EnemyCombatVisual.cs
* **Tác dụng**: Quản lý hoạt ảnh và hiệu ứng hiển thị của kẻ địch: lao lên tấn công, thủ, niệm phép, chớp đỏ khi bị đánh và tan biến khi chết.
  - *Awake() / Start()*: Lưu vị trí đứng ban đầu và tìm các thành phần SpriteRenderer/Animator.
  - *SetOriginalPosition(Vector3 newPos)*: Gán tọa độ gốc của quái trên sân đấu.
  - *PlayAttackRoutine(Transform playerTarget, Action onImpact)*: Coroutine quái vật lao nhanh về phía người chơi, kích hoạt sát thương lúc va chạm rồi nhảy lùi về vị trí cũ.
  - *PlayDefendRoutine(Action onApply)*: Coroutine hiệu ứng co người phòng thủ.
  - *PlayBuffRoutine(...) / PlayDebuffRoutine(...)*: Coroutine hiệu ứng niệm phép tăng chỉ số hoặc làm suy yếu đối phương.
  - *PlayHurtFlash() / HurtFlashRoutine()*: Hoạt ảnh chớp trắng/đỏ nhanh khi quái bị trúng đòn.
  - *PlayDeathEffect() / FadeOutRoutine()*: Coroutine mờ dần và biến mất khi quái tử trận.
  - *PlayHitAnimation() / PlayDeathAnimation() / PlayAttackAnimation()*: Các hàm gọi nhanh hoạt ảnh tương ứng.
  - *FaceDirection(Vector3 dir)*: Lật hướng sprite kẻ địch quay mặt về phía mục tiêu.
  - *TryPlayAnimatorState(...)*: Thử kích hoạt trạng thái trong Unity Animator nếu có cấu hình.

### 25. EnemyStats.cs
* **Tác dụng**: Quản lý toàn bộ chỉ số của kẻ địch (HP, ATK, DEF, Speed, buff/debuff, hệ thống Ý định báo trước hành động tiếp theo và rơi đồ khi chết).
  - *GetSpawnYOffset()*: Lấy độ lệch trục Y khi sinh ra quái trên sân đấu.
  - *Awake() / Start()*: Khởi tạo các chỉ số từ dữ liệu EnemyData.
  - *OnMouseDown()*: Click chuột vào thân quái để chọn làm mục tiêu chiến đấu.
  - *GetCurrentATK() / GetCurrentDEF() / GetCurrentSpeed()*: Tính toán chỉ số hiện tại sau khi cộng/trừ các hiệu ứng buff và debuff.
  - *SelectNextIntent()*: Chọn ngẫu nhiên hoặc theo thứ tự hành vi tiếp theo của quái (Tấn công, Thủ, Buff, Debuff) để người chơi nhìn thấy trước.
  - *TakeDamage(int damage)*: Trừ máu quái sau khi tính toán qua chỉ số phòng ngự, kích hoạt âm thanh và kiểm tra tử trận.
  - *Heal(int amount)*: Hồi phục một lượng máu cho quái.
  - *AddBuff(...)*: Thêm hiệu ứng tăng hoặc giảm chỉ số kèm thời hạn tồn tại.
  - *ReduceTurnBuffDuration()*: Giảm thời hạn của các buff/debuff theo từng lượt đi.
  - *ResetDefend()*: Đặt lại trạng thái phòng ngự khi bắt đầu lượt mới.
  - *PerformIntentAction(PlayerStats playerStats)*: Thực hiện hành vi đã báo trước lên người chơi hoặc bản thân quái.
  - *ApplyIntentEffect(...)*: Áp dụng sát thương, giáp hoặc hiệu ứng trạng thái cụ thể theo Intent.
  - *GetLootDrops()*: Lấy danh sách các vật phẩm rơi ra khi quái này bị đánh bại.
  - *Dead()*: Xử lý khi quái hết máu: vô hiệu hóa tương tác và báo lên CombatManager.

### 26. EnemyTargetSelector.cs
* **Tác dụng**: Quản lý việc ngắm và chuyển đổi mục tiêu kẻ địch đang được chọn trong trận đấu.
  - *SelectEnemy(EnemyStats enemy)*: Chọn một quái cụ thể làm mục tiêu hiện tại.
  - *ClearTarget()*: Bỏ chọn mục tiêu hiện tại.
  - *CheckTarget()*: Kiểm tra mục tiêu đang chọn còn sống hay không, nếu đã chết thì tự động bỏ chọn.
  - *AutoSelectTarget(EnemyStats[] enemies)*: Tự động ngắm vào kẻ địch còn sống đầu tiên trong đợt.
  - *CycleTarget(EnemyStats[] enemies, bool forward)*: Chuyển mục tiêu sang kẻ địch tiếp theo hoặc trước đó (bằng phím Tab).

### 27. PlayerCombatVisual.cs
* **Tác dụng**: Quản lý hoạt ảnh và biểu cảm thị giác của nhân vật người chơi trong trận đấu (bước vào sàn đấu, lướt lên chém, chớp đỏ khi trúng đòn, bật khiên thủ, ánh sáng hồi máu và gục ngã).
  - *Awake() / Start() / Update() / OnDestroy()*: Quản lý vòng đời và đăng ký các sự kiện tấn công/phòng thủ của Player.
  - *HandleTurnChanged(...)*: Cập nhật trạng thái đứng yên hoặc sẵn sàng theo lượt.
  - *PlayMoveTransition(...) / MoveTransitionRoutine(...)*: Coroutine hoạt ảnh nhân vật bước từ mép màn hình vào vị trí chiến đấu.
  - *FaceRight()*: Xoay mặt nhân vật sang phải (hướng về phía quái).
  - *SetIdle()*: Đưa nhân vật về trạng thái đứng thở bình thường.
  - *OnAttack(...) / AttackLungeRoutine()*: Coroutine nhân vật lướt tới mục tiêu chém rồi lùi về vị trí cũ.
  - *OnDamaged(...) / HurtFlashRoutine()*: Coroutine chớp đỏ và giật lùi nhẹ khi nhận sát thương.
  - *OnDefended() / DefendPulseRoutine()*: Hoạt ảnh tạo vòng hào quang phòng thủ màu xanh/vàng.
  - *OnHealed(...) / HealPulseRoutine()*: Hoạt ảnh tỏa sáng màu xanh lá khi được hồi máu.
  - *OnDeath() / DeathRoutine()*: Hoạt ảnh nhân vật ngã gục và mờ dần khi cạn máu.

### 28. SpeedManager.cs
* **Tác dụng**: Tính toán và sắp xếp thứ tự đi lượt (Turn Order) giữa Người chơi và các Kẻ địch dựa trên chỉ số Tốc độ (Speed).
  - *Awake()*: Thiết lập khởi tạo thành phần.
  - *SetEnemies(EnemyStats[] newEnemies)*: Cập nhật danh sách kẻ địch đang tham gia trận đấu.
  - *CreateSpeedOrder(bool isAmbush)*: Thu thập chỉ số tốc độ của Player và tất cả quái, sắp xếp giảm dần để tạo danh sách thứ tự lượt (nếu bị Ambush thì quái luôn đi trước).
  - *SortEntries()*: Sắp xếp danh sách lượt theo điểm tốc độ hiện tại.
  - *RandomEqualSpeed()*: Xáo trộn ngẫu nhiên thứ tự giữa các đối tượng có cùng tốc độ bằng nhau.

### 29. TurnManager.cs
* **Tác dụng**: Quản lý vòng lặp các vòng đấu (Rounds) và từng lượt đi đơn lẻ trong một vòng, chuyển giao quyền hành động từ thực thể này sang thực thể tiếp theo.
  - *ResetCombatRounds()*: Đặt lại số đếm vòng đấu về 0 khi bắt đầu trận mới.
  - *Awake()*: Khởi tạo và thiết lập tham chiếu.
  - *StartRound(bool isAmbush)*: Bắt đầu một vòng đấu mới, gọi SpeedManager tạo thứ tự lượt và bắt đầu lượt của đối tượng đầu tiên.
  - *StartCurrentTurn()*: Kích hoạt lượt đi của đối tượng hiện tại ở đầu hàng đợi (nếu là Player thì mở UI hành động, nếu là Quái thì gọi logic hành vi của quái).
  - *NextTurn()*: Chuyển sang lượt đi của đối tượng kế tiếp trong hàng đợi tốc độ; nếu hết hàng đợi thì bắt đầu vòng đấu mới (StartRound).
  - *EndPlayerTurn()*: Kết thúc lượt của người chơi và chuyển quyền cho quái vật kế tiếp.

---

## 18.4. Phân hệ Nấu ăn (Assets/_Scripts/Cooking/)

### 30. CookingManager.cs
* **Tác dụng**: Trọng tài trung tâm của hệ thống ẩm thực: kiểm tra công thức, so khớp nguyên liệu đặt trong nồi (Pot Items), chế biến món ăn và mở khóa công thức vào sổ tay.
  - *Awake() / Start()*: Thiết lập Singleton (CookingManager.Instance) và nạp dữ liệu công thức mặc định.
  - *EnsureDefaultRecipes()*: Tự động quét và nạp toàn bộ RecipeData có trong dự án.
  - *EnsureKitchenStationInBase()*: Tự động đảm bảo đối tượng Bếp nấu hiện diện tại Căn cứ nếu Scene chưa có.
  - *IsValidRecipe(RecipeData r)*: Kiểm tra tính hợp lệ của một công thức nấu ăn.
  - *GetAllAvailableRecipes()*: Lấy danh sách toàn bộ các công thức có trong game.
  - *MatchRecipe(List<ItemSlot> potItems)*: So sánh chính xác danh sách nguyên liệu người chơi bỏ vào nồi với tất cả công thức để tìm món ăn tương ứng.
  - *TryCookPot(...)*: Thử chế biến các nguyên liệu trong nồi: nếu khớp công thức thì trừ nguyên liệu, trả về món ăn thành phẩm và ghi nhận khám phá công thức mới vào ProgressionManager.
  - *CanCook(...)*: Kiểm tra xem người chơi có đủ nguyên liệu trong ba lô hoặc rương để nấu theo một công thức biết trước hay không.
  - *Cook(...)*: Chế biến trực tiếp từ công thức đã biết: tự động trừ nguyên liệu từ các túi đồ và thêm món ăn vào ba lô.
  - *GetItemCount(...) / GetTotalItemCount(...)*: Đếm số lượng nguyên liệu trong một hoặc nhiều túi đồ.

---

## 18.5. Phân hệ Dữ liệu (Assets/_Scripts/Data/)

### 31. CropData.cs
* **Tác dụng**: ScriptableObject định nghĩa hạt giống, nông sản tạo ra, chuỗi sprite sinh trưởng, số ngày lớn của từng giai đoạn và sản lượng thu hoạch. Không chứa hàm logic.

### 32. EnemyData.cs
* **Tác dụng**: ScriptableObject định nghĩa quái vật: tên, prefab, HP tối đa, ATK, DEF, Speed, danh sách hành vi/ý định (Intents), bảng tỉ lệ rơi đồ.
  - *RollDrops()*: Tính toán ngẫu nhiên rơi vật phẩm và hạt giống theo tỉ lệ % cấu hình và trạng thái mở khóa của người chơi.

### 33. FloorData.cs
* **Tác dụng**: ScriptableObject định nghĩa một tầng hầm ngục: tên tầng, số thứ tự, chuỗi các ải (WaveData[]), tên Boss, vật phẩm thưởng khi hoàn thành và các chỉ số thưởng vĩnh viễn (ATK, DEF, Max HP, Speed bonus). Không chứa hàm logic.

### 34. FoodData.cs
* **Tác dụng**: ScriptableObject (kế thừa ItemData) định nghĩa món ăn: lượng hồi đói, lượng hồi máu và danh sách các hiệu ứng buff (Foodbuff: loại buff, giá trị, thời hạn theo lượt/ải/tầng). Không chứa hàm logic.

### 35. ItemData.cs
* **Tác dụng**: ScriptableObject gốc định nghĩa vật phẩm: tên, icon hiển thị, prefab rơi ra đất, loại item (Seed, Produce, Food, Other), khả năng cộng dồn (Stackable) và độ hiếm (Rare). Không chứa hàm logic.

### 36. RecipeData.cs
* **Tác dụng**: ScriptableObject định nghĩa công thức nấu ăn: tên công thức, mảng nguyên liệu yêu cầu (ItemRequirement[]), món ăn thành phẩm và số lượng tạo ra. Không chứa hàm logic.

### 37. RewardData.cs
* **Tác dụng**: Định nghĩa cấu trúc phần thưởng vật phẩm rơi (RewardItemEntry) gồm item, số lượng tối thiểu/tối đa và tỉ lệ rơi. Không chứa hàm logic.

### 38. StatEffectData.cs
* **Tác dụng**: ScriptableObject định nghĩa hiệu ứng tăng/giảm chỉ số (Buff/Debuff): tên, loại chỉ số (Attack, Defense, Speed, Health), khả năng cộng dồn và thuộc tính IsDebuff. Không chứa hàm logic.

### 39. WaveData.cs
* **Tác dụng**: ScriptableObject định nghĩa một ải trong tầng hầm ngục: loại ải (Combat, Camp, Event, Reward, Boss, Merchant), danh sách quái xuất hiện và phần thưởng.
  - *GetEnemiesToSpawn()*: Trả về mảng prefab các quái vật sẽ được sinh ra trong ải.

---

## 18.6. Phân hệ Hầm ngục & Roguelike (Assets/_Scripts/Dungeon/)

### 40. Boss.cs
* **Tác dụng**: Lớp quái vật đặc biệt (kế thừa EnemyStats) quản lý cơ chế chuyển đổi các giai đoạn chiến đấu (Phases) của Boss khi máu giảm xuống các mốc phần trăm nhất định.
  - *Awake()*: Khởi tạo thông số Boss và giai đoạn ban đầu.
  - *TakeDamage(int damage)*: Ghi nhận sát thương và kiểm tra điều kiện chuyển phase.
  - *CheckPhaseTransition()*: So sánh tỉ lệ máu hiện tại với các ngưỡng phase để kích hoạt phase mới.
  - *TriggerPhase(int phase)*: Thay đổi phase của Boss, cường hóa chỉ số và cập nhật bộ ý định hành vi nguy hiểm hơn.
  - *GetCurrentPhase()*: Lấy chỉ số giai đoạn hiện tại của Boss.

### 41. Camp.cs
* **Tác dụng**: Quản lý logic Trại nghỉ chân giữa các tầng hầm ngục: ăn thức ăn, nấu nướng, tập luyện tăng tạm thời chỉ số, ngủ hồi phục, nguy cơ bị quái phục kích (Ambush Risk) và rút lui về nhà an toàn.
  - *OpenCamp(...)*: Kích hoạt mở Trại nghỉ với tham chiếu Player, túi đồ và WaveManager.
  - *CloseCamp()*: Đóng Trại nghỉ.
  - *PerformAction(ActionType action, object parameter)*: Thực thi hành động tại trại (Eat, Cook, Exercise, Rest) và tính toán tăng chỉ số cũng như tăng nguy cơ Ambush.
  - *ReturnHome()*: Bảo toàn chiến lợi phẩm và rút lui an toàn về Căn cứ.
  - *CheckAmbushRoll()*: Đổ xí ngầu ngẫu nhiên kiểm tra xem người chơi có bị quái phục kích hay không dựa trên tỉ lệ Ambush Risk tích lũy.
  - *TriggerAmbush()*: Kích hoạt trận chiến phục kích bất ngờ thông qua WaveManager.
  - *ContinueToNextStage()*: Hoàn thành nghỉ ngơi và tiếp tục tiến lên ải tiếp theo.

### 42. CampUI.cs
* **Tác dụng**: Giao diện Trại nghỉ chân (thanh nguy cơ phục kích, các nút Ăn/Nấu/Tập luyện/Nghỉ ngơi/Về nhà/Tiếp tục, modal chọn thức ăn và modal nấu nướng tại chỗ).
  - *EnsureInstance()*: Singleton lấy hoặc tự động tạo đối tượng CampUI.
  - *Awake() / Start() / OnDestroy()*: Quản lý vòng đời UI.
  - *InitializeCampUI() / FindReferences()*: Khởi tạo và liên kết các thành phần giao diện.
  - *SubscribeEvents() / UnsubscribeEvents()*: Đăng ký lắng nghe sự kiện từ Camp.
  - *EnsureUIConstructed()*: Đảm bảo giao diện modal được dựng sẵn.
  - *BindButtons()*: Gán sự kiện click cho toàn bộ các nút bấm hành động trong trại.
  - *ShowCampScreen()*: Hiển thị toàn bộ giao diện Trại nghỉ ngơi.
  - *ShowPostAmbushCampScreen()*: Hiển thị giao diện trại sau khi vừa đánh bại đợt quái phục kích (với nguy cơ Ambush được đặt về 0%).
  - *HideCampScreen()*: Ẩn giao diện Trại.
  - *HandleCampOpened() / HandleCampClosed() / HandleCampAction(...) / HandleAmbushTriggered()*: Các hàm phản hồi trạng thái từ script Camp.
  - *OnEatButtonClicked()*: Mở modal chọn đồ ăn để tiêu thụ.
  - *OnCookButtonClicked()*: Mở modal nấu ăn dã chiến trong hầm ngục.
  - *OnExerciseButtonClicked()*: Xử lý hành động tập luyện (tăng tạm thời ATK/DEF).
  - *OnRestButtonClicked()*: Xử lý hành động nghỉ ngơi hồi phục máu.
  - *OnContinueButtonClicked()*: Rời trại và tiến lên ải tiếp theo.
  - *OnReturnHomeButtonClicked()*: Rút lui về Căn cứ.
  - *UpdateAmbushRiskUI(float risk)*: Cập nhật thanh đo và phần trăm nguy cơ bị phục kích.
  - *UpdatePlayerStatusUI()*: Cập nhật chỉ số HP, Đói và Buff hiện tại của nhân vật trên UI Trại.
  - *SetButtonsInteractable(bool interactable)*: Bật/tắt khả năng bấm của các nút hành động.
  - *PopulateFoodModal() / CreateFoodCard(...)*: Tạo danh sách các món ăn có thể ăn trong modal.
  - *PopulateCookModal() / CreateRecipeCard(...)*: Tạo danh sách công thức có thể nấu trong modal.
  - *GetFoodBuffSummary(FoodData food)*: Tạo chuỗi tóm tắt các buff của món ăn.

### 43. CampUIBuilder.cs
* **Tác dụng**: Lớp hỗ trợ dựng động bằng code toàn bộ hệ thống giao diện Trại nghỉ (CampUI) chuẩn bố cục aesthetic nếu trong Scene chưa có sẵn Prefab.
  - *Build(CampUI ui)*: Dựng khung canvas, background mờ, bảng trạng thái nhân vật, thanh rủi ro và dàn nút bấm.
  - *CreateCampButton(...)*: Tạo một nút bấm phong cách Trại với màu sắc và nhãn chỉ định.
  - *BuildFoodModal(...)*: Tạo modal chọn món ăn kèm khung cuộn.
  - *BuildCookModal(...)*: Tạo modal nấu ăn dã chiến kèm danh sách công thức.

### 44. DungeonEntrance.cs
* **Tác dụng**: Cổng dịch chuyển vào hầm ngục đặt tại Căn cứ, cho phép người chơi tiếp cận để mở bảng chọn tầng DungeonEntranceUI.
  - *Start() / FindPlayer() / Update()*: Tìm kiếm Player và kiểm tra khoảng cách tương tác.
  - *OnMouseDown() / OpenDungeonUI()*: Bấm chuột hoặc kích hoạt để mở giao diện chọn tầng hầm ngục.
  - *OnTriggerEnter2D(...) / OnTriggerExit2D(...)*: Tự động kích hoạt khi người chơi bước vào vùng cổng.

### 45. DungeonEntranceArrowUI.cs
* **Tác dụng**: Nút mũi tên lớn chỉ dẫn nổi trên màn hình phòng chờ ban đầu của tầng (Entrance Room), cho phép người chơi bấm vào để bắt đầu tiến vào ải đầu tiên.
  - *EnsureInstance()*: Singleton lấy hoặc tự động tạo đối tượng.
  - *Awake() / OnDestroy() / Update()*: Quản lý vòng đời và hiệu ứng nhấp nháy/bồng bềnh của mũi tên.
  - *Show(int floorNumber, int totalStages, Action onProceed)*: Hiển thị mũi tên chỉ dẫn kèm tên tầng và số lượng ải.
  - *Hide()*: Ẩn mũi tên.
  - *OnArrowClicked()*: Xử lý sự kiện click vào mũi tên để tiến bước.
  - *BuildUI()*: Tự động dựng giao diện mũi tên đồ họa nếu chưa có.

### 46. DungeonEventData.cs
* **Tác dụng**: Lớp dữ liệu định nghĩa các sự kiện ngẫu nhiên trong hầm ngục (DungeonEvent), các lựa chọn (DungeonEventChoice) và hệ quả/phần thưởng tương ứng.
  - *CanPlayerSelect(...)*: Kiểm tra xem người chơi có thỏa mãn điều kiện để bấm vào một lựa chọn sự kiện hay không (kiểm tra HP, đói, vật phẩm trong túi).
  - *GetItemCount(...)*: Đếm số lượng vật phẩm trong túi đồ của người chơi.

### 47. DungeonEventManager.cs
* **Tác dụng**: Quản lý ngân hàng dữ liệu các sự kiện ngẫu nhiên trong hầm ngục (Suối thiêng hồi phục, Bàn thờ cổ đại, Tương tác bẫy, v.v.).
  - *Awake() / OnDestroy()*: Quản lý vòng đời Singleton.
  - *InitializeDefaultEvents()*: Nạp sẵn danh sách đầy đủ các sự kiện ngẫu nhiên phong phú cho hầm ngục.
  - *GetRandomEvent()*: Bốc ngẫu nhiên một sự kiện để kích hoạt khi người chơi bước vào ải Sự kiện.

### 48. DungeonEventUI.cs
* **Tác dụng**: Giao diện hiển thị cửa sổ Sự kiện ngẫu nhiên (hiển thị câu chuyện, danh sách các nút lựa chọn, kết quả thành công/thất bại và hiệu ứng nhận được).
  - *EnsureInstance()*: Singleton lấy hoặc tạo thể hiện UI.
  - *Awake() / OnDestroy()*: Quản lý vòng đời UI.
  - *OpenEvent(...)*: Mở giao diện sự kiện với dữ liệu câu chuyện và các lựa chọn.
  - *Close()*: Đóng cửa sổ sự kiện.
  - *DisplayEventContent()*: Hiển thị tên sự kiện, mô tả câu chuyện và thể loại.
  - *GetCategoryColor(string category)*: Lấy mã màu hiển thị theo thể loại sự kiện.
  - *BuildChoiceButtons()*: Tạo động các nút bấm cho từng lựa chọn có trong sự kiện.
  - *CreateChoiceButton(...)*: Dựng giao diện một nút lựa chọn (kiểm tra điều kiện để bật hoặc xám mờ).
  - *OnChoiceSelected(...)*: Xử lý khi người chơi bấm chọn một hướng đi: tính toán thành công/thất bại, trao thưởng hoặc trừ máu.
  - *ResolveOutcome(...)*: Hiển thị bảng tóm tắt kết quả sau khi chọn.
  - *OnContinueClicked()*: Bấm tiếp tục để rời ải sự kiện.
  - *GetCanvas() / EnsureUIBuilt()*: Đảm bảo giao diện UI được tạo đầy đủ.

### 49. DungeonUIAssetHelper.cs
* **Tác dụng**: Thư viện tiện ích đồ họa tập trung cung cấp Sprite, Font chữ, Prefab ô đồ và các hàm style giao diện chuẩn aesthetic cho toàn bộ UI trong Hầm ngục.
  - *GetFrameSprite() / GetSlotSprite() / GetToolbarSprite() / GetChestSprite() / GetBossSprite() / GetTentSprite()*: Các hàm nạp Sprite đồ họa giao diện.
  - *GetSlotPrefab()*: Nạp hoặc tự sinh Prefab ô đồ chuẩn.
  - *GetFontAsset()*: Nạp Font chữ TextMesh Pro của dự án.
  - *StyleSlicedFrame(Image img, Color color)*: Áp dụng định dạng khung bo góc cho Image.
  - *StyleSlotImage(...)*: Áp dụng định dạng hình ảnh cho ô đồ.
  - *ApplyFont(...)*: Áp dụng font và kiểu chữ chuẩn.
  - *StyleButton(...)*: Áp dụng màu sắc, hiệu ứng hover và bấm cho nút Button.
  - *FindSprite(string spriteName)*: Tìm kiếm sprite theo tên.
  - *GetMainScreenCanvas()*: Lấy hoặc tạo Canvas chính trên màn hình.
  - *CreateVerticalScrollbar(...)*: Tạo thanh cuộn dọc (Scrollbar) chuẩn thẩm mỹ.

### 50. Floor.cs
* **Tác dụng**: Lớp dữ liệu chứa thông tin cấu hình cấu trúc tầng (loại ải, điểm trọng số, chỉ số tầng, danh sách ràng buộc StageConstraint). Không chứa hàm logic.

### 51. FloorGenerator.cs
* **Tác dụng**: Thuật toán sinh tầng Roguelike tự động: tạo chuỗi các ải đa dạng cân bằng (Quái, Trại, Sự kiện, Thương nhân, Rương, Boss) dựa trên hệ thống tính điểm trọng số và ràng buộc số lượng.
  - *OnDestroy() / CleanupRuntimeWaves()*: Dọn dẹp các ScriptableObject sinh ra trong runtime để tránh rò rỉ bộ nhớ.
  - *GetScoreForWaveType(WaveData.WaveType type)*: Tính điểm số tương ứng của từng loại ải để cân bằng độ khó.
  - *GenerateFloor()*: Thuật toán chính sinh ra chuỗi mảng các ải (WaveData[]) hoàn chỉnh cho một tầng.
  - *FindWaveOfType(...)*: Tìm ải thuộc loại chỉ định trong danh sách mẫu.
  - *HasEnemies(WaveData wave)*: Kiểm tra một ải có chứa quái vật hay không.
  - *CloneWaveWithType(...)*: Sao chép một ải và đổi sang loại ải mới.
  - *CreateDefaultWave(...)*: Tạo một ải mặc định an toàn khi thiếu dữ liệu mẫu.
  - *GetRandomFromPool(WaveData[] pool)*: Lấy ngẫu nhiên một ải từ tập hợp.
  - *BuildFallbackFloor()*: Tạo cấu trúc tầng dự phòng tiêu chuẩn nếu thuật toán gặp lỗi.

### 52. FloorVictoryUI.cs
* **Tác dụng**: Màn hình Chiến thắng Tầng (khi diệt Boss): trao chỉ số vĩnh viễn, mở khóa Ban phước mới, nhận chiến lợi phẩm quý và lựa chọn Tiếp tục tầng sau hay Về căn cứ.
  - *EnsureInstance()*: Singleton lấy hoặc tạo giao diện chiến thắng.
  - *Awake() / OnDestroy()*: Quản lý vòng đời UI.
  - *Open(...)*: Mở màn hình chiến thắng kèm dữ liệu tầng, ba lô và trạng thái lần đầu vượt tầng (First Clear).
  - *CloseConflictingPanels()*: Đóng các bảng UI khác đang mở để tránh chồng chéo.
  - *Close()*: Đóng màn hình chiến thắng.
  - *LoadHomeChestContainer()*: Nạp rương nhà để sẵn sàng cất đồ.
  - *SetupPermanentStats()*: Hiển thị các chỉ số vĩnh viễn (ATK, DEF, HP, Speed) nhận được từ việc phá đảo tầng.
  - *SetupBlessingUnlockDisplay()*: Hiển thị thông báo mở khóa Ban phước mới (Blessing) nếu có.
  - *SetupFloorLoot()*: Thiết lập danh sách phần thưởng vật phẩm rơi từ Boss và quà clear tầng.
  - *AddItemIfExists(...)*: Thêm vật phẩm vào danh sách thưởng nếu tồn tại trong dự án.
  - *SetupButtonsState()*: Cập nhật trạng thái các nút bấm (cho phép qua tầng kế hoặc quay về).
  - *RefreshRewardItems()*: Làm mới danh sách hiển thị các dòng phần thưởng.
  - *ShowEmptyLootNotice()*: Hiển thị thông báo khi đã nhặt sạch phần thưởng.
  - *UpdateRewardRow(...) / CreateRewardRow()*: Tạo và cập nhật từng dòng vật phẩm thưởng kèm nút Nhặt.
  - *GetItemDescription(ItemData item)*: Lấy mô tả chi tiết của vật phẩm.
  - *OnTakeSingleItem(int index)*: Người chơi nhặt một món đồ cụ thể vào ba lô.
  - *OnLootAllClicked()*: Người chơi bấm nút Nhặt tất cả (Loot All).
  - *SetHint(string msg)*: Hiển thị dòng nhắc nhở cho người chơi (ví dụ: ba lô đã đầy).
  - *OnReturnToBaseClicked()*: Bấm nút quay trở về Căn cứ nhà chính.
  - *OnNextFloorClicked()*: Bấm nút tiến lên tầng hầm ngục tiếp theo.
  - *EnsureUIBuilt() / CreateStyledButton(...)*: Dựng động toàn bộ khung giao diện chiến thắng.

### 53. LootUI.cs
* **Tác dụng**: Giao diện nhặt chiến lợi phẩm rơi ra sau các trận đấu thường hoặc mở rương nhỏ.
  - *EnsureInstance()*: Singleton lấy hoặc tạo đối tượng giao diện.
  - *Awake() / OnDestroy() / GetCanvas() / BindButtonEvents()*: Quản lý vòng đời và liên kết nút bấm.
  - *Open(...)*: Mở giao diện nhặt đồ với danh sách vật phẩm rơi và ba lô người chơi.
  - *Close()*: Đóng giao diện nhặt đồ.
  - *OnContinueClicked()*: Bấm tiếp tục hành trình.
  - *OnLootAllClicked()*: Tự động gom toàn bộ chiến lợi phẩm vào ba lô người chơi.
  - *BuildSlots(...) / CreateFallbackSlot(...)*: Dựng các ô hiển thị vật phẩm rơi.
  - *RefreshAll() / UpdateBackpackTitle() / RefreshPanel(...)*: Làm mới các ô hiển thị và số lượng đồ.
  - *EnsureUIBuilt() / CreateStyledButton(...)*: Dựng cấu trúc giao diện nhặt đồ chuẩn thẩm mỹ.

### 54. MerchantEvent.cs
* **Tác dụng**: Quản lý logic trao đổi buôn bán của Thương nhân trong hầm ngục (Đổi hạt giống quý bằng nông sản, đổi thức ăn/lương khô dã chiến bằng vật phẩm).
  - *Awake() / Start() / OnDestroy()*: Quản lý vòng đời và lắng nghe sự kiện sang ngày mới.
  - *RefreshCalendarStatus()*: Kiểm tra lịch xuất hiện của thương nhân theo chu kỳ ngày.
  - *IsMerchantActive()*: Kiểm tra thương nhân hiện tại có đang mở bán hay không.
  - *ResetMerchant(int days)*: Đặt lại chu kỳ xuất hiện của thương nhân.
  - *EnsureDefaultTrades()*: Nạp danh sách các gói giao dịch mặc định (Seed Trades & Ration Trades).
  - *HandleNewDay()*: Cập nhật trạng thái thương nhân khi qua ngày mới.
  - *CanTradeSeed(...) / TradeSeed(...)*: Kiểm tra điều kiện và thực hiện đổi lấy hạt giống cây trồng.
  - *CanTradeRation(...) / TradeRation(...)*: Kiểm tra điều kiện và thực hiện đổi lấy lương khô/món ăn dã chiến.
  - *HasContainerSpaceFor(...)*: Kiểm tra túi đồ của người chơi còn đủ chỗ trống để nhận vật phẩm mới hay không.
  - *GetItemCount(...)*: Đếm số lượng vật phẩm trong túi đồ.

### 55. MerchantUI.cs
* **Tác dụng**: Giao diện buôn bán với Thương nhân trong Hầm ngục (danh mục hàng hóa, giá đổi, ba lô của người chơi và nút Giao dịch).
  - *EnsureInstance()*: Singleton lấy hoặc tạo giao diện thương nhân.
  - *Awake() / Start() / Update() / OnDestroy()*: Quản lý vòng đời UI.
  - *SubscribeEvents() / UnsubscribeEvents() / HandleInventoryChanged()*: Lắng nghe thay đổi kho đồ để cập nhật UI ngay lập tức.
  - *UpdateBackpackHeader()*: Cập nhật tiêu đề số ô ba lô còn trống.
  - *Open(...)*: Mở giao diện thương nhân kèm ba lô và callback tiếp tục.
  - *Close()*: Đóng giao diện thương nhân.
  - *SetFeedback(string message)*: Hiển thị dòng chữ thông báo kết quả giao dịch.
  - *BuildWaresList()*: Xây dựng danh sách toàn bộ các món hàng mà thương nhân đang bán.
  - *CreateDepartedRestCard()*: Hiển thị thông báo nếu thương nhân đã rời đi.
  - *CreateSectionHeader(...)*: Tạo tiêu đề phân loại hàng hóa (Hạt giống / Lương khô).
  - *CreateSeedTradeCard(...)*: Tạo thẻ giao dịch đổi hạt giống.
  - *CreateRationTradeCard(...)*: Tạo thẻ giao dịch đổi đồ ăn.
  - *BuildBackpackSlots(...) / RefreshBackpackSlots()*: Xây dựng và làm mới hiển thị các ô đồ trong ba lô bên cạnh.
  - *EnsureSlotPrefab() / GetCanvas() / EnsureUIBuilt()*: Đảm bảo giao diện UI hoàn chỉnh.
  - *GetItemCount(...)*: Đếm số lượng item trong ba lô.

### 56. RewardChest.cs
* **Tác dụng**: Logic sinh và nhặt phần thưởng từ Rương báu vật ở ải Reward (tự động tạo vật phẩm giá trị cao theo độ sâu của tầng hầm ngục).
  - *ResetStatics() / GetAllProjectItems()*: Nạp danh sách toàn bộ vật phẩm có trong dự án.
  - *GenerateChestLoot(WaveData waveData, int floorNumber)*: Thuật toán sinh danh sách chiến lợi phẩm ngẫu nhiên giá trị cao cho rương báu theo tầng.
  - *ClaimSingleItem(int index, ItemContainer backpack, out string message)*: Người chơi lấy một món đồ từ rương bỏ vào ba lô.
  - *ClaimAllLoot(ItemContainer backpack, out bool isFull)*: Người chơi lấy tất cả các món đồ có trong rương.

### 57. RewardUI.cs
* **Tác dụng**: Giao diện mở Rương báu vật ở ải Reward (hình ảnh rương mở, danh sách vật phẩm quý, túi đồ người chơi và các nút nhặt đồ).
  - *EnsureInstance()*: Singleton lấy hoặc tạo thể hiện UI.
  - *Awake() / Update() / OnDestroy()*: Quản lý vòng đời UI.
  - *Open(...)*: Mở giao diện rương báu vật với danh sách quà và ba lô người chơi.
  - *Close()*: Đóng giao diện rương báu.
  - *RefreshAll() / UpdateBackpackHeader()*: Làm mới toàn bộ thông tin rương và số ô ba lô.
  - *RefreshChestItems()*: Làm mới danh sách hiển thị các món đồ trong rương.
  - *ShowEmptyChestNotice()*: Hiển thị thông báo khi rương đã được vét sạch.
  - *UpdateRewardRow(...) / CreateRewardRow(...)*: Tạo từng dòng hiển thị vật phẩm thưởng kèm nút nhặt riêng.
  - *GetItemTag(...) / GetItemDescription(...)*: Lấy nhãn phân loại (Hạt giống hiếm, Thức ăn quý) và mô tả item.
  - *UpdateControlsState()*: Cập nhật trạng thái các nút bấm điều khiển.
  - *OnTakeItemClicked(ItemSlot targetSlot)*: Bấm nhặt một món đồ cụ thể.
  - *OnLootAllClicked()*: Bấm nhặt sạch tất cả đồ trong rương.
  - *OnContinueClicked()*: Bấm tiếp tục hành trình sang ải kế tiếp.
  - *SetHint(string message)*: Hiển thị thông báo nhắc nhở tạm thời.
  - *BuildBackpackSlots(...) / RefreshBackpackSlots()*: Xây dựng và làm mới hiển thị ba lô người chơi.
  - *EnsureSlotPrefab() / GetCanvas() / EnsureUIBuilt()*: Đảm bảo giao diện UI được dựng đầy đủ.

### 58. StageConstraint.cs
* **Tác dụng**: Thành phần dữ liệu ràng buộc số lượng tối thiểu (min) và tối đa (max) của từng loại ải (StageType) khi sinh tầng ngẫu nhiên. Không chứa hàm logic.

---

## 18.7. Phân hệ Túi đồ (Assets/_Scripts/Inventory/)

### 59. CurrentItemPanel.cs
* **Tác dụng**: Thanh hiển thị vật phẩm đang được chọn trên tay nhân vật (Active/Selected Item HUD).
  - *Awake() / Start() / OnEnable() / OnDisable()*: Quản lý vòng đời và đăng ký sự kiện thay đổi lựa chọn vật phẩm.
  - *SetCurrentItem()*: Cập nhật hiển thị icon và số lượng của vật phẩm đang cầm trên tay.

### 60. InventoryButton.cs
* **Tác dụng**: Từng ô chứa đồ đơn lẻ trên giao diện: hiển thị icon, số lượng, xử lý chọn ô và toàn bộ cơ chế Kéo - Thả (Drag & Drop) vật phẩm giữa các ô hoặc giữa các túi đồ khác nhau.
  - *Awake() / FixTextLayout()*: Khởi tạo và căn chỉnh chữ số lượng hiển thị.
  - *SetSlotData(ItemContainer container, int index)*: Gán dữ liệu kho chứa và chỉ số ô cho nút.
  - *SetCanvas(Canvas targetCanvas)*: Gán Canvas cha phục vụ việc tính toán tọa độ kéo thả.
  - *OnDisable()*: Dọn dẹp trạng thái kéo khi ô bị ẩn.
  - *BindComponents(Image icon, TextMeshProUGUI amount)*: Liên kết trực tiếp các thành phần hình ảnh và văn bản.
  - *SetItem(ItemData itemData, int amount)*: Gán và hiển thị vật phẩm cùng số lượng vào ô.
  - *ClearItem()*: Xóa sạch dữ liệu hiển thị (đưa ô về trạng thái trống).
  - *SelectItem()*: Đánh dấu ô này là ô đang được người chơi chọn.
  - *OnBeginDrag(PointerEventData)*: Bắt đầu kéo vật phẩm: tạo bản sao icon kéo theo con trỏ chuột.
  - *OnDrag(PointerEventData)*: Cập nhật vị trí icon theo tọa độ chuột di chuyển.
  - *OnEndDrag(PointerEventData)*: Kết thúc kéo vật phẩm: hủy icon kéo theo chuột.
  - *OnDrop(PointerEventData)*: Tiếp nhận vật phẩm từ một ô khác thả vào đây để tráo đổi (Swap) hoặc dồn số lượng.

### 61. InventoryController.cs
* **Tác dụng**: Bộ điều khiển bật/tắt cửa sổ Túi đồ chính bằng phím tắt (như phím B hoặc I).
  - *Awake() / Start()*: Khởi tạo bộ điều khiển.
  - *Update()*: Lắng nghe phím bấm để chuyển đổi trạng thái bật/tắt túi đồ.
  - *SwapInventoryPanel()*: Đảo ngược trạng thái đóng/mở của túi đồ.
  - *CloseInventoryPanel()*: Đóng cửa sổ túi đồ.
  - *OpenInventoryPanel()*: Mở cửa sổ túi đồ.

### 62. InventoryPanel.cs
* **Tác dụng**: Bảng hiển thị danh sách các ô đồ trong túi người chơi (Backpack Grid).
  - *Start() / OnEnable() / OnDisable()*: Quản lý vòng đời và đăng ký lắng nghe sự kiện thay đổi dữ liệu từ kho đồ.
  - *RefreshInventory()*: Làm mới lại toàn bộ các ô đồ trên giao diện.
  - *SetInventoryButton()*: Gán dữ liệu tương ứng từ ItemContainer vào từng nút InventoryButton.

### 63. ItemContainer.cs
* **Tác dụng**: Lớp quản lý kho dữ liệu túi đồ (Data Container): thêm đồ, trừ đồ, kiểm tra sức chứa, cộng dồn vật phẩm (Stacking), hoán đổi ô (Swap) và phát sự kiện OnInventoryChanged.
  - *OnEnable()*: Khởi tạo mảng các ô đồ nếu chưa có.
  - *AddItem(ItemData itemData, int amount)*: Thêm một lượng vật phẩm vào túi: ưu tiên cộng dồn vào ô đã có sẵn cùng loại, sau đó mới tìm ô trống tiếp theo.
  - *IsItemMatch(ItemData a, ItemData b)*: Kiểm tra hai vật phẩm có cùng loại hay không.
  - *RemoveItem(ItemData itemData, int amount)*: Trừ bớt một lượng vật phẩm khỏi túi đồ.
  - *GetItemCount(ItemData itemData)*: Đếm tổng số lượng của một vật phẩm đang có trong toàn bộ túi đồ.
  - *HasItem(...)*: Kiểm tra trong túi có chứa vật phẩm này hay không (có hỗ trợ kiểm tra đủ số lượng yêu cầu).
  - *GetEmptySlot()*: Tìm chỉ số ô trống đầu tiên trong túi (-1 nếu đầy).
  - *CanAddItem(ItemData itemData)*: Kiểm tra túi đồ còn đủ chỗ để nhận thêm vật phẩm này hay không.
  - *SwapSlots(int indexA, int indexB)*: Hoán đổi vị trí vật phẩm giữa ô A và ô B (hỗ trợ cả tính năng gộp số lượng nếu cùng loại).
  - *NotifyChange()*: Phát sự kiện thông báo dữ liệu túi đồ đã thay đổi để các UI liên quan tự cập nhật.

---

## 18.8. Phân hệ Vật phẩm rơi (Assets/_Scripts/Item/)

### 64. ItemDrop.cs
* **Tác dụng**: Quản lý vật phẩm rơi trên mặt đất thế giới 2D: hiệu ứng nảy khi rơi ra, hiệu ứng bay hút về phía người chơi (Magnet Pull) và nhặt vào ba lô.
  - *Awake()*: Khởi tạo thành phần Rigidbody2D và SpriteRenderer.
  - *PlayDropEffect() / StopDrop()*: Kích hoạt lực văng ngẫu nhiên khi vật phẩm mới rớt ra mặt đất và dừng lại sau thoáng chốc.
  - *StartMove(GameObject targetPlayer)*: Kích hoạt trạng thái bay hút về phía người chơi khi người chơi lại gần.
  - *Update()*: Di chuyển vật phẩm lao nhanh về phía người chơi.
  - *Collect()*: Nhặt vật phẩm vào ba lô người chơi (AddItem) và hủy đối tượng trên mặt đất.

### 65. ItemDropSpawner.cs
* **Tác dụng**: Tiện ích sinh các đối tượng vật phẩm rơi ra đất tại một vị trí tọa độ cụ thể.
  - *Start() / EnsureReferences()*: Khởi tạo và thiết lập các prefab rơi mặc định.
  - *SpawnItem(ItemData itemData, int amount)*: Sinh prefab ItemDrop tại vị trí hiện tại với dữ liệu vật phẩm và số lượng chỉ định.

### 66. ItemPickupRange.cs
* **Tác dụng**: Vùng trigger bán kính nhặt đồ quanh người chơi (Pickup Radius).
  - *OnTriggerEnter2D(Collider2D other)*: Phát hiện khi có ItemDrop lọt vào bán kính nhặt để lập tức kích hoạt hút vật phẩm bay về phía người chơi (itemDrop.StartMove).

---

## 18.9. Phân hệ Người chơi (Assets/_Scripts/Player/)

### 67. BedInteraction.cs
* **Tác dụng**: Chiếc giường ngủ trong nhà (thực thi interface IInteractable).
  - *Start()*: Khởi tạo tham chiếu.
  - *Interact()*: Xử lý khi người chơi bấm tương tác với giường: gọi DayManager.Instance.Sleep() để sang ngày mới và hồi máu.

### 68. PlayerInteraction.cs
* **Tác dụng**: Quản lý hành vi tương tác của người chơi trong thế giới nông trại (phát hiện ô đất trồng trọt FarmPlot gần nhất, giường ngủ, bàn bếp, bấm phím E hoặc click chuột).
  - *OnTriggerEnter2D(...) / OnTriggerExit2D(...)*: Phát hiện và ghi nhận các đối tượng tương tác xung quanh người chơi.
  - *Awake() / Update()*: Lắng nghe phím tương tác E và click chuột trái/phải.
  - *IsAnyUIOpen()*: Kiểm tra xem có bảng giao diện UI nào đang mở hay không để tránh tương tác xuyên thấu.
  - *InteractFarmPlot()*: Xử lý tương tác với ô đất trồng: gieo hạt giống đang cầm trên tay hoặc thu hoạch nông sản chín.
  - *InteractBed()*: Tương tác với giường ngủ khi đứng gần.
  - *GetNearestBed()*: Tìm kiếm đối tượng giường ngủ gần nhất.

### 69. PlayerMovement.cs
* **Tác dụng**: Quản lý di chuyển 4 hướng của nhân vật bằng bàn phím (WASD / Mũi tên), tốc độ di chuyển có tính đến buff/chỉ số, hoạt ảnh chạy và tự động khóa di chuyển khi đang mở UI hoặc đang trong trận đấu.
  - *Awake() / OnDestroy() / OnSceneLoaded(...) / UpdateSceneCache()*: Quản lý vòng đời và cập nhật trạng thái scene khi chuyển cảnh.
  - *IsInCombat()*: Kiểm tra xem người chơi có đang trong trạng thái chiến đấu hay không.
  - *IsAnyUIOpen()*: Kiểm tra xem có giao diện UI nào đang mở hay không.
  - *Update()*: Đọc tín hiệu phím bấm di chuyển từ người chơi (Horizontal, Vertical) và cập nhật hoạt ảnh Animator.
  - *GetCurrentMoveSpeed()*: Tính toán tốc độ di chuyển thực tế (kết hợp tốc độ cơ bản và chỉ số Speed của PlayerStats).
  - *FixedUpdate()*: Di chuyển nhân vật thông qua Rigidbody2D.velocity.

### 70. PlayerStats.cs
* **Tác dụng**: Quản lý toàn bộ chỉ số thực tế của Người chơi (HP hiện tại/tối đa, Đói hiện tại/tối đa, ATK, DEF, Speed, các trạng thái Buff/Debuff từ đồ ăn, phòng thủ và trừ thời hạn hiệu ứng theo lượt/ải/tầng).
  - *Awake() / OnEnable() / OnDisable() / Start()*: Quản lý vòng đời và đăng ký lắng nghe sự kiện từ ProgressionManager.
  - *RefreshStatsFromProgression()*: Đồng bộ các chỉ số thưởng vĩnh viễn (Permanent Stats) từ ProgressionManager vào chỉ số nhân vật.
  - *Eat(ItemData item)*: Tiêu thụ món ăn: hồi máu, hồi đói và nạp các hiệu ứng buff đi kèm.
  - *Defend()*: Bật trạng thái phòng ngự (tăng tạm thời phòng thủ cho lượt tiếp theo).
  - *GetCurrentATK()*: Tính toán chỉ số Tấn công hiện tại (Cơ bản + Vĩnh viễn + Buff món ăn + Ban phước Blessing).
  - *GetCurrentDEF()*: Tính toán chỉ số Phòng thủ hiện tại (Cơ bản + Vĩnh viễn + Khiên thủ + Buff món ăn + Ban phước Blessing).
  - *GetCurrentSpeed()*: Tính toán chỉ số Tốc độ hiện tại (Cơ bản + Vĩnh viễn + Buff món ăn + Ban phước Blessing).
  - *TakeDamage(int damage)*: Trừ máu người chơi sau khi giảm trừ qua giáp DEF, kích hoạt phản đòn (nếu có) và kiểm tra tử trận.
  - *OnStageTransition()*: Xử lý khi vượt qua một ải mới trong hầm ngục.
  - *OnCombatComplete()*: Giảm thời hạn của các buff theo loại trận đấu (Combat).
  - *AddHunger(int amount) / ReduceHunger(int amount)*: Tăng hoặc giảm điểm no của người chơi.
  - *Heal(int amount)*: Hồi máu cho người chơi (không vượt quá Max HP).
  - *ReduceTurnBuffDuration()*: Giảm thời hạn các buff tính theo từng lượt đi (Turn).
  - *ReduceCombatBuffDuration()*: Giảm thời hạn các buff tính theo từng trận đấu (Combat).
  - *ReduceFloorBuffDuration()*: Giảm thời hạn các buff tính theo từng tầng hầm ngục (Floor).

---

## 18.10. Phân hệ Tiến trình chơi vĩnh viễn (Assets/_Scripts/Progression/)

### 71. ProgressionManager.cs
* **Tác dụng**: Trọng tâm của hệ thống Meta Progression (Lưu trữ và đồng bộ vĩnh viễn qua PlayerPrefs): chỉ số vĩnh viễn tích lũy từ Tượng thần, mở khóa tầng hầm ngục, kỷ lục hạ Boss, mở khóa công thức nấu ăn, mở khóa hạt giống rơi, hệ thống Ban phước thần linh (Blessings) và bảo toàn tài nguyên khi chết.
  - *ResetStatics() / OnApplicationQuit() / OnDestroy()*: Dọn dẹp tài nguyên khi tắt game.
  - *Awake()*: Khởi tạo Singleton (ProgressionManager.Instance), nạp toàn bộ dữ liệu đã lưu từ PlayerPrefs.
  - *InitializeDefaultUnlocks()*: Khởi tạo các mở khóa mặc định ban đầu cho người chơi mới.
  - *GetSeedMinFloor(ItemData seed)*: Lấy tầng tối thiểu cần đạt được để hạt giống này bắt đầu có tỉ lệ rơi từ quái vật.
  - *IsRecipeDiscovered(RecipeData recipe) / DiscoverRecipe(RecipeData recipe)*: Kiểm tra và ghi nhận một công thức nấu ăn đã được khám phá vào sổ tay.
  - *SaveDiscoveredRecipes() / LoadDiscoveredRecipes()*: Lưu và nạp danh sách công thức đã mở khóa từ bộ nhớ.
  - *IsItemUnlocked(ItemData item) / UnlockItem(ItemData item)*: Kiểm tra và mở khóa một vật phẩm vào bảng loot rơi của thế giới.
  - *StartRun(int floor)*: Bắt đầu một lượt đi hầm ngục mới ở tầng chỉ định.
  - *AddPermanentStat(Offering.RewardStat stat, int amount)*: Cộng vĩnh viễn chỉ số ATK, DEF hoặc Speed khi cống nạp Tượng Thần.
  - *SavePermanentStats() / LoadPermanentStats()*: Lưu và nạp các chỉ số vĩnh viễn từ PlayerPrefs.
  - *UnlockRecipe(RecipeData recipe) / UnlockSeed(ItemData seedItem)*: Mở khóa công thức hoặc hạt giống mới.
  - *IsOfferingCompleted(string offeringID) / CompleteOffering(string offeringID)*: Kiểm tra và đánh dấu một cống phẩm tượng thần đã hoàn tất.
  - *GetOfferingProgress(...) / SetOfferingProgress(...) / AddOfferingProgress(...)*: Quản lý tiến độ số lượng nguyên liệu đã nộp cho từng cống phẩm.
  - *IsStarterProtectionActive()*: Kiểm tra hiệu lực bảo hộ tân thủ (giảm tổn thất khi chết ở những tầng đầu).
  - *HasBlessing(BlessingType blessing) / SetBlessing(...) / ClearRunBlessing()*: Kiểm tra, kích hoạt và xóa Ban phước thần linh (Blessing) cho lượt đi hiện tại.
  - *IsBlessingUnlocked(...) / UnlockBlessing(...) / CheckAllBlessingUnlocks()*: Quản lý điều kiện và mở khóa các loại Ban phước mới khi đạt thành tựu.
  - *SaveUnlockedBlessings() / LoadUnlockedBlessings()*: Lưu và nạp danh sách Ban phước đã mở khóa.
  - *HandlePlayerDeath(...) / HandlePlayerDeathWithoutReload(...)*: Xử lý tổn thất khi người chơi tử trận trong hầm ngục (trừ đồ ba lô theo quy tắc Roguelike, giữ lại các mở khóa vĩnh viễn).
  - *CompleteRun()*: Xử lý khi người chơi hoàn thành chuyến đi hầm ngục và trở về an toàn.
  - *IsFloorUnlocked(int floor) / UnlockFloor(int floor)*: Kiểm tra và mở khóa tầng hầm ngục tiếp theo.
  - *IsBossDefeated(int floor) / RecordBossDefeated(int floor)*: Kiểm tra và ghi nhận kỷ lục đánh bại Boss của tầng.
  - *HasClaimedFirstClearStats(int floor) / MarkFirstClearStatsClaimed(int floor)*: Kiểm tra và đánh dấu đã nhận quà chỉ số vĩnh viễn lần đầu vượt tầng.
  - *ApplyPermanentStatBonus(int atk, int def, int hp, int spd)*: Áp dụng trực tiếp gói chỉ số thưởng vĩnh viễn vào người chơi.
  - *SaveFloorProgression() / LoadFloorProgression()*: Lưu và nạp tiến độ vượt tầng từ PlayerPrefs.
  - *ClearRunLoot(ItemContainer inventory)*: Xóa các vật phẩm nhặt được trong run khi chết mà không có bảo hộ.
  - *BlessingInfo(...)*: Constructor khởi tạo thông tin chi tiết của một Ban phước (tên, icon, mô tả, màu sắc, điều kiện mở khóa).
  - *GetBlessingInfo(...) / GetAllBlessings() / GetRandomChoices(int count)*: Tiện ích lấy thông tin chi tiết và bốc ngẫu nhiên các lựa chọn Ban phước cho người chơi chọn.
