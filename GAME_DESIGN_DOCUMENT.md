# TÀI LIỆU THIẾT KẾ TRÒ CHƠI CHI TIẾT (GAME DESIGN DOCUMENT)
**Tên Dự Án:** Harvest & Hollow (Dungeon Farm)
**Phiên Bản Tài Liệu:** 3.0 (Production Master Specification)
**Engine & Nền Tảng:** Unity 2D (URP) — PC (Windows)

---

## 1. TỔNG QUAN TRÒ CHƠI (GAME OVERVIEW)

### 1.1. Ý Tưởng Cốt Lõi (High Concept)
**Harvest & Hollow** là một tựa game **2D Top-Down Farming Roguelite kết hợp Turn-Based Tactical RPG**. Trò chơi hợp nhất hai thể loại tưởng chừng đối lập: sự bình yên, tính toán dài hạn của mô phỏng nông trại (Farming Simulation) và sự khắc nghiệt, quản trị rủi ro theo từng bước đi của khám phá hầm ngục theo lượt (Turn-Based Dungeon Crawler).

Trong thế giới của **Harvest & Hollow**, không tồn tại hệ thống điểm kinh nghiệm (EXP) hay cấp độ nhân vật (Level) truyền thống. Thay vào đó, **Nông sản chính là Sức mạnh (Food is Power)**:
* Mọi hạt giống đều phải đoạt lấy từ quái vật và các sự kiện trong hầm ngục.
* Mọi vụ mùa thu hoạch tại Căn cứ đều là nguồn nguyên liệu duy nhất để chế biến thức ăn tiếp tế cho chuyến thám hiểm tiếp theo, hoặc dâng hiến lên Tượng Nữ Thần để đổi lấy chỉ số vĩnh viễn và Phước Lành Thần Thánh.

### 1.2. Thông Tin Chung (General Information)
* **Thể loại (Genre):** Roguelite / Turn-Based RPG / Farming & Resource Management.
* **Góc nhìn (Perspective):** 2D Top-Down (tự do di chuyển tại Căn cứ kết hợp giao diện chiến đấu theo lượt trực diện trong Hầm ngục).
* **Đối tượng người chơi (Target Audience):**
  * Người chơi yêu thích dòng game *Stardew Valley*, *Moonlighter*, *Cult of the Lamb* muốn trải nghiệm vòng lặp xây dựng căn cứ gắn liền với chiến đấu.
  * Người chơi đam mê dòng game *Slay the Spire*, *Darkest Dungeon* yêu thích cơ chế quản lý Điểm Hành Động (Action Points - AP), tính toán sát thương và quyết định rút lui chiến thuật.
* **Điểm Độc Đáo Nhất (Unique Selling Points - USP):**
  1. **Kinh Tế Thuần Nông Sản (Zero-Gold Economy):** Không dùng tiền vàng làm trung gian. Hạt giống, nông sản thô và món ăn chế biến đóng vai trò vừa là máu, vừa là năng lượng hành động, vừa là tiền tệ giao dịch với Thương nhân, vừa là lễ vật nâng cấp nhân vật.
  2. **Đồng Hồ Sinh Học Chung (Unified Day Cycle):** Mỗi một bước tiến qua phòng hầm ngục, mỗi lần ngủ qua đêm tại nông trại, hay mỗi lần gục ngã đều thúc đẩy Ngày trong game tăng lên. Khi người chơi chiến đấu dưới lòng đất, cây trồng trên mặt đất vẫn âm thầm lớn lên theo từng phòng vượt qua.
  3. **Hệ Thống Độ No Chiến Thuật (Tactical Fullness System):** Thanh Độ No (Fullness/Hunger) không chỉ giữ cho nhân vật khỏi suy kiệt mà còn là đòn bẩy kích hoạt các trường phái chiến đấu đối lập: ăn thật no để hóa thân thành pháo đài phản đòn (*Khiên No Nê*), hoặc cố tình nhịn đói xuống dưới ngưỡng nguy hiểm để bộc phát sát thương chí mạng (*Cuồng Nộ Khi Đói*).

---

## 2. TRỤ CỘT THIẾT KẾ & VÒNG LẶP GAMEPLAY (CORE PILLARS & GAMEPLAY LOOP)

### 2.1. Ba Trụ Cột Thiết Kế (Three Design Pillars)
1. **Nông Trại Nuôi Dưỡng Hầm Ngục — Hầm Ngục Nuôi Dưỡng Nông Trại:** Không thể đi sâu vào hầm ngục nếu thiếu thức ăn chế biến từ nông trại; ngược lại, không thể mở rộng nông trại hay sở hữu giống cây quý nếu không đánh bại quái vật ở các tầng sâu hơn.
2. **Quản Trị Rủi Ro & Lòng Tham (Risk vs. Reward):** Người chơi luôn đứng trước lựa chọn căng thẳng tại mỗi Lửa Trại (Camp) hoặc trước cửa phòng Boss: *Rút lui an toàn về nhà để bảo toàn 100% hạt giống quý vừa nhặt được, hay liều lĩnh đi tiếp khi máu và thức ăn đã cạn với nguy cơ mất sạch hành trang?*
3. **Chiến Đấu Theo Lượt Linh Hoạt (AP & Turn-Order Manipulation):** Mọi quyết định trong lượt (Tấn công, Phòng thủ, Ăn uống) đều tiêu tốn Điểm Hành Động (AP). Kết hợp hệ thống tốc độ (Speed) và Phước Lành, người chơi có thể hoàn trả AP, ăn miễn phí không tốn lượt, hoặc biến chỉ số phòng thủ và tốc độ chênh lệch thành sát thương thuần.

### 2.2. Vòng Lặp Gameplay Chi Tiết (Core Gameplay Loop)
Toàn bộ trải nghiệm của người chơi vận hành qua một chu trình khép kín gồm 5 giai đoạn liên tiếp:

* **Giai đoạn 1 — Chuẩn bị & Canh tác tại Căn Cứ (Base Preparation):**
  Người chơi thức dậy tại căn cứ, kiểm tra các ô đất đã chín sau chuyến đi trước để thu hoạch nông sản. Tiếp theo, người chơi gieo các hạt giống mới thu thập được xuống đất trống, cất bớt vật phẩm quý vào Rương Đồ (Home Chest), mang nông sản vào Trạm Nấu Ăn (Kitchen Station) để chế biến thành các món ăn mang hiệu ứng cường hóa (Buff), và đem nông sản dư thừa đến Tượng Nữ Thần (Goddess Statue) để hiến tế cộng chỉ số vĩnh viễn.
* **Giai đoạn 2 — Chọn Tầng & Tiến Vào Hầm Ngục (Dungeon Entry):**
  Người chơi lựa chọn trang bị 1 Phước Lành Thần Thánh (Divine Blessing) phù hợp với chiến thuật chuyến đi, sắp xếp thức ăn vào Ba lô (Backpack), và bước vào Cổng Hầm Ngục (Dungeon Entrance) ở tầng đã mở khóa (Tầng 1, Tầng 2 hoặc Tầng 3).
* **Giai đoạn 3 — Chinh Phục Các Phòng (Wave Progression):**
  Người chơi lần lượt vượt qua chuỗi các phòng (Waves) của tầng hầm ngục. Mỗi bước tiến qua 1 phòng sẽ tiêu hao **12 điểm Độ No** và làm thời gian trôi qua **1 Ngày** (giúp cây ở nhà lớn thêm 1 ngày). Người chơi phải đối mặt với Phòng Chiến Đấu (Combat), Phòng Sự Kiện Ngẫu Nhiên (Event), Thương Nhân (Merchant) và Khu Cắm Trại (Camp).
* **Giai đoạn 4 — Quyết Định Chiến Thuật: Hạ Boss hoặc Rút Lui (Extraction or Boss Fight):**
  Tại các Khu Cắm Trại dọc đường, người chơi có thể chọn **Rút Lui An Toàn (Return Home)** để mang toàn bộ chiến lợi phẩm về nhà an toàn, hoặc tiếp tục tiến đến phòng cuối cùng để đối đầu với **Trùm Tầng (Floor Boss)**.
