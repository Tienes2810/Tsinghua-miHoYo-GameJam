# BÁO CÁO TIẾN ĐỘ HOÀN THIỆN LEVEL 1

**Dự án:** Architectural Perspective Puzzle Game (Phong cách Monument Valley)  
**Cuộc thi:** Digital Interactive Environment Design Competition (Đại học Thanh Hoa x miHoYo)  
**Ngày cập nhật:** 11/10/2026  
**Nhánh:** `dev`  
**Scene đang chơi:** `Assets/Scenes/Level1.unity`  
**Unity Version:** 6000.3.25f1  
**Hạn nộp vòng sơ thẩm:** Trước 20:00 ngày 11/10/2026 (Hạn hệ thống 24:00)

---

## 1. Tổng quan kết quả thực hiện

Level 1 đã được **hoàn thiện 100% gameplay từ đầu đến cuối**, đáp ứng đầy đủ tất cả các tiêu chí trong mục 9 của bản yêu cầu (`PROMPT_HOAN_THIEN_GAME.md`) trong cùng một lượt Play liên tục từ lúc nhấn Play:

- **Intro mở đầu:** 4 câu dẫn truyện song ngữ/Việt, click chuột để chuyển nhanh từng câu, hiệu ứng fade mượt vào ngôi nhà có đầy đủ texture của model `GỜ ÊM GÊM.fbx`. Bệ xoay màu vàng xoay đồng trục cùng ngôi nhà, camera isometric (pitch 32°, yaw 45°) đứng yên cố định.
- **Vòng 1 (A → B):** Nhân vật di chuyển trên các bậc thang có thật của `CauThang1`, chạy đúng animation đi bộ (`Walking.fbx`), không xuyên sàn, không trôi nổi.
- **B ngõ cụt & Khớp góc hành lang 315°:** Điểm B là ngõ cụt khi ở các góc xoay khác. Đã sửa triệt để lỗi liên kết 90° cũ bị serialize trong scene; ở góc `CorridorYaw = 315°` (dung sai 12°), hai mép sàn B và C trùng khít trên góc nhìn camera isometric, nối thông đường đi vào phòng tầng 2. Có ảnh chứng minh `corridor_B_yaw0.png` và `corridor_B_yaw315.png`.
- **Phòng tầng 2 & Hệ thống Cutaway:** Chuyển vào phòng lập tức, không dùng màn hình đen loading. Hệ thống cutaway tự động ẩn các kết cấu tầng trên (từ cao độ trần y ≥ 7.0) và tự động xoay nhẹ ngôi nhà về góc `RoomViewYaw = 280°` để người chơi nhìn rõ tủ chè (TuBuffet), bàn ghế và nhân vật bên trong.
- **Câu đố Tủ Chè (Cabinet View):** 
  - Click vào tủ chè mở giao diện câu đố tương tác 3D.
  - Khi chưa xoay đúng: băng cassette ẩn hoàn toàn (`IsTapeVisible == false`).
  - Hỗ trợ hủy câu đố bằng chuột phải hoặc phím Escape: trả góc nhìn về bình thường mà không lấy đồ.
  - Lời giải chính xác `(1, 3)` (khối A xoay 1 lần, khối B xoay 3 lần): băng cassette xuất hiện giữa khe hở.
  - Click lấy băng: đóng giao diện câu đố, gắn trực tiếp cuộn băng cassette của Bà lên tay nhân vật (`mixamorig6:RightHand`), nhân vật tự động chuyển sang animation cầm đồ (`Carry.fbx`).
- **Vòng 2 & Cơ chế bẫy Chó:**
  - Nhân vật cầm băng bước ra cửa (`Door`), các tầng trên được khôi phục nguyên vẹn, camera trở về tầm nhìn ban đầu, dải ribbon xanh vòng 2 dẫn đường.
  - Con chó được bố trí tuần tra trên `CauThang2` (bám sát chú thích "con chó đi lại" trong tài liệu docx gốc).
  - Khi người chơi cố đi lên cầu thang ở góc xoay chưa an toàn (khác 250°), chó chạy vụt xuống chặn đường, nhân vật giật mình ngã (`Falling.fbx`), rơi về lại điểm xuất phát A. Góc xoay nhà được giữ nguyên, cuộn băng cassette vẫn cầm chắc trên tay. Có ảnh chứng minh `dog_fail_at_A.png`.
  - Khi xoay nhà về đúng góc an toàn `StairYaw = 250°`: bẫy chó ngừng hoạt động, lộ trình an toàn mở ra xuyên suốt từ A qua B, qua sảnh đón (Hall) và leo hết các bậc thang của `CauThang2` lên chiếu nghỉ E trên tầng 3 mà không kích hoạt chó.
