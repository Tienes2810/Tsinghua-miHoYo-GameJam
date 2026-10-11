# BÁO CÁO TIẾN ĐỘ HOÀN THIỆN TOÀN BỘ GAME (CHƯƠNG 1 & CHƯƠNG 2)

**Dự án:** Architectural Perspective Puzzle Game (Phong cách Monument Valley - Không gian Ký ức Hà Nội)  
**Cuộc thi:** Digital Interactive Environment Design Competition (Đại học Thanh Hoa x miHoYo)  
**Ngày hoàn thiện:** 11/10/2026  
**Nhánh:** `dev`  
**Scenes hoàn thiện:** 
- `Assets/Scenes/Level1.unity` (Chương 1: Tiếng Loa Phường)
- `Assets/Scenes/Level2.unity` (Chương 2: Hà Nội Hoàng Hôn)
**Bản Build Standalone:** `Builds/PerspectivePuzzle.exe` & `Builds/StandaloneWindows64/PerspectivePuzzle.exe` (Windows 64-bit, 226 MB, 0 errors)  
**Unity Version:** 6000.3.25f1  
**Trạng thái kiểm thử:** 16/16 Unit Tests Passed (100% Green), Console 0 Lỗi (0 Errors).

---

## 1. Tổng quan Dự án & Tính cạnh tranh cho Cuộc thi

Dự án đã được phát triển và hoàn thiện thành một tác phẩm nghệ thuật giải đố phối cảnh hoàn chỉnh gồm **2 Chương truyện liền mạch** đậm chất văn hóa và thẩm mỹ:

1. **Ý tưởng cốt lõi & Tính nhân văn:**
   - Kết hợp lối chơi giải đố không gian 3D dạng isometric (kinh điển từ *Monument Valley*) với kiến trúc nhà ống và không gian sinh hoạt phố cổ Hà Nội.
   - Trải nghiệm đưa người chơi đi tìm những mảnh ghép ký ức: từ cuộn băng cassette của người Bà phát thanh trên loa phường đến khoảnh khắc thắp sáng những ngọn đèn lồng đón hoàng hôn buông xuống trên mái ngói rêu phong.

2. **Hệ thống Âm thanh & Hiệu ứng Thẩm mỹ cao cấp (Audio & Visual Polish):**
   - **Âm thanh bầu không khí (Atmospheric Ambience):** Vòng lặp âm thanh nền sâu lắng, êm dịu tái hiện không gian tĩnh lặng của phố cổ.
   - **Âm phản hồi tương tác (SFX Feedback):** Âm click thanh thoát (`navtouch`), âm rơ khớp nhịp cầu (`bridgedrop`), âm mở ngăn tủ (`door_slide`), âm chuông thành tựu (`level_complete`), âm gõ bước chân (`buttonTouch`), âm báo động của chó.
   - **Vòng sóng phản hồi chạm (Click Ripple Effect):** Hiệu ứng vòng tròn ánh sáng tỏa rộng dạng sóng nước (Monument Valley style) tại vị trí click chuột.
   - **Bụi khí quyển lấp lánh (Atmospheric Dust Particles):** Hệ thống hạt lơ lửng tự nhiên mang ánh vàng ấm áp trong không gian isometric.

---

## 2. Chi tiết Nội dung & Cơ chế Giải đố theo từng Chương

### CHƯƠNG 1: "TIẾNG LOA PHƯỜNG" (`Assets/Scenes/Level1.unity`)

- **Intro & Khởi đầu:** Lời dẫn song ngữ/Việt dẫn dắt cảm xúc, hiệu ứng làm mờ và chuyển cảnh mượt mà vào ngôi nhà phố cổ đầy đủ texture gốc. Bệ xoay màu vàng xoay đồng trục, camera isometric (pitch 32°, yaw 45°) đứng yên cố định.
- **Hành lang tầng 1 (A → B):** Nhân vật di chuyển chân thực trên từng bậc thang của `CauThang1`, animation đi bộ (`Walking.fbx`) đồng bộ, không trôi nổi, không xuyên sàn.
- **Cơ chế Căn góc Hành lang 315° (Corridor Alignment):**
  - Điểm B là ngõ cụt ở góc nhìn thường.
  - Khi xoay ngôi nhà về góc `CorridorYaw = 315°` (dung sai 12°), hai mép sàn B và C trùng khít trên phối cảnh 2D, âm thanh khớp cầu vang lên, mở lối thông vào phòng tầng 2.