* **Giai đoạn 5 — Kết Thúc Chuyến Đi & Tái Đầu Tư (Resolution & Reinvestment):**
  * **Nếu Chiến Thắng Boss hoặc Rút Lui:** Giữ nguyên 100% vật phẩm trong Ba lô, mở khóa tầng tiếp theo (nếu hạ Boss lần đầu), và đưa Boss vào trạng thái hồi sinh trong **3 Ngày**.
  * **Nếu Thất Bại (HP về 0):** Người chơi tỉnh dậy tại Căn cứ vào ngày hôm sau, **mất toàn bộ vật phẩm trong Ba lô** (trừ khi đang trong giai đoạn Bảo Hộ Tân Thủ ở Tầng 1 hoặc trang bị phước lành *Bảo Hộ Thần Thánh*). Chu trình mới lại bắt đầu.

---

## 3. HỆ THỐNG NHÂN VẬT, ĐIỀU KHIỂN & CHỈ SỐ (PLAYER STATS & CONTROLS)

### 3.1. Điều Khiển Nhân Vật Tại Căn Cứ (Overworld Controls)
* **Di chuyển 8 hướng:** Phím `W, A, S, D` hoặc các phím Mũi tên. Tốc độ di chuyển cơ bản (`moveSpeed`) là **5.0 đơn vị/giây**.
* **Chạy nhanh (Sprint):** Giữ phím `Left Shift` khi di chuyển để tăng tốc độ lên **1.5 lần** (**7.5 đơn vị/giây**).
* **Tương tác (Interact):** Phím `E` hoặc `Chuột Trái` khi đứng gần các công trình (Ô đất, Giường ngủ, Bếp, Rương đồ, Tượng Nữ Thần, Cổng Hầm Ngục).
* **Mở Túi Đồ / Đóng Giao Diện:** Phím `I`, `Tab` hoặc `ESC`.

### 3.2. Bảng Chỉ Số Nhân Vật Khởi Đầu (Base Player Stats)
Khi bắt đầu trò chơi mới, nhân vật sở hữu bộ chỉ số gốc được quy định trong `PlayerStats`:

| Chỉ Số (Stat) | Giá Trị Khởi Đầu | Ý Nghĩa & Cơ Chế Hoạt Động |
| :--- | :---: | :--- |
| **Máu Tối Đa (Max HP)** | **50** | Lượng sinh lực tối đa của nhân vật. Khi HP giảm xuống `0`, chuyến thám hiểm lập tức thất bại. |
| **Sức Tấn Công (Base ATK)** | **10** | Sát thương vật lý cơ bản gây ra trong mỗi đòn đánh thường trước khi tính giáp đối phương. |
| **Phòng Thủ (Base DEF)** | **2** | Giảm trực tiếp sát thương nhận vào từ mỗi đòn tấn công của kẻ địch. |
| **Tốc Độ (Base Speed)** | **16** | Quyết định thứ tự ưu tiên hành động khi bắt đầu trận đấu và tương tác với phước lành *Xung Lực Thần Tốc*. |
| **Giá Trị Phòng Thủ (Defend Value)** | **5** | Lượng Giáp Ảo (Shield) cộng thêm mỗi lần người chơi sử dụng hành động `Defend` trong trận đấu. |
| **Điểm Hành Động Tối Đa (Max AP)** | **3** | Số điểm năng lượng cấp mới vào đầu mỗi lượt của người chơi để thực hiện kỹ năng. |
| **Độ No Tối Đa (Max Fullness)** | **100** | Thanh năng lượng sinh tồn. Khởi đầu ở mức `100/100` và giảm dần khi đi qua từng phòng hầm ngục. |

### 3.3. Cơ Chế Độ No & Suy Kiệt (Fullness & Starvation Mechanics)
* **Tiêu hao tự nhiên theo bước tiến:** Mỗi khi chuyển sang một phòng (Wave) mới trong hầm ngục, nhân vật tự động tiêu hao **12 điểm Độ No** (`hungerCostPerRoom = 12`).
* **Trạng thái Đói Bụng (Starving Penalty):** Khi Độ No tụt xuống bằng **0**, nhân vật rơi vào trạng thái suy kiệt (`IsStarving = true`). Ở trạng thái này, mỗi bước tiến sang phòng tiếp theo sẽ trực tiếp rút **8 Máu (HP)** của nhân vật (tuyệt đối không làm nhân vật chết ngay ngoài phòng chờ mà để lại tối thiểu `1 HP` trước khi vào trận).
* **Tương tác chiến thuật với Độ No:**
  * **Ngưỡng Đói (<= 35% Độ No):** Kích hoạt nội tại của phước lành *Cuồng Nộ Khi Đói* (35% tỉ lệ bạo kích gây 150% sát thương).
  * **Ngưỡng No (>= 50% hoặc >= 80% Độ No):** Kích hoạt nội tại của phước lành *Khiên No Nê* (tăng giáp và phản sát thương trực tiếp vào kẻ địch).

---

## 4. HỆ THỐNG CĂN CỨ & NÔNG TRẠI (BASE & FARMING SYSTEM)

### 4.1. Các Công Trình Kiến Trúc Tại Căn Cứ (Base Stations)
Căn cứ (`Base.unity`) là trung tâm hậu cần an toàn tuyệt đối của người chơi, bao gồm 6 khu vực tương tác chính:
1. **Khu Đất Canh Tác (`FarmPlot`):** Hệ thống các ô đất trồng trọt. Mỗi ô đất hiển thị trực quan trạng thái hạt giống, giai đoạn nảy mầm và biểu tượng báo hiệu khi cây đã chín.
2. **Trạm Nấu Ăn (`KitchenStation`):** Nơi chế biến nông sản thành các món ăn hoàn chỉnh. Điểm đặc biệt về tiện ích (QoL): Bếp tự động quét và sử dụng nguyên liệu nằm trong **cả Ba lô lẫn Rương Đồ ở nhà**, giúp người chơi không cần mất công chuyển nguyên liệu qua lại khi đứng nấu.
3. **Rương Cất Trữ (`HomeChest`):** Kho lưu trữ an toàn tuyệt đối. Mọi hạt giống, nông sản và món ăn cất trong Rương sẽ không bao giờ bị mất đi dù người chơi có tử trận trong hầm ngục.
4. **Tượng Nữ Thần (`GoddessStatue`):** Công trình tâm linh cho phép người chơi dâng hiến nông sản để nhận chỉ số vĩnh viễn và mở khóa/trang bị các Phước Lành Thần Thánh.
5. **Giường Ngủ (`BedStation`):** Cho phép người chơi chủ động kết thúc ngày hiện tại, hồi phục toàn bộ HP và thúc đẩy thời gian sang ngày mới (`AdvanceDay(1)`) để cây trồng phát triển nhanh hơn.
6. **Cổng Hầm Ngục (`DungeonEntrance`):** Mở giao diện bản đồ chọn tầng hầm ngục để bắt đầu cuộc hành trình.

### 4.2. Cơ Chế Sinh Trưởng Của Cây Trồng (Crop Growth Mechanics)
Mỗi ô đất (`FarmPlot`) vận hành qua 3 trạng thái: `Empty` (Đất trống) -> `Planted` (Đang phát triển) -> `Ready` (Chín, sẵn sàng thu hoạch).
* Mỗi loại cây trồng có nhiều giai đoạn hình ảnh (`growthStages`). Chỉ số `growthTime` của cây quy định số ngày cần thiết để cây chuyển từ giai đoạn hình ảnh hiện tại sang giai đoạn tiếp theo.
* Khi cây đạt đến giai đoạn cuối cùng (`Ready`), người chơi tương tác để thu hoạch toàn bộ sản lượng (`harvestAmount`) vào túi đồ và ô đất quay trở về trạng thái `Empty`.
* Nếu sự kiện thời tiết **Mưa Rào (Rain)** diễn ra, cây trồng được hưởng hệ số tăng tốc phát triển (`growthMultiplier`), giúp rút ngắn số ngày cần thiết của mỗi giai đoạn.

