# BÁO CÁO TIẾN ĐỘ HOÀN THIỆN GAME (CHƯƠNG 1 & CHƯƠNG 2)

**Dự án:** Architectural Perspective Puzzle Game (phong cách Monument Valley, không gian ký ức Hà Nội)  
**Cuộc thi:** Digital Interactive Environment Design Competition (Đại học Thanh Hoa x miHoYo)  
**Ngày cập nhật:** 11/10/2026  
**Nhánh:** `dev`  
**Unity:** 6000.3.25f1  
**Scenes (Build Settings):** `Level1` (0), `MainGame` (1), `Level2` (2)  
**Bản build Windows 64-bit:** `Builds/StandaloneWindows64/PerspectivePuzzle.exe`

---

## 0. Ghi chú trung thực về đợt sửa lỗi này

- Bản Chương 2 cũ **không chơi được bằng chuột/bàn phím thật**: các ô đi bộ nằm ngoài khung hình, nhiều vật thể không có collider, nên click không trúng gì. Trước đây nó chỉ được “kiểm tra” bằng cách gọi hàm trực tiếp trong script, chứ không phải bằng input thật. Phần kiểm thử đó không có giá trị.
- Đợt này, Chương 2 được **viết lại hoàn toàn** (mã mới trong `Assets/Scripts/Level2/`).
- Toàn bộ input của game (Chương 1 và 2) giờ đi qua một lớp duy nhất là `GameInput`.
- Việc kiểm thử dùng **bộ chơi thử tự động** (`Level1Playtest`, `Level2Playtest`). Bộ này bơm click, kéo và phím vào toạ độ pixel trên màn hình, đi đúng đường của chuột thật: pixel → tia ray → collider → logic game.

---

## 1. Chương 1: “Tiếng Loa Phường” (`Assets/Scenes/Level1.unity`)

- Ngôi nhà phố cổ đặt trên bệ xoay. Kéo chuột ngang để xoay nhà, click vào ô để đi.
- **Căn hành lang 315°:** hai mép sàn B và C trùng nhau trên ảnh 2D, mở ra lối lên tầng 2.
- **Câu đố tủ chè:** xoay hai khối về nghiệm (1, 3) thì cuộn băng cassette của Bà lộ ra.
- **Con chó trên cầu thang:** nếu đi lên ở góc không an toàn, nhân vật bị chặn, ngã và trượt về điểm xuất phát (vẫn giữ cuộn băng). Góc an toàn là 250°.
- **Leo mái:** chuỗi góc E → F → I → L → G → M. Kết thúc bằng việc gắn băng lên cột loa phường.
- **Màn hình kết:** có nút sang Chương 2. Phím **Enter/Space** cũng chuyển sang Chương 2, **Esc** để thoát.

## 2. Chương 2: “Hà Nội Hoàng Hôn” (`Assets/Scenes/Level2.unity`), viết lại hoàn toàn

Chương 2 là một mô hình diorama phố cổ dựng hoàn toàn bằng code lúc chạy, gồm:
- Nhà ống, quán phở và cà phê, biệt thự Pháp có mái ngói, tháp chuông với chòi mái cong.
- Hồ nước, cột điện chằng chịt dây, ghế nhựa, xe Cub, diều và đèn trời.

Mục tiêu là thắp lại **3 chiếc đèn lồng** mà Bà vẫn thắp mỗi Trung thu.

| Bước | Cơ chế | Thao tác |
|---|---|---|
| 1 | **Thang máy đỏ** đưa nhân vật từ phố lên mái nhà ống | Click ô thang để bước lên, rồi **kéo thang lên** (hoặc click thân thang) |
| 2 | Đèn lồng 1 trên mái nhà ống | Click ô cuối mái |
| 3 | **Cầu xoay** nối mái nhà ống với biệt thự | **Kéo cầu theo vòng tròn** quanh trụ, hoặc click **tay quay** trên trụ cầu (mỗi lần xoay 90°) |
| 4 | Đèn lồng 2 trên lối đi của biệt thự | Đi qua cầu |
| 5 | **Đường nối bất khả** (ảo giác phối cảnh): mép biệt thự và đỉnh tháp chỉ trùng nhau khi nhìn từ một góc | **Kéo vùng trời trống** để xoay cả thành phố (hoặc **Q/E**, phím mũi tên) về đúng góc |
| 6 | Bước qua đường nối lên tháp chuông | Chuông ngân, đèn trời bay lên, phố lên đèn khi màn đêm buông xuống |