- **Phòng Tầng 2 & Hệ thống Cutaway Nội thất:**
  - Tự động ẩn các tầng mái phía trên (cao độ y ≥ 7.0) khi vào phòng và xoay nhẹ về `RoomViewYaw = 280°` để người chơi chiêm ngưỡng rõ nét tủ chè, bàn trà và nhân vật.
- **Câu đố Tủ Chè 3D (Cabinet View Puzzle):**
  - Mở giao diện câu đố tương tác 3D riêng biệt, hỗ trợ hủy/quay lại bằng chuột phải hoặc phím Escape.
  - Khi xoay hai khối đạt nghiệm `(1, 3)`, cuộn băng cassette của Bà phát lộ.
  - Nhân vật nhặt cuộn băng, gắn trực tiếp lên bàn tay (`mixamorig6:RightHand`) và chuyển sang animation cầm đồ (`Carry.fbx`).
- **Cơ chế Tuần tra & Bẫy Chó trên Cầu thang 2:**
  - Bám sát tài liệu thiết kế gốc: Con chó tuần tra trên cầu thang tầng 2.
  - Khi người chơi cố đi lên ở góc xoay chưa an toàn (khác 250°), chó lao xuống chặn đường, nhân vật giật mình ngã (`Falling.fbx`), trượt về điểm xuất phát A nhưng vẫn bảo lưu cuộn băng cassette.
  - Khi xoay đúng góc an toàn `StairYaw = 250°`: chó ngừng kích hoạt, người chơi đi an toàn từ A qua sảnh đón (Hall) lên chiếu nghỉ E tầng 3.
- **Lộ trình Leo Mái Rooftop (E → F → I → L → G → M):**
  - Chuỗi điểm mốc yêu cầu phối cảnh góc xoay tinh vi: E (250°) → F (200°) → I (160°) → L (245°) → G (190°) → M (280°).
  - Tận dụng bờ tường, bệ gạch, nhịp bước sàn thực tế để tạo nên trải nghiệm leo trèo thú vị.
- **Đoạn kết & Cột Loa Phường:**
  - Nhân vật đứng tại đỉnh M, cuộn băng bay lên gắn vào cột loa phát thanh (`MountOnPole`).
  - Âm thanh radio stinger phát tiếng rè băng cassette cùng đoạn nhạc hiệu phát thanh Hà Nội cổ điển vang lên.
  - Camera lùi toàn cảnh, hiển thị thông điệp tri ân và nút chuyển tiếp ngay sang **Chương 2: Hà Nội Hoàng Hôn**.

---

### CHƯƠNG 2: "HÀ NỘI HOÀNG HÔN" (`Assets/Scenes/Level2.unity`)

- **Bầu không khí Hoàng Hôn (Sunset Mood):**
  - Bầu trời chuyển sang sắc tím chạng vạng (`Twilight Indigo`), ánh nắng chiều chiếu xiên màu hổ phách vàng ấm (`Amber Light`).
  - Hạt bụi hoàng hôn lơ lửng ánh vàng hồng ấm áp.
- **Cơ chế 3 Ngọn Đèn Lồng Cổ (The 3 Sunset Lanterns):**
  - Người chơi có nhiệm vụ đánh thức và thắp sáng 3 ngọn đèn lồng nằm ở các góc cao của kiến trúc phố cổ:
    + **Đèn lồng 1 (Ban công Đông):** Bước từ sân gạch lên ban công Đông. Chạm vào đèn lồng, ngọn lửa ấm bừng sáng kèm hiệu ứng ánh sáng bùng nở và âm báo thành tựu (1/3).
    + **Đèn lồng 2 (Cầu Không Gian & Ban công Tây):** Đứng trước vực sâu ngăn cách hai bờ đông - tây, người chơi xoay ngôi nhà về góc `BridgeYaw = 135°`. Nhịp cầu không gian (`connect1.fbx`) khớp nối với mép ban công, mở lối bước qua thắp sáng Đèn lồng 2 (2/3).
    + **Đèn lồng 3 & Tháp Chuông Hoàng Hôn (Sunset Bell Tower):** Xoay nhà về góc `TowerYaw = 225°` để nối liền dải thang lên đỉnh tháp chuông cao nhất.