### 4.3. Bảng Thông Số Chi Tiết Toàn Bộ 7 Loại Cây Trồng (Crop Master Table)
Dưới đây là thông số chính xác của toàn bộ 7 loại nông sản trong trò chơi (dữ liệu trích xuất từ `CropData` và `FoodData`):

| Tên Nông Sản | Thời Gian Mỗi Giai Đoạn (`growthTime`) | Sản Lượng Thu Hoạch (`harvestAmount`) | Hồi Độ No Khi Ăn Sống | Hồi Máu Khi Ăn Sống | Tầng Mở Khóa Hạt Giống | Vai Trò Kinh Tế & Chiến Thuật |
| :--- | :---: | :---: | :---: | :---: | :---: | :--- |
| **Cà Rốt (Carrot)** | 4 ngày | **3 củ** | +5 Độ No | +5 HP | Tầng 1+ | Cây lương thực chủ lực đầu game, sản lượng cao, dùng nấu Súp Cà Rốt tăng Tốc độ. |
| **Lúa Mì (Wheat)** | 5 ngày | **1 bó** | *Nguyên liệu* | *Nguyên liệu* | Tầng 1+ | Nguyên liệu nền thiết yếu để nấu Súp Cà Chua và giao dịch với Nhà Thực Vật Học. |
| **Ngô (Corn)** | 4 ngày | **2 bắp** | +10 Độ No | +10 HP | Tầng 2+ | Giá trị ăn sống rất tốt, nguyên liệu chính cho Ngô Nướng Bơ (+DEF) và Ngô Cay (+ATK). |
| **Cà Chua (Tomato)** | 6 ngày | **1 quả** | +7 Độ No | +10 HP | Tầng 2+ | Nguyên liệu cao cấp để chế biến Súp Cà Chua, Salad Bắp Cải và Đại Tiệc Hoàng Gia. |
| **Bắp Cải (Cabbage)** | 6 ngày | **3 bắp** | +9 Độ No | +10 HP | Tầng 2+ | Sản lượng thu hoạch lớn (3 bắp/vụ), trụ cột cho các món ăn tăng Phòng thủ và hồi máu. |
| **Dưa Chuột (Cucumber)** | 12 ngày | **3 quả** | +9 Độ No | +4 HP | Tầng 2+ | Chu kỳ sinh trưởng dài, chuyên dùng chế biến Salad Dưa Chuột mang lại đa hiệu ứng. |
| **Ớt Hiểm (Chilli)** | 10 ngày | **3 quả** | +5 Độ No | +2 HP | Tầng 3+ | Nông sản hiếm mở khóa ở Tầng 3, gia vị bắt buộc cho các món đại tiệc tăng mạnh ATK. |

---

## 5. HỆ THỐNG ẨM THỰC & CÔNG THỨC NẤU ĂN (COOKING & CULINARY SYSTEM)

### 5.1. Cơ Chế Chế Biến & Khám Phá Công Thức
* Người chơi có thể nấu ăn tại **Trạm Nấu Ăn** ở Căn cứ hoặc nấu dã chiến tại **Lửa Trại (Camp)** trong hầm ngục.
* Khi một công thức được chế biến thành công lần đầu tiên, công thức đó sẽ được ghi nhận vĩnh viễn vào danh sách **Công Thức Đã Khám Phá (`discoveredRecipes`)** trong hồ sơ lưu trữ.
* So với việc ăn nông sản sống, các món ăn qua chế biến mang lại lượng Độ No và Máu vượt trội (gấp 2 đến 4 lần), đồng thời ban tặng các **Hiệu Ứng Cường Hóa (Stat Buffs)** kéo dài theo số lượt chiến đấu (`buffDuration`).

### 5.2. Bảng Tổng Hợp Toàn Bộ Món Ăn & Công Thức Chế Biến (Recipe Master Table)

| Tên Món Ăn (Combo Dish) | Công Thức Nguyên Liệu | Hồi Độ No (`hungerValue`) | Hồi Máu (`healthValue`) | Hiệu Ứng Cường Hóa (Combat Buff) | Thời Lượng Buff (`buffDuration`) |
| :--- | :--- | :---: | :---: | :--- | :---: |
| **Súp Cà Rốt (Carrot Soup)** | 3x Cà Rốt | **+25** | **+18 HP** | Tăng **Tốc Độ (Speed)** | 2 lượt (4 lượt với *Đại Tiệc Vô Tận*) |
| **Súp Cà Chua (Tomato Soup)** | 2x Cà Chua + 1x Lúa Mì *(hoặc 2x Cà Chua)* | **+25** | **+22 HP** | Tăng **+3 ATK** | 2 lượt (4 lượt với *Đại Tiệc Vô Tận*) |
| **Salad Dưa Chuột (Cucumber Salad)** | 3x Dưa Chuột *(hoặc 2x Dưa Chuột)* | **+28** | **+20 HP** | Cường hóa kép **Phòng Thủ & Tốc Độ** | 2 lượt (4 lượt với *Đại Tiệc Vô Tận*) |
| **Salad Bắp Cải (Cabbage Salad)** | 2x Bắp Cải + 1x Cà Rốt + 1x Cà Chua | **+30** | **+25 HP** | Tăng mạnh **Phòng Thủ (DEF)** | 2 lượt (4 lượt với *Đại Tiệc Vô Tận*) |
| **Ngô Cay (Corn Spicy)** | 1x Ngô + 2x Ớt Hiểm *(hoặc 1x Ngô + 1x Ớt)* | **+30** | **+15 HP** | Tăng mạnh **Sức Tấn Công (ATK)** | 2 lượt (4 lượt với *Đại Tiệc Vô Tận*) |
| **Ngô Nướng Bơ (Corn Butter / Grilled Corn)** | 2x Ngô | **+35** | **+25 HP** | Tăng **Phòng Thủ (DEF)** và hồi phục bền bỉ | 2 lượt (4 lượt với *Đại Tiệc Vô Tận*) |
| **Đại Tiệc Hoàng Gia (Spicy Tomato Cabbage Carrot)** | 1x Ớt Hiểm + 2x Cà Chua + 1x Bắp Cải + 1x Cà Rốt | **+50** | **+40 HP** | Tăng đồng thời cả 3 chỉ số: **ATK + DEF + Speed** | 1 lượt (2 lượt với *Đại Tiệc Vô Tận*) |

---

## 6. HỆ THỐNG TƯỢNG NỮ THẦN, HIẾN TẾ & PHƯỚC LÀNH (STATUE & BLESSINGS)

### 6.1. Cơ Chế Hiến Tế Nông Sản (Statue Offerings)
Tại Tượng Nữ Thần ở Căn cứ, người chơi có thể nộp các loại nông sản thu hoạch được vào 3 gói lễ vật để gia tăng vĩnh viễn chỉ số gốc của nhân vật. Hệ thống hỗ trợ **Đóng góp từng phần (Partial Contribution)**: người chơi có thể nộp lẻ từng củ cà rốt hay bắp cải đang có, hệ thống sẽ lưu lại tiến độ cho đến khi đủ số lượng yêu cầu:

| Tên Gói Lễ Vật | Yêu Cầu Nguyên Liệu | Phần Thưởng Vĩnh Viễn | Tác Động Mở Khóa |
| :--- | :--- | :--- | :--- |
| **Lễ Vật Chiến Binh (`Offering_ATK`)** | 2 loại nông sản x 3 đơn vị (Tổng 6 nông sản) | **+1 ATK Vĩnh Viễn** | Mở khóa ngay Phước Lành *Cuồng Nộ Khi Đói* (`StarvingFury`). |
| **Lễ Vật Hộ Vệ (`Offering_DEF`)** | 2 loại nông sản x 3 đơn vị (Tổng 6 nông sản) | **+1 DEF Vĩnh Viễn** | Mở khóa ngay Phước Lành *Tấn Công Pháo Đài* (`BastionStrike`). |
| **Lễ Vật Nhanh Nhẹn (`Offering_Speed`)** | 2 loại nông sản x 3 đơn vị (Tổng 6 nông sản) | **+1 Speed Vĩnh Viễn** | Mở khóa ngay Phước Lành *Xung Lực Thần Tốc* (`SwiftMomentum`). |