Ngoài ra còn có:
- Mục tiêu hiển thị liên tục và 3 chấm đèn lồng trên HUD.
- Gợi ý dạng **viên kim cương phát sáng** sau khoảng 11 giây không thao tác; các bộ phận cơ khí cũng nhấp nháy để nhắc.
- Tiếng bước chân, thông báo ngắn và phụ đề kể chuyện.
- Màn hình kết thúc có thời gian và số bước. Phím: **R/Enter** chơi lại, **C** về Chương 1, **Esc** thoát.

## 3. Kiểm thử

### 3.1. Chơi thử tự động bằng input thật (pixel)

- **Chương 2, đường kéo chuột** (`Level2Playtest.Run(false)`): **PASS**. Đi hết từ intro qua thang máy, đèn 1, kéo cầu, đèn 2, kéo trời xoay thành phố, đường nối, chuông, cho tới màn kết.
- **Chương 2, đường click và phím** (`Level2Playtest.Run(true)`): **PASS**. Click thân thang, click tay quay cầu, dùng phím Q để xoay thành phố.
- **Kiểm tra phủ định** (đều đạt):
  - Click mái khi chưa lên được thì nhân vật không đi.
  - Biệt thự không tới được trước khi xoay cầu.
  - Tháp không tới được trước khi xoay đúng góc.
- **Chương 1** (`Level1Playtest.Run()`): **PASS** (xem mục 3.3).
- **Chuyển chương bằng phím** từ màn kết Chương 2 về Chương 1 (phím C): đạt.

### 3.2. Unit tests (EditMode)

- `StoryRulesTests`, `CabinetBlocksTests`, `StairSpanTests` cho Chương 1.
- `Level2RulesTests` cho Chương 2: góc camera, chiều cao thang, chuẩn hoá góc, điều kiện cầu, điều kiện đường nối, phép chiếu phối cảnh.
- Kết quả: xem mục 3.3.

### 3.3. Kết quả lần chạy cuối (11/10/2026, ~11:30)

| Hạng mục | Kết quả |
|---|---|
| Level2Playtest – kéo chuột | **PASS** (toàn bộ check `ok`) |
| Level2Playtest – click/phím | **PASS** (toàn bộ check `ok`) |
| Level1Playtest | Hoàn thành tới màn hình kết (`end screen: True`) |
| Chuyển chương: Level1 Enter → Level2, Level2 C → Level1 | Đạt |
| EditMode tests | **38/38 Passed** |
| Console khi chơi cả 2 chương | **0 lỗi** |

**Chạy trong bản build thật** (development build, cửa sổ 1280x720, lệnh `PerspectivePuzzle.exe -pt1 | -pt2drag | -pt2click`):

| Bài test | Kết quả |
|---|---|
| `-pt1` (Chương 1) | **PASS**, exit code 0 |
| `-pt2drag` (Chương 2, kéo chuột) | **PASS**, exit code 0 |
| `-pt2click` (Chương 2, click/phím) | **PASS**, exit code 0 |

> Lỗi tìm ra nhờ chạy trong bản build: tủ chè **bỏ qua các click bấm trong lúc khối đang xoay** (0,22 giây), nên người chơi bấm nhanh sẽ bị mất lượt xoay. Đã sửa: click mới sẽ hoàn tất ngay lượt xoay đang chạy rồi xoay tiếp.

## 4. Hướng dẫn giám khảo / người chơi

1. **Chạy game:** mở `Builds/StandaloneWindows64/PerspectivePuzzle.exe`. Trong Editor, Play scene `Assets/Scenes/Level1.unity`.
2. **Chương 1:**
   - Click vào ô để đi, kéo ngang để xoay nhà.
   - Tủ chè: click để xoay khối, chuột phải hoặc Esc để thoát.
   - Màn kết: Enter để sang Chương 2.
3. **Chương 2:**
   - Click vào ô để đi.
   - **Kéo hoặc click phần màu đỏ** để điều khiển thang máy và cầu xoay (tay quay trên trụ cầu).
   - Kéo vùng trời trống, hoặc Q/E (phím mũi tên), để xoay cả thành phố.
   - R để chơi lại. Nút “Chương 1” ở góc phải trên.
   - Màn kết: R/Enter chơi lại, C về Chương 1, Esc thoát.
4. **Chạy lại bài chơi thử tự động** (trong Editor, đang Play scene tương ứng):
   - `PerspectivePuzzle.Level2Playtest.Run(false)` hoặc `Run(true)`
   - `PerspectivePuzzle.Level1Playtest.Run()`
