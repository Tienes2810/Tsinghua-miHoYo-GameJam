# BÁO CÁO TIẾN ĐỘ

**Dự án:** Architectural Perspective Puzzle Game (phong cách Monument Valley)  
**Cuộc thi:** Digital Interactive Environment Design Competition (Đại học Thanh Hoa x miHoYo)  
**Ngày báo cáo:** 10/10/2026  
**Nhánh:** `dev`  
**Mã nguồn đã có trên remote trước báo cáo này:** `9e84336` — Use the new house model and the cassette round.  
**Scene đang chơi:** `Assets/Scenes/Level1.unity`  
**Unity:** 6000.3.25f1  
**Hạn nộp vòng sơ thẩm:** trước 20:00 ngày 11/10/2026 (hạn hệ thống 24:00)

Báo cáo này mô tả đúng code và scene đang nằm trên nhánh `dev`. File `Ke_hoach_va_Phan_cong_Cong_viec_Du_an_Tsinghua_miHoYo.md` (06/10) vẫn giữ lại làm kế hoạch phân công. Phần kỹ thuật trong kế hoạch đó (camera orthographic, lật cầu thang 180°, scene `Dev2_Stage.unity` / `MainGame.unity`, dung sai ±2°) không còn là cách Level 1 đang chạy.

Gameplay đang bám bản mô tả mới ngày 10/10: vòng 1 đường đỏ vào phòng tầng 2 lấy băng cassette; vòng 2 đường xanh, tránh chó, đưa băng lên cột loa trên mái. Vật cầm tay là băng cassette. Điểm đặt vẫn là cột loa.

Chưa chơi hết một lượt từ đầu đến cuối trong Editor. Phần gắn model và gán clip đã kiểm tra trong Editor. Phần chơi thì chưa.

## Đã xong

### Hạ tầng

- Repo public: https://github.com/Tienes2810/Tsinghua-miHoYo-GameJam.git, nhánh làm việc là `dev`.
- Level 1 dùng tìm đường theo node (point-and-click, DOTween). Không dùng NavMesh.
- Camera isometric đứng yên. Kéo chuột xoay cả nhà và bệ vàng. Góc hành lang mở ở yaw 315°, dung sai 12°. Góc cầu thang vòng 2 ở yaw 200°, cùng dung sai.
- Hai bản mô tả gameplay cũ đã xóa ở commit `630992c`. Kế hoạch 06/10 và file link model được giữ.

### Nhà và nhân vật (commit 9e84336)

- Level 1 dùng `Assets/Art/Models/House/GỜ ÊM GÊM.fbx`, lấy từ bản model ngày 09/10. Texture nằm cạnh mesh, trong thư mục `.fbm`. Có 36 texture. Hai tên UDIM trong file FBX không có trong bản nén.
- Khi gắn nhà mới, tỉ lệ chiều cao so với nhà cũ là 1.00, nên các node đã đặt được giữ nguyên chỗ.
- Nhân vật trong scene đã gán bốn clip: đứng nghỉ (`StandingIdle.fbx`), đi (`Walking.fbx`), cầm đồ (`Carry.fbx`), ngã (`Falling.fbx`).

### Vòng 1

- Intro vài dòng, chữ mờ dần, click để bỏ qua.
- Đi từ A, qua cầu thang, tới B. B là ngõ cụt cho đến khi yaw nhà nằm trong dung sai của 315°. Lúc đó B nối sang C và nhân vật đi vào phòng tầng 2, không có màn cắt loading.
- Click tủ mở một góc nhìn chỉ có tủ, không có nền nhà. Hai khối xoay độc lập, mỗi lần 90°. Lời giải đang mã hóa: khối trái 1 lần, khối phải 3 lần. Chuột phải hoặc Escape đóng tủ và không lấy đồ.
- Khi giải đúng, băng cassette lấy được. Băng gắn vào điểm tay, chạy clip cầm đồ, không có túi đồ.

### Vòng 2 (phần đã viết trong code)

- Ra khỏi phòng theo đường xanh. Đụng chó thì nhân vật ngã, bị đưa về A, góc xoay nhà giữ nguyên, băng vẫn cầm trên tay.
- Thoại khi ngã: "Có lẽ con đường vừa rồi không dành cho chúng ta." Gợi ý xoay nhà hiện lại.
- Code chuỗi mái đã có: E → F → I → L → G → M. Góc yaw từng điểm: E 200°, F 250°, I 280°, L 40°, G 80°, M 155°.
- Đặt băng lên cột thì khóa di chuyển, camera lùi ra, hiện câu kết, rồi màn tối dần.

## Đang làm dở

- Con chó trong scene vẫn là khối capsule nâu, chưa phải model chó.
- Nhặt và đặt đồ dùng điểm `Hand` cộng clip cầm đồ. Chưa có clip cúi xuống hoặc giơ tay đặt băng.
- Kết thúc đang là một tiếng beep 440 Hz do code tạo ra, rồi fade tối. Chưa có tiếng vịt, tiếng nhiễu, bản tin buổi sáng Hà Nội cũ, và chưa có End Screen.
- Scene có các node A, A2, B, C, Stair, Door, Dog, Safe, F, I, L, G, M, Pole. Không có node tên E, cũng không có bậc thang trung gian. Vì vậy đoạn đi lên mái không bắt đầu được, dù code chuỗi E → M đã viết.
- Các góc yaw mái vẫn là bộ số cũ. Chưa đo lại trên mesh nhà mới.
- Vài câu thoại trong phòng vẫn nhắc chiếc loa, vì mục đó trong bản mô tả còn nguyên câu cũ. Vật người chơi cầm là băng cassette.

## Còn thiếu trước hạn 11/10

- Đặt node E và các bậc trên đúng sàn của nhà mới, rồi chơi thử cả hai vòng.
- Đo lại yaw mái trên mesh mới trước khi khóa số.
- Bản tin buổi sáng và màn End Screen.
- Model chó thay cho capsule.
- Hai texture UDIM còn thiếu, nên một số cây hoặc hình trên nhà có thể mất màu.
- `GOEMGEM.glb` vẫn nằm trong repo vì scene xem trước còn tham chiếu. Level 1 không còn dùng file này.
- Theo lịch nước rút ngày 10/10: build `.exe` / WebGL, video demo 1–2 phút, và PDF thuyết minh ý tưởng kiến trúc. Ba mục này chưa làm.
- Một lượt chơi đầy đủ từ intro đến màn kết. Chưa làm.

## Cách kéo về

```
git lfs install
git clone https://github.com/Tienes2810/Tsinghua-miHoYo-GameJam.git
cd Tsinghua-miHoYo-GameJam
git checkout dev
git pull
git lfs pull
```

Mở Unity 6000.3.25f1, scene `Assets/Scenes/Level1.unity`. Lần `git lfs pull` tải model nhà, texture và bốn clip nhân vật. Dung lượng LFS lớn.

## Việc không đưa lên git

Giữ ngoài commit: `House_Full.fbx`, `House_Partial.fbx`, `Blockout/test`, `Screenshots`, `Temp`, `_Recovery`, và các bản mesh hoặc texture trùng ở thư mục gốc repo. Không bake lại nhà đang chơi thành `House_Gameplay`. Bản bake cũ làm mất mặt cắt đọc được của ngôi nhà.