### 6.2. Bảng Chi Tiết 8 Phước Lành Thần Thánh (Divine Blessings Specification)
Người chơi được chọn **1 Phước Lành chủ động** trước mỗi chuyến đi hầm ngục. Sự kết hợp giữa Phước Lành, Độ No và Món Ăn tạo nên các trường phái (Builds) đa dạng:

| STT | Tên Phước Lành | Điều Kiện Mở Khóa (`ProgressionManager`) | Cơ Chế & Công Thức Hoạt Động Chi Tiết Trong Chiến Đấu |
| :---: | :--- | :--- | :--- |
| **1** | **Bảo Hộ Thần Thánh** (`KeepLootOnDefeat`) | Mở sẵn từ đầu game. Tự động kích hoạt miễn phí khi ở Tầng 1 (`highestUnlockedFloor <= 2`). | Khi bị đánh bại trong hầm ngục (HP về 0), người chơi **giữ lại 100% vật phẩm trong Ba lô** thay vì bị mất trắng. |
| **2** | **Sức Sống Hoang Dã** (`RawVitality`) | Hoàn thành ít nhất **1 Gói Lễ Vật** bất kỳ tại Tượng Nữ Thần (`completedOfferings >= 1`). | Cường hóa khả năng hấp thụ nông sản thô: ăn nông sản chưa qua chế biến được tăng mạnh lượng Máu và Độ No hồi phục. |
| **3** | **Tấn Công Pháo Đài** (`BastionStrike`) | Mở khóa Tầng 2 **HOẶC** đã nâng cấp `+1 DEF Vĩnh Viễn`. | Chuyển hóa chỉ số Phòng Thủ (DEF) thành sát thương cộng thêm khi tấn công. Đặc biệt: **Lần Phòng Thủ (`Defend`) đầu tiên trong mỗi lượt được hoàn trả ngay +1 AP** (tương đương tạo giáp miễn phí không tốn AP). |
| **4** | **Xung Lực Thần Tốc** (`SwiftMomentum`) | Mở khóa Tầng 2 **HOẶC** đã nâng cấp `+1 Speed Vĩnh Viễn`. | Trong mỗi đòn tấn công, nếu Tốc độ của người chơi cao hơn kẻ địch (`playerSpeed > enemySpeed`), cộng thẳng toàn bộ phần chênh lệch `SpeedDiff = playerSpeed - enemySpeed` vào sát thương đòn đánh (`+X Speed Surge!`). |
| **5** | **Cuồng Nộ Khi Đói** (`StarvingFury`) | Mở khóa Tầng 2 **HOẶC** đã nâng cấp `+1 ATK Vĩnh Viễn`. | Khi thanh Độ No hiện tại giảm xuống **<= 35%**, mọi đòn tấn công của người chơi có **35% tỉ lệ Bạo Kích (`HUNGER CRIT!`)**, nhân **1.5 lần (150%)** tổng sức tấn công trước khi trừ giáp địch. |
| **6** | **Khiên No Nê** (`GluttonousAegis`) | Đánh bại Boss Tầng 2 để mở khóa Tầng 3 (`highestUnlockedFloor >= 3`). | Khi thanh Độ No ở mức cao, nhân vật được cộng thêm DEF và kích hoạt cơ chế **Phản Đòn (`Reflect Damage`)**: mỗi khi bị kẻ địch đánh trúng, tự động phản lại một lượng sát thương trực tiếp vào máu của kẻ địch đó. |
| **7** | **Phân Hủy Cay Đắng** (`BitterDecay`) | Đánh bại Boss Tầng 2 để mở khóa Tầng 3 (`highestUnlockedFloor >= 3`). | Mỗi đòn tấn công của người chơi ăn mòn vĩnh viễn **-2 DEF** của mục tiêu trong suốt trận đấu (`target.ReduceDEF(2)`), đồng thời gây thêm **4 sát thương độc chuẩn (`Decay Poison! -4 HP`)** bỏ qua giáp. |
| **8** | **Đại Tiệc Vô Tận** (`EndlessFeast`) | Hoàn thành **>= 2 Gói Lễ Vật** **HOẶC** đánh bại Boss Tầng 3 (`highestUnlockedFloor >= 4`). | **Nhân đôi (x2) thời lượng duy trì Buff** của mọi món ăn tiêu thụ. Đồng thời, **lần Ăn (`Eat`) đầu tiên trong mỗi lượt tiêu tốn 0 AP** (`CanPlayerEatFree`), cho phép vừa ăn lấy buff vừa dùng trọn 3 AP để tấn công. |

---

## 7. HỆ THỐNG CHIẾN ĐẤU THEO LƯỢT (TURN-BASED COMBAT SYSTEM)

### 7.1. Nhịp Độ & Cấu Trúc Trận Đấu (Combat Flow & Timing)
Trận đấu được điều phối bởi `CombatManager` qua 5 trạng thái (`CombatState`):
1. **Giai đoạn Chuẩn Bị (`Preparation` — 3.0 giây):** Khi bước vào phòng chiến đấu, hệ thống dành `3.0 giây` để hiển thị đội hình quái vật của phòng đó. Cuối giai đoạn chuẩn bị, hệ thống so sánh chỉ số **Speed** của người chơi với chỉ số **Speed cao nhất** trong nhóm quái vật:
   * Nếu `Player Speed >= Max Enemy Speed`: Người chơi giành quyền đi trước (`PlayerTurn`).
   * Nếu `Player Speed < Max Enemy Speed`: Quái vật giành quyền tấn công phủ đầu (`EnemyTurn`).
2. **Lượt Của Người Chơi (`PlayerTurn`):**
   * Đầu mỗi lượt, người chơi được hồi đầy **3 AP** (`currentAP = maxAP`), xóa bỏ lượng Giáp Ảo (`currentShield`) còn tồn đọng từ lượt trước, và giảm `1 lượt` thời gian hiệu lực của các Buff món ăn đang có.
   * Giữa mỗi hành động của người chơi có khoảng nghỉ ngắn `0.35 giây` (`playerActionDelay`) để hiển thị hiệu ứng hình ảnh và số sát thương bay lên (Floating Text).
3. **Các Hành Động Của Người Chơi (1 AP / hành động):**
   * **Tấn Công (`Attack` — 1 AP):** Tấn công kẻ địch đang được chọn làm mục tiêu (hoặc kẻ địch sống đầu tiên trong hàng).
   * **Phòng Thủ (`Defend` — 1 AP, hoặc 0 AP lần đầu nếu có *Tấn Công Pháo Đài*):** Tạo một lớp Giáp Ảo bằng `defendValue` (**+5 Shield**). Có thể cộng dồn nhiều lần trong cùng một lượt.
   * **Ăn Uống (`Eat` — 1 AP, hoặc 0 AP lần đầu nếu có *Đại Tiệc Vô Tận*):** Tiêu thụ 1 vật phẩm thức ăn từ Ba lô, lập tức hồi Máu, hồi Độ No và kích hoạt toàn bộ Buff của món ăn đó.
   * **Kết Thúc Lượt (`End Turn`):** Tự động kích hoạt ngay khi người chơi dùng hết `0 AP` hoặc khi người chơi chủ động bấm nút Kết thúc lượt.
4. **Lượt Của Kẻ Địch (`EnemyTurn`):**
   * Tất cả kẻ địch còn sống lần lượt thực hiện đòn tấn công vào người chơi, cách nhau `0.35 giây` (`enemyTurnDelay`).
5. **Kết Thúc Trận Đấu (`Victory` / `Defeat`):**
   * Khi toàn bộ kẻ địch bị tiêu diệt, sau `1.0 giây` (`victoryDelay`), hệ thống trao thưởng hạt giống rơi ra và chuyển sang phòng tiếp theo.

### 7.2. Hệ Thống Công Thức Tính Toán Sát Thương Chính Xác (Combat Math Formulas)