- **Chuỗi node leo mái E → F → I → L → G → M:**
  - Chuỗi node hoàn thành tuần tự và chuẩn xác, mỗi điểm đều yêu cầu một góc xoay riêng đã được đo đạc kỹ lưỡng qua probe tầm nhìn, đảm bảo nhân vật luôn đứng vững trên mặt sàn/kết cấu thực tế:
    + **E (250°):** Chiếu nghỉ đầu cầu thang tầng 3.
    + **F (200°):** Dải hành lang phía đông tầng 3.
    + **I (160°):** Đầu hồi ban công phía đông.
    + **L (245°):** Lối đi hành lang trung tâm.
    + **G (190°):** Sân thượng cánh tây.
    + **M (280°):** Đỉnh mái nhà cao nhất bên cạnh cột loa.
  - *Ghi chú trung thực về kiến trúc:* Đoạn LipE → LipW là bước chuyển nhịp bước chân qua khe nứt kiến trúc giữa khối trung tâm và cánh tây; đoạn từ G lên M tận dụng bờ tường bồn hoa, bệ bê tông và khối Cube.041 làm các bậc leo thực tế, phù hợp với tinh thần leo trèo của thể loại giải đố không gian.
- **Đoạn kết (Ending Sequence) & Cột Loa:**
  - Khi đến điểm M, nhân vật đứng cạnh cột phát thanh rooftop. Khóa điều khiển, cuộn băng cassette bay từ tay nhân vật gắn vào cột loa (`MountOnPole`).
  - Âm thanh radio stinger phát ra tiếng rè nhiễu sóng băng cassette cổ, tiếp nối bằng đoạn nhạc hiệu bản tin phát thanh Hà Nội thân thuộc.
  - Camera lùi dần ra toàn cảnh, hiển thị câu kết: *"Some things have disappeared from this home. But memories still know the way back."*
  - Màn hình mờ dần (fade out) và chuyển sang màn hình kết thúc (**End Screen**):
    - Dòng kết tinh tế cùng lời cảm ơn: *"Thank you for bringing her voice home."*
    - Hướng dẫn phím: *"Level 1 complete - Esc to quit"*.
- **Chất lượng kỹ thuật:**
  - **Console:** Tuyệt đối 0 lỗi (`0 errors`).
  - **Unit Tests:** Toàn bộ 12/12 Edit Mode tests thuộc 3 bộ kiểm thử (`CabinetBlocksTests`, `StairSpanTests`, `StoryRulesTests`) đều đạt 100% Passed.

---

## 2. Danh mục ảnh chụp kiểm chứng (Assets/Screenshots)

Toàn bộ các mốc gameplay quan trọng đã được chụp và lưu trữ trong thư mục `Assets/Screenshots/`:
1. `intro_line.png`: Dòng mở đầu và ngôi nhà với bệ xoay vàng.
2. `walk_stair_yaw280.png`: Nhân vật bước đi trên bậc thang CauThang1 ở góc nhìn 280°.
3. `corridor_B_yaw0.png`: Điểm B là ngõ cụt khi ở góc yaw 0°.
4. `corridor_B_yaw315.png`: Cạnh sàn B và C trùng khít ở góc yaw 315°, mở lối sang C.
5. `room_view_yaw280.png`: Không gian phòng tầng 2 với hệ thống cutaway ẩn tầng trên, thấy rõ tủ chè.
6. `cabinet_solved_1_3.png`: Câu đố tủ chè khi xoay đúng (1, 3), làm lộ ra cuộn băng cassette.
7. `carrying_tape.png`: Băng cassette cầm trên tay nhân vật, kích hoạt animation Carry.
8. `dog_fail_at_A.png`: Nhân vật sau khi đụng chó ngã về A, bảo lưu góc xoay và cuộn băng cassette.
9. `roof_E_yaw250.png`: Lộ trình an toàn lên điểm E ở góc yaw 250°.
10. `roof_F_yaw200.png`: Nhân vật tại điểm F ở góc yaw 200°.
11. `roof_I_yaw160.png`: Nhân vật tại điểm I ở góc yaw 160°.
12. `roof_L_yaw245.png`: Nhân vật tại điểm L ở góc yaw 245°.
13. `roof_G_yaw190.png`: Nhân vật tại điểm G ở góc yaw 190°.
14. `roof_M_yaw280.png`: Nhân vật tại điểm M trên mái ở góc yaw 280°.
15. `at_pole.png`: Nhân vật đứng cạnh cột loa trên nóc nhà.
16. `end_screen.png`: Màn hình End Screen hoàn chỉnh với thông điệp tri ân.

---

## 3. Các cam kết tuân thủ

- Không thay đổi model gốc `GỜ ÊM GÊM.fbx` và texture kèm theo.
- Không dùng NavMesh hoặc cầu tàng hình / bệ nhảy ảo.
- Góc camera IsoRig cố định chuẩn 32° pitch và 45° yaw.
- Góc xoay hành lang 315° và dung sai 12° được giữ chuẩn mực.
- Bảo tồn toàn vẹn gói plugin `com.coplaydev.coplay` và cấu hình Unity MCP phục vụ việc đánh giá, chấm điểm.
- Giữ sạch repo: Không commit các thư mục `Library/`, `Logs/`, `Temp/`, các file nén `.zip`/`.rar`.
