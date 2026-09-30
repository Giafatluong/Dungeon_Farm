# 🏠 Hướng dẫn UI Rương Nằm Ngang (Đồng bộ 100% với Inventory)

## 🌟 Thiết kế Mới: Nằm Ngang Chuẩn Pixel Art

Toàn bộ UI Rương được thiết kế lại thành **bố cục nằm ngang (Horizontal)**, đồng bộ hoàn hảo từng pixel với giao diện Inventory trong scene:

```
┌──────────────────────────────────────────────────────────────────┐
│                   📦 RƯƠNG NHÀ (CHEST)                           │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │ 33 Ô CHỨA • BẢO VỆ AN TOÀN 100% KHI VÀO DUNGEON        [✕] │  │
│  │                                                            │  │
│  │ [50x50][50x50][50x50][50x50][50x50] ... (11 ô hàng 1)      │  │
│  │ [50x50][50x50][50x50][50x50][50x50] ... (11 ô hàng 2)      │  │
│  │ [50x50][50x50][50x50][50x50][50x50] ... (11 ô hàng 3)      │  │
│  └────────────────────────────────────────────────────────────┘  │
│                                                                  │
│                       [ ❌ ĐÓNG (ESC / E) ]                      │
│                                                                  │
│                   🎒 BA LÔ (INVENTORY)                           │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │ 33 Ô CHỨA • TÚI ĐỒ MANG THEO                               │  │
│  │                                                            │  │
│  │ [50x50][50x50][50x50][50x50][50x50] ... (11 ô hàng 1)      │  │
│  │ [50x50][50x50][50x50][50x50][50x50] ... (11 ô hàng 2)      │  │
│  │ [50x50][50x50][50x50][50x50][50x50] ... (11 ô hàng 3)      │  │
│  └────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────┘
```

---

## 🔍 Khắc phục lỗi Slot bị lệch:
1. **Kích thước khung**: Cả 2 khung Rương và Ba lô đều có kích thước chuẩn **`600 x 290`** (giống hệt `inventory` của người chơi), dùng `Image.Type = Simple` để hình ảnh không bị méo.
2. **Tọa độ căn chỉnh slot**:
   - `Padding`: `Left: 24, Top: 123` (vừa khít với các ô vẽ sẵn trên `UI_Frame.png`).
   - `CellSize`: `50 x 50`.
   - `Spacing`: `0.01, 0`.
   - `Constraint`: `11 cột x 3 hàng` = 33 ô.
3. **Chống lệch do Text**: Tiêu đề và phụ đề đều được gắn `LayoutElement (ignoreLayout = true)` để `GridLayoutGroup` không đẩy lệch vị trí các slot.
4. **Tạo sẵn trực quan trong Scene**: Toàn bộ 33 slot của mỗi khung được sinh sẵn trong Scene Base, bạn có thể kiểm tra trực tiếp trong cửa sổ Scene / Game view.

---

## 🚀 Kích hoạt thiết lập (Chỉ 1 Click):
1. Chuyển sang cửa sổ **Unity Editor** (đang mở scene `Base`).
2. Trên thanh menu trên cùng, click:
   👉 **`Tools` → `Tạo UI Rương Nằm Ngang (Setup Chest UI in Scene)`**
3. Hộp thoại báo thành công hiện ra: scene sẽ tự động cập nhật và lưu lại.
4. Nhấn **Play** và trải nghiệm: đến rương bấm **`E`** để mở, kéo thả đồ qua lại giữa 2 khung nằm ngang!