#### A. Sát Thương Từ Người Chơi Lên Quái Vật (`PlayerAttack`)
1. **Tính Sức Tấn Công Hiệu Dụng (`Effective ATK`):**
   * `ATK_Base_Total = Base ATK (10) + Permanent ATK + Food Buff ATK + Blessing Bonus ATK`
2. **Cộng Sát Thương Chênh Lệch Tốc Độ (nếu trang bị *Xung Lực Thần Tốc*):**
   * `Speed_Bonus = Max(0, Player Speed - Target Enemy Speed)`
   * `ATK_Surge = ATK_Base_Total + Speed_Bonus`
3. **Kiểm Tra Bạo Kích Khi Đói (nếu trang bị *Cuồng Nộ Khi Đói* và Độ No <= 35%):**
   * Với xác suất **35%**, đòn đánh nổ chí mạng (`HUNGER CRIT!`):
   * `ATK_Final = Floor(ATK_Surge * 1.5)`
4. **Trừ Giáp Kẻ Địch & Tính Sát Thương Cuối:**
   * `Damage_Dealt = Max(0, ATK_Final - Enemy Current DEF)`
5. **Hiệu Ứng Ăn Mòn & Độc Tố (nếu trang bị *Phân Hủy Cay Đắng*):**
   * Sau khi gây sát thương vật lý, nếu mục tiêu còn sống:
   * `Enemy_DEF_New = Max(0, Enemy_DEF_Current - 2)`
   * `Enemy_HP_New = Max(0, Enemy_HP_Current - 4)`

#### B. Sát Thương Từ Quái Vật Lên Người Chơi (`TakeDamage`)
1. **Tính Sát Thương Sau Phòng Thủ (`Post-DEF Damage`):**
   * `Damage_After_DEF = Max(0, Enemy ATK - Player Total DEF)`
2. **Hấp Thụ Qua Lớp Giáp Ảo (`currentShield`):**
   * Nếu `currentShield >= Damage_After_DEF`: Giáp ảo hấp thụ toàn bộ, người chơi mất `0 HP` (`Blocked!`) và `currentShield` giảm đi một lượng bằng `Damage_After_DEF`.
   * Nếu `currentShield < Damage_After_DEF`:
     * `HP_Lost = Damage_After_DEF - currentShield`
     * `currentShield = 0`
3. **Phản Sát Thương (nếu trang bị *Khiên No Nê* và đủ ngưỡng Độ No):**
   * Kẻ địch vừa ra đòn lập tức nhận sát thương phản lại trực tiếp vào HP.

---

## 8. CẤU TRÚC HẦM NGỤC, LỬA TRẠI & SỰ KIỆN (DUNGEON, CAMP & EVENTS)

### 8.1. Cấu Trúc 3 Tầng Hầm Ngục & Thuật Toán Sinh Phòng (`FloorGenerator`)
Hầm ngục bao gồm 3 tầng với độ dài và độ khó tăng tiến rõ rệt:
* **Tầng 1 (`Floor_01`):** Tầng khởi đầu (được hưởng cơ chế **Bảo Hộ Tân Thủ** giữ lại toàn bộ đồ nếu chẳng may thất bại). Quái vật chủ yếu là Chuột, Dơi, Slime và Nấm Nổ; canh giữ cuối tầng là **Slime Chúa (`SlimeBoss`)**.
* **Tầng 2 (`Floor_02` — 16 Waves):** Gồm 16 phòng liên tiếp (`F2_W01_Combat` đến `F2_W16_Boss`), bổ sung Xương Chiến Binh, Xương Cung Thủ, Quái Bay; canh giữ cuối tầng là **Vua Xương (`Boss_Skeleton_King`)**.
* **Tầng 3 (`Floor_03` — 18 Waves):** Gồm 18 phòng khắc nghiệt nhất (`F3_W01_Combat` đến `F3_W18_Boss`), xuất hiện các tinh anh Giáp Nặng (`ME1`, `ME2`) và kết thúc bằng trận quyết chiến với **Kỵ Sĩ Bầu Trời (`FlyingRider`)**.

**Thuật toán cân bằng tự động (`FloorGenerator` Score Balance):**
Khi sinh cấu trúc tầng, hệ thống gán điểm trọng số cho từng loại phòng: Phòng Chiến Đấu = `-2 điểm`, Phòng Sự Kiện = `+1 điểm`, Khu Cắm Trại = `+2 điểm`, Thương Nhân = `+3 điểm`. Thuật toán đảm bảo tổng điểm cân bằng của cả tầng luôn nằm trong ngưỡng `[-3, +3]`, đồng thời áp dụng quy tắc chống trùng lặp: không bao giờ xếp 2 phòng Sự kiện/Trại/Thương nhân liên tiếp nhau và giới hạn tối đa 2 phòng Chiến đấu liên tiếp trước khi có nhịp nghỉ.

### 8.2. Hệ Thống Khu Cắm Trại & Thanh Rủi Ro Phục Kích (`Camp` & Ambush Risk Gauge)
Khi bước vào Khu Cắm Trại (`Camp`), người chơi được tạm nghỉ ngoài giao tranh. Tuy nhiên, hầm ngục luôn rình rập nguy hiểm thông qua **Thanh Rủi Ro Phục Kích (`encounterChance`)** khởi đầu ở mức **0%**.

Mỗi hành động sinh hoạt tại trại sẽ làm tiếng động lớn dần, cộng dồn phần trăm vào thanh rủi ro và **lập tức quay số kiểm tra phục kích (`CheckAmbushRoll`)** ngay sau hành động đó:

| Hành Động Tại Trại | Hiệu Quả Nhận Được | Rủi Ro Cộng Thêm | Cơ Chế Chi Tiết |
| :--- | :--- | :---: | :--- |
| **Nghỉ Ngơi (`Rest`)** | Hồi phục **+25 HP** | **+10% Rủi ro** | Giải pháp hồi máu nhanh khi cạn thức ăn nhưng làm tăng nhẹ nguy cơ bị đánh úp. |
| **Ăn Uống (`Eat`)** | Hồi Máu, Độ No & nhận Buff món ăn | **+15% Rủi ro** | Mở túi đồ ăn uống chuẩn bị sẵn Buff cho các phòng tiếp theo mà không tốn AP. |
| **Nấu Ăn Dã Chiến (`Cook`)** | Chế biến nông sản trong Ba lô thành Món ăn | **+20% Rủi ro** | Cho phép biến nông sản nhặt được dọc đường thành thức ăn cao cấp ngay giữa hầm ngục. |
| **Tập Luyện Tiêu Hóa (`Exercise`)** | Giảm **-25 Độ No** (`exerciseHungerReduction`) | **+35% Rủi ro** | Hành động chiến thuật cao cấp: chủ động đốt bớt Độ No để có thể ăn thêm món ăn lấy chồng Buff, hoặc ép Độ No xuống `<= 35%` nhằm kích hoạt *Cuồng Nộ Khi Đói*. |
| **Đi Tiếp (`Continue`)** | Rời trại, tiến sang phòng kế tiếp | **0% Rủi ro** | Kết thúc nghỉ ngơi an toàn và tiếp tục hành trình. |
| **Rút Lui An Toàn (`Return Home`)** | Trở về Căn cứ ngay lập tức | **0% Rủi ro** | Kích hoạt `RetreatRun()`: kết thúc chuyến đi và **bảo toàn 100% chiến lợi phẩm trong Ba lô**. |

* **Cơ chế Phục Kích (`TriggerAmbush`):** Nếu lần quay số ngẫu nhiên rơi vào vùng rủi ro (`Random.value < encounterChance`), một toán quái vật sẽ lập tức tập kích ngay tại trại! Sau khi đánh bại toán phục kích này, trại bị phá hủy và người chơi bắt buộc phải **Đi Tiếp (`mustContinue = true`)**.