- **Đại kết cục Tháp Chuông (Grand Climax & Bell Ringing):**
  - Nhân vật bước lên đỉnh tháp cao nhất, thắp sáng ngọn đèn thứ 3 và thỉnh quả Chuông Hoàng Hôn.
  - Chuông đung đưa ngân vang giai điệu hoàng hôn, tỏa sáng rực rỡ khắp toàn cảnh phố cổ.
  - Màn hình chiến thắng hiện ra cùng 2 lựa chọn tương tác:
    + **[ CHƠI LẠI CHƯƠNG 2 ]**
    + **[ ← VỀ CHƯƠNG 1 ]**

---

## 3. Danh mục Kiểm thử & Chất lượng Kỹ thuật

### 3.1. Kết quả Unit Tests (16/16 Tests Passed - 100%)
- **Chương 1 Tests (12/12):**
  1. `CabinetBlocksTests.CassetteIsReachableOnlyAtTheSolvedTurns`: Passed.
  2. `StairSpanTests.PlankMeetsBothStairLipsOnlyInsideTheJoinWindow`: Passed.
  3. `StoryRulesTests.ClueDoesNotRepeat`: Passed.
  4. `StoryRulesTests.CorridorAndStairStayInsideTheirOwnTolerance`: Passed.
  5. `StoryRulesTests.DogFailRestoresTheStartAndKeepsUnlockedRotations`: Passed.
  6. `StoryRulesTests.PositiveDragFollowsTheHand`: Passed.
  7. `StoryRulesTests.PresenterColorsAreNotBlackOnBlack`: Passed.
  8. `StoryRulesTests.RoofOrderWaitsForItsOwnRotation`: Passed.
  9. `StoryRulesTests.RoofStepStandsOnTheExistingSlope`: Passed.
  10. `StoryRulesTests.ShippedSeamIsOneWalkOnlyAtTheCorridorYaw`: Passed.
  11. `StoryRulesTests.SpeakerMovesFromHeldToPlacedOnlyOnThePole`: Passed.
  12. `StoryRulesTests.YawInsideToleranceOpensTheCorridorAndOutsideDoesNot`: Passed.
- **Chương 2 Tests (4/4):**
  13. `Level2RulesTests.BridgeAlignmentOpensOnlyWithinTolerance`: Passed (135° ± 14°).
  14. `Level2RulesTests.TowerAscentOpensOnlyWithinTolerance`: Passed (225° ± 14°).
  15. `Level2RulesTests.LanternPropInitialStateIsUnlit`: Passed.
  16. `Level2RulesTests.Level2PresenterHasValidTotalLanterns`: Passed (3/3 lanterns).

### 3.2. Hiệu năng & Ổn định
- **Tốc độ khung hình:** 58 - 61 FPS mượt mà ổn định.
- **Console Log:** 0 Errors, 0 Warnings trong suốt cả 2 lượt playthrough.
- **Bản Build Standalone:** Thành công 100% không cảnh báo lỗi tại `Builds/PerspectivePuzzle.exe`.

---

## 4. Hướng dẫn Giám khảo / Người chơi Trải nghiệm

1. **Khởi chạy game:**
   - Mở file thực thi: `Builds/PerspectivePuzzle.exe` (hoặc nhấn Play scene `Assets/Scenes/Level1.unity` trong Unity Editor).
2. **Điều khiển:**
   - **Kéo giữ chuột trái và di chuyển ngang:** Xoay kiến trúc ngôi nhà quanh trục trung tâm để tìm góc phối cảnh ăn khớp.
   - **Click chuột trái:** Di chuyển nhân vật đến điểm mốc (xuất hiện vòng sóng vàng `ClickRipple`).
   - **Tủ chè:** Click chuột trái để xoay khối; click chuột phải hoặc bấm phím `Esc` để thoát câu đố.
   - **Chuyển chương nhanh:** Nhấn nút chuyển chương ở góc phải trên màn hình để chuyển đổi qua lại giữa Chương 1 và Chương 2 bất cứ lúc nào.
   - **Thoát game:** Nhấn phím `Esc` tại màn hình kết thúc.