### 8.3. Lịch Trình Thương Nhân Theo Ngày (`MerchantEvent` Calendar Cycle)
Thương nhân trong hầm ngục không đứng cố định mãi mãi mà vận hành theo một lịch trình sinh động gắn liền với hệ thống Ngày của trò chơi (được theo dõi riêng cho từng tầng qua `FloorMerchantStatus`):
1. **Trước lần gặp đầu tiên (`hasMet = false`):** Thương nhân luôn luôn có mặt tại tầng đó để chào đón người chơi; số ngày lưu trú chưa bị trừ khi người chơi ngủ ở nhà.
2. **Chu kỳ Lưu Trú 5 Ngày — Nghỉ Nhập Hàng 3 Ngày:** Ngay khi người chơi gặp Thương nhân lần đầu tiên (`RecordMerchantEncounter`), chu kỳ lịch bắt đầu chạy:
   * Thương nhân sẽ ở lại tầng đó trong **5 Ngày liên tiếp** (`DefaultStayDuration = 5`).
   * Hết 5 ngày, Thương nhân rời hầm ngục trong **3 Ngày** (`DefaultCooldownDuration = 3`) để đi nhập chuyến hàng mới. Nếu người chơi đi vào phòng Thương nhân trong 3 ngày này, phòng sẽ trống và người chơi đi qua thẳng.
   * Hết 3 ngày nghỉ, Thương nhân quay trở lại với đầy đủ hàng hóa trong 5 ngày tiếp theo.

### 8.4. Bảng Chi Tiết Toàn Bộ 8 Sự Kiện Tương Tác Trong Hầm Ngục (`DungeonEventManager`)
Khi bước vào phòng Sự Kiện (`Event`) hoặc Thương Nhân (`Merchant`), một hộp thoại tương tác nhiều lựa chọn sẽ xuất hiện. Mọi chi phí và phần thưởng được quy định chính xác như sau:

| STT | Mã & Tên Sự Kiện | Phân Loại | Lựa Chọn 1 (Chi Phí -> Phần Thưởng) | Lựa Chọn 2 (Chi Phí -> Phần Thưởng) | Lựa Chọn 3 |
| :---: | :--- | :---: | :--- | :--- | :--- |
| **1** | **Nhà Thực Vật Học** (`Event_Merchant_Seeds`) | Thương Nhân | **Đổi 2 Hạt Giống cơ bản** (Lúa mì/Cà rốt) -> Nhận **2 Hạt Giống Hiếm** (Cà chua/Ngô). | **Tặng 1 Nông sản bất kỳ** -> Nhận **+3 DEF (6 lượt)** và **+15 Độ No**. | Mở Cửa Hàng / Rời đi. |
| **2** | **Đầu Bếp Lang Thang** (`Event_Merchant_Chef`) | Thương Nhân | **Đổi 2 Nông sản** -> Hồi **+40 HP**, **+30 Độ No** và nhận **1 Món Ăn Chế Biến**. | **Tặng 1 Nông sản** -> Nhận **+4 ATK (6 lượt)** và hồi **+15 HP**. | Mở Cửa Hàng / Rời đi. |
| **3** | **Quan Quân Nhu** (`Event_Merchant_Quartermaster`) | Thương Nhân | **Giao nộp 2 Nông sản** -> Nhận **2 Khẩu Phần Ăn Chế Biến**, **+2 Speed (5 lượt)** và **+20 HP**. | **Đổi 1 Nông sản** -> Băng bó vết thương hồi ngay **+30 HP**. | Mở Cửa Hàng / Rời đi. |
| **4** | **Bàn Thờ Cổ Đại** (`Event_Shrine`) | Sự Kiện | **[Hiến Máu] Hy sinh 12 HP** -> Nhận chúc phúc chiến thần **+3 ATK** và **+2 DEF trong 5 lượt**. | **[Cầu Nguyện Thành Tâm] Tiêu hao 10 Độ No** -> Hồi phục **+35 HP**. | Rời đi an toàn. |
| **5** | **Suối Nguồn Sinh Mệnh** (`Event_Fountain`) | Sự Kiện | **[Uống Nước Suối] Miễn phí** -> Hồi phục ngay **+45 HP** và **+30 Độ No**. | **[Rửa Vũ Khí] Miễn phí** -> Cường hóa vũ khí nhận **+4 ATK trong 6 lượt**. | — |
| **6** | **Cổ Vật Bị Nguyền** (`Event_Relic`) | Sự Kiện | **[Cạy Mở Rương] Đánh cược:** **70% tỉ lệ** nhận **2 Món Ăn Chế Biến**; **30% tỉ lệ** bẫy nổ mất **-15 HP**. | **[Thanh Tẩy Cẩn Thận] Tiêu hao 12 Độ No** -> Phá giải lời nguyền an toàn, nhận **2 Nông sản**. | Bỏ qua rương. |
| **7** | **Người Hái Lượm Lạc Đường** (`Event_Forager`) | Sự Kiện | **[Chia Sẻ Thức Ăn] Tặng 1 Nông sản** -> Nhận lại **3 Hạt Giống** (Ngô/Lúa mì) để cảm ơn. | **[Chỉ Đường Giúp Đỡ] Miễn phí** -> Nhận lối đi tắt tăng **+2 Speed trong 5 lượt**. | — |
| **8** | **Đền Thờ Phước Lành** (`Event_Divine_Blessing`) | Sự Kiện | **[Dâng Hiến Nông Sản] Hy sinh 1 Nông sản** -> Mở khóa vĩnh viễn và kích hoạt ngay **1 Phước Lành Thần Thánh** cho chuyến đi hiện tại. | **[Cầu Nguyện Sùng Kính] Tiêu hao 10 Độ No** -> Hồi **+25 HP** và nhận **+3 ATK, +3 DEF, +2 Speed trong 8 lượt**. | Rời đi. |

---

## 9. HỆ THỐNG QUÁI VẬT & TRÙM TẦNG (BESTIARY & BOSS SPECIFICATION)

### 9.1. Bảng Thông Số Toàn Bộ 10 Quái Vật Thường (Normal Enemies)
Tất cả quái vật thường đều có tỉ lệ rơi hạt giống cơ bản là **35% (`seedDropChance = 0.35`)** khi bị tiêu diệt:

| STT | Tên Quái Vật (`EnemyData`) | Máu (`maxHP`) | Tấn Công (`ATK`) | Phòng Thủ (`DEF`) | Tốc Độ (`Speed`) | Đặc Điểm Chiến Thuật & Vai Trò Trong Đội Hình |
| :---: | :--- | :---: | :---: | :---: | :---: | :--- |
| **1** | **Chuột Hầm Ngục (`Rat`)** | 15 | 5 | 0 | **18** | Máu thấp nhưng tốc độ cao hơn người chơi khởi đầu (18 vs 16), thường đánh phủ đầu ở Tầng 1. |
| **2** | **Nấm Nổ (`Bombschroom`)** | 16 | **8** | 0 | 14 | Sát thương cao ở Tầng 1 nhưng tốc độ chậm và không có giáp; cần ưu tiên tiêu diệt trước khi nó kịp ra đòn. |
| **3** | **Dơi Hút Máu (`Bat`)** | 18 | 6 | 0 | **20** | Một trong những quái vật nhanh nhất hầm ngục (`Speed = 20`), khắc chế lối chơi dựa vào *Xung Lực Thần Tốc* nếu người chơi chưa ăn Súp Cà Rốt. |
| **4** | **Xương Cung Thủ (`Skeleton_Bowman`)** | 20 | 7 | 1 | **19** | Tốc độ rất nhanh, có 1 lớp giáp nhẹ và rỉa máu tầm xa ổn định từ Tầng 2. |
| **5** | **Slime Xanh (`Slime`)** | 22 | 4 | 0 | 8 | Tốc độ rất chậm (`Speed = 8`), là mục tiêu lý tưởng để người chơi tích lũy sát thương thưởng từ *Xung Lực Thần Tốc* (+8 sát thương cộng thêm). |
| **6** | **Quái Bay (`Flying`)** | 24 | 8 | 1 | **19** | Biến thể nâng cấp của Dơi ở Tầng 2–3 với lượng máu và sát thương cao hơn. |
| **7** | **Xạ Thủ Bóng Đêm (`Range`)** | 24 | 8 | 1 | **18** | Đơn vị cấu rỉa chủ lực ở Tầng 2–3, luôn đi trước nếu người chơi không nâng cấp Speed. |
| **8** | **Xương Chiến Binh (`Skeleton`)** | 26 | 8 | 1 | 15 | Bộ binh tiêu chuẩn ở Tầng 2, cân bằng giữa máu, sát thương và tốc độ. |
| **9** | **Thiết Giáp Binh I (`ME1`)** | **30** | 9 | **2** | 16 | Tinh anh hạng trung ở Tầng 2–3; sở hữu 2 DEF làm giảm đáng kể sát thương thông thường. |
| **10** | **Thiết Giáp Khổng Lồ II (`ME2`)** | **42** | **11** | **3** | 10 | Quái vật thường trâu nhất game (`42 HP, 3 DEF`), đòi hỏi người chơi phải có Buff tăng ATK hoặc phước lành *Phân Hủy Cay Đắng* để xuyên giáp. |

### 9.2. Bảng Thông Số Toàn Bộ 3 Trùm Tầng (Floor Bosses)
Mỗi tầng hầm ngục được cai quản bởi một Trùm Tầng. Khi một Trùm Tầng bị tiêu diệt:
* Mở khóa vĩnh viễn tầng hầm ngục kế tiếp và các Phước Lành/Hạt giống cấp cao tương ứng.
* Trùm Tầng đó sẽ bước vào trạng thái **Hồi Sinh (`Boss Cooldown`) kéo dài 3 Ngày** (`defaultBossResetDays = 3`). Trong 3 ngày này, nếu người chơi quay lại tầng đó, phòng cuối sẽ không có Boss cho đến khi đủ 3 ngày trôi qua.

| Tầng | Tên Trùm (`EnemyData`) | Máu (`maxHP`) | Tấn Công (`ATK`) | Phòng Thủ (`DEF`) | Tốc Độ (`Speed`) | Tỉ Lệ & Bảng Rơi Hạt Giống (`seedLootTable`) |
| :---: | :--- | :---: | :---: | :---: | :---: | :--- |
| **Tầng 1** | **Slime Chúa (`SlimeBoss`)** | **85 HP** | **11** | **2** | 12 | **75% tỉ lệ rơi:** Rơi lượng lớn Hạt Cà Rốt, Hạt Lúa Mì và mở đầu nguồn cung Hạt Cà Chua / Hạt Ngô (2–3 hạt mỗi ô rơi). |
| **Tầng 2** | **Vua Xương (`Boss_Skeleton_King`)** | **95 HP** | **12** | **3** | 15 | **80% tỉ lệ rơi:** Rơi dồi dào Hạt Bắp Cải, Hạt Dưa Chuột, Hạt Cà Chua và Hạt Ngô (3–5 hạt mỗi ô rơi). |
| **Tầng 3** | **Kỵ Sĩ Bầu Trời (`FlyingRider`)** | **110 HP** | **14** | **3** | **19** | **80% tỉ lệ rơi:** Trùm cuối có tốc độ cực cao (`19 Speed`), rơi Hạt Ớt Hiểm quý hiếm cùng lượng lớn hạt giống cao cấp (3–5 hạt mỗi ô rơi). |

---

## 10. HỆ THỐNG KHO ĐỒ & QUẢN LÝ VẬT PHẨM (INVENTORY & STORAGE ARCHITECTURE)

### 10.1. Phân Tách Ba Lô & Rương Căn Cứ
Hệ thống lưu trữ vật phẩm được chia thành 2 kho riêng biệt để phục vụ cơ chế Roguelite:
1. **Ba Lô Hành Trang (`Inventory` / Backpack):**
   * Đi theo người chơi vào hầm ngục.
   * Chứa thức ăn mang theo để dùng trong trận đấu/lửa trại và chứa toàn bộ hạt giống, nông sản nhặt được trong chuyến đi.
   * **Quy tắc rủi ro:** Nếu nhân vật tử trận (`HP = 0`) mà không có Bảo Hộ Tân Thủ hoặc phước lành *Bảo Hộ Thần Thánh*, toàn bộ vật phẩm nằm trong Ba lô sẽ bị xóa sạch (`ClearBackpackOnDeath`).
2. **Rương Căn Cứ (`HomeChest`):**
   * Đặt cố định tại Căn cứ.
   * **Quy tắc an toàn:** Bất khả xâm phạm trước mọi thất bại trong hầm ngục.
   * Hỗ trợ thao tác chuyển nhanh vật phẩm qua lại với Ba lô và liên kết trực tiếp với Trạm Nấu Ăn cùng Tượng Nữ Thần.

### 10.2. Phân Loại Vật Phẩm (`ItemData` Hierarchy)
Mọi vật phẩm trong trò chơi kế thừa từ lớp cơ sở `ItemData` (ScriptableObject) và chia thành 3 nhánh:
* **`SeedData`:** Hạt giống dùng để gieo xuống `FarmPlot`. Liên kết trực tiếp với `CropData` tương ứng và chứa thông tin tầng mở khóa.
* **`CropData`:** Nông sản thu hoạch từ nông trại. Có thể dùng làm nguyên liệu nấu ăn, nguyên liệu hiến tế Tượng Nữ Thần, tiền tệ trao đổi ở Sự kiện hầm ngục, hoặc ăn trực tiếp.
* **`FoodData`:** Bao gồm thức ăn đơn lẻ và món ăn chế biến (`Combo`). Lưu trữ thông số `hungerValue`, `healthValue`, danh sách hiệu ứng `statBuffs` (`ATK`, `DEF`, `Speed`) và thời lượng `buffDuration`.

---

## 11. GIAO DIỆN NGƯỜI DÙNG & TRẢI NGHIỆM (UI / UX SPECIFICATION)

### 11.1. Giao Diện Tại Căn Cứ (Base HUD & Panels)
* **Thanh Trạng Thái Góc Trên Trái:** Hiển thị Ngày hiện tại (`Day X`), Thời tiết hiện tại, Thanh Máu (`HP`) và Thanh Độ No (`Fullness`).
* **Giao Diện Trạm Nấu Ăn (`CookingUI`):** Hiển thị danh sách công thức nấu ăn, số lượng nguyên liệu hiện có (tổng hợp từ Ba lô + Rương), thông số hồi phục và nút Nấu ngay.
* **Giao Diện Tượng Nữ Thần (`StatueUI`):**
  * Khu vực Lễ Vật: Hiển thị 3 thẻ lễ vật (`ATK`, `DEF`, `Speed`) cùng các khung hình ảnh nguyên liệu yêu cầu và số lượng đã nộp (`X/3`).
  * Khu vực Phước Lành: Hiển thị danh sách 8 Phước Lành Thần Thánh, trạng thái Đã Mở Khóa / Đang Khóa (kèm gợi ý điều kiện mở khóa) và nút Trang Bị (`Equip`).

### 11.2. Giao Diện Trong Hầm Ngục (Dungeon & Combat HUD)
* **Thanh Tiến Trình Tầng (Wave Progress Header):** Hiển thị Tầng hiện tại, số thứ tự phòng hiện tại trên tổng số phòng (ví dụ: `Wave 5 / 16`) và loại phòng sắp tới.
* **Giao Diện Chiến Đấu (`CombatUI`):**
  * Hiển thị trực quan số Điểm Hành Động (`AP: 3/3`), lượng Giáp Ảo đang bật (`Shield`), các biểu tượng Buff món ăn kèm số lượt còn lại, và nút bấm cho 4 hành động (`Attack`, `Defend`, `Eat`, `End Turn`).
  * Hệ thống chữ nổi động (`FloatingText`): Hiển thị rõ ràng bằng màu sắc riêng biệt cho từng loại tác động: sát thương thường, sát thương chí mạng khi đói (`HUNGER CRIT!`), sát thương cộng thêm từ tốc độ (`Speed Surge!`), sát thương độc ăn mòn giáp (`Decay Poison! -2 DEF`), lượng giáp chặn đòn (`Blocked!`) và máu hồi phục.

---

## 12. KIẾN TRÚC KỸ THUẬT & HỆ THỐNG LƯU TRỮ (TECHNICAL ARCHITECTURE & SAVE SYSTEM)

### 12.1. Kiến Trúc Quản Lý (Singleton & ScriptableObject Architecture)
Trò chơi được xây dựng theo kiến trúc hướng dữ liệu (Data-Driven Architecture) trong Unity:
* **Tầng Dữ Liệu Tĩnh (`ScriptableObjects`):** Toàn bộ thông số của Cây trồng (`CropData`), Hạt giống (`SeedData`), Món ăn (`FoodData`), Công thức (`RecipeData`), Quái vật (`EnemyData`), Cấu trúc tầng (`FloorData`) và Lễ vật (`StatueOfferingData`) được tách biệt hoàn toàn thành các file `.asset` độc lập, giúp cân bằng game nhanh chóng mà không cần sửa mã nguồn.
* **Tầng Quản Lý Vòng Đời (`Persistent Singletons`):**
  * `ProgressionManager`: Quản lý tiến trình vĩnh viễn xuyên suốt giữa các cảnh (`DontDestroyOnLoad`), bao gồm tầng đã mở khóa, chỉ số vĩnh viễn, phước lành, lịch hồi sinh Boss và lịch Thương nhân.
  * `TimeManager`: Quản lý bộ đếm Ngày toàn cục và phát sự kiện `OnDayChanged` cho toàn bộ hệ thống nông trại, Boss và Thương nhân.
  * `CombatManager` & `DungeonWaveManager`: Điều phối vòng lặp chiến đấu, sinh phòng và chuyển tiếp giữa các Wave trong `Dungeon.unity`.

### 12.2. Bảng Đặc Tả Dữ Liệu Lưu Trữ Vĩnh Viễn (Save System Specification)
Trò chơi tự động lưu trữ trạng thái tiến trình thông qua hệ thống khóa `PlayerPrefs` kết hợp tuần tự hóa JSON:

| Khóa Lưu Trữ (`Save Key`) | Kiểu Dữ Liệu | Nội Dung Lưu Trữ Chi Tiết |
| :--- | :---: | :--- |
| `Progression_HighestFloor` | `Int` | Tầng hầm ngục cao nhất người chơi đã mở khóa (mặc định = `1`). |
| `Progression_PermATK` / `DEF` / `Speed` | `Int` | Tổng chỉ số `ATK`, `DEF`, `Speed` vĩnh viễn đã nhận từ Tượng Nữ Thần. |
| `Progression_CompletedOfferings` | `String (CSV)` | Danh sách mã các gói lễ vật đã hoàn thành. |
| `Progression_OfferingContributions` | `String` | Tiến độ số lượng nông sản đã nộp từng phần vào từng ô lễ vật chưa hoàn tất. |
| `Progression_UnlockedBlessings` | `String (CSV)` | Danh sách các Phước Lành Thần Thánh đã mở khóa vĩnh viễn. |
| `Progression_ActiveBlessing` | `Int (Enum)` | Phước Lành đang được người chơi chọn trang bị cho chuyến đi. |
| `Progression_DiscoveredRecipes` | `String (CSV)` | Danh sách các công thức nấu ăn người chơi đã từng chế biến thành công. |
| `Progression_BossCooldowns` | `String` | Số ngày còn lại trước khi Boss của từng tầng hồi sinh. |
| `Progression_MerchantStatus` | `String` | Trạng thái của Thương nhân ở từng tầng (`hasMet`, `isAvailable`, `daysRemaining`). |

---

## 13. PHÂN TÍCH CÂN BẰNG KINH TẾ & CÁC TRƯỜNG PHÁI CHIẾN THUẬT (GAME BALANCE & ARCHETYPES)

### 13.1. Ba Trường Phái Chiến Thuật Điển Hình (Viable Player Builds)
Nhờ sự giao thoa giữa Nông sản, Món ăn và 8 Phước Lành, người chơi có thể định hình rõ rệt 3 lối chơi chuyên biệt:

1. **Trường Phái Sát Thủ Nhịn Đói (The Starving Berserker Build):**
   * **Phước lành chủ đạo:** *Cuồng Nộ Khi Đói* (`StarvingFury`).
   * **Vận hành:** Khi vào Khu Cắm Trại, chủ động dùng hành động **Tập Luyện Tiêu Hóa (`Exercise`)** để đốt Độ No xuống dưới mức **35/100**. Mang theo các nông sản hồi máu nhưng hồi ít Độ No (như Cà Chua, Cà Rốt sống) hoặc kết hợp với *Ngô Cay* (`+ATK`).
   * **Sức mạnh:** Duy trì 35% tỉ lệ nổ chí mạng **150% tổng ATK** ở mọi đòn đánh, kết liễu quái vật Tầng 2–3 chỉ trong 1–2 lượt.

2. **Trường Phái Tia Chớp Tốc Độ (The Speed Surge Build):**
   * **Phước lành chủ đạo:** *Xung Lực Thần Tốc* (`SwiftMomentum`).
   * **Vận hành:** Ưu tiên trồng **Cà Rốt** (thời gian chín nhanh, thu hoạch 3 củ/ô) để nấu hàng loạt **Súp Cà Rốt** và hiến tế `Offering_Speed` tại Tượng Nữ Thần.
   * **Sức mạnh:** Luôn giành quyền đi trước mọi kẻ địch trong hầm ngục và biến toàn bộ điểm Speed vượt trội thành sát thương cộng thẳng vào mỗi đòn đánh (đặc biệt hủy diệt các mục tiêu chậm chạp như Slime, Thiết Giáp Khổng Lồ `ME2` hay `SlimeBoss`).

3. **Trường Phái Pháo Đài Thực Thần (The Gluttonous Fortress Build):**
   * **Phước lành chủ đạo:** *Tấn Công Pháo Đài* (`BastionStrike`), *Khiên No Nê* (`GluttonousAegis`) hoặc *Đại Tiệc Vô Tận* (`EndlessFeast`).
   * **Vận hành:** Trồng **Ngô** và **Bắp Cải** số lượng lớn để chế biến *Ngô Nướng Bơ* và *Salad Bắp Cải*. Giữ thanh Độ No luôn trên 80% và tận dụng lượt `Defend` miễn phí AP mỗi lượt.
   * **Sức mạnh:** Sở hữu lượng DEF và Giáp Ảo khổng lồ khiến quái vật đánh vào chỉ gây `0 sát thương (Blocked!)`, trong khi kẻ địch tự mất máu đến chết do hiệu ứng Phản Đòn và sát thương chuyển hóa từ giáp.

---

## 14. LỘ TRÌNH PHÁT TRIỂN & MỞ RỘNG (DEVELOPMENT ROADMAP)

* **Giai đoạn Alpha (Đã hoàn thiện):**
  * Hoàn chỉnh vòng lặp Căn Cứ <-> Hầm Ngục 3 tầng.
  * Hoàn thiện 7 loại cây trồng, 7 món ăn chế biến, 10 quái vật thường, 3 Boss tầng, 8 Sự kiện tương tác, hệ thống Lửa Trại và Tượng Nữ Thần với 8 Phước Lành.
* **Giai đoạn Beta (Cân bằng & Đa dạng hóa):**
  * Mở rộng Tầng 4 & Tầng 5 với cơ chế thời tiết khắc nghiệt ảnh hưởng trực tiếp vào hầm ngục (Sương mù giảm tầm nhìn, Nắng hạn tăng tiêu hao Độ No).
  * Bổ sung hệ thống Nâng cấp Công trình tại Căn cứ (Mở rộng thêm ô đất canh tác, Nâng cấp Bếp giảm nguyên liệu yêu cầu, Nâng cấp Rương).
* **Giai đoạn Gold Master (Hoàn thiện trải nghiệm):**
  * Tối ưu hóa hiệu ứng âm thanh (SFX) cho từng nhát cuốc, tiếng sôi của nồi súp, tiếng va chạm giáp và nhạc nền chuyển đổi mượt mà giữa giai điệu đồng quê yên bình (Base) sang nhịp trống dồn dập (Dungeon Boss).
