# **KẾ HOẠCH PHÁT TRIỂN & PHÂN CÔNG CÔNG VIỆC**

---

**Dự án:** Architectural Perspective Puzzle Game (Phong cách Monument Valley)  
**Cuộc thi:** Digital Interactive Environment Design Competition (Đại học Thanh Hoa x miHoYo)  
**Hạn nộp vòng sơ thẩm:** 24:00 ngày 11/10/2026

## ---

**1\. TỔNG QUAN GAMEPLAY & LOGIC 2 VÒNG**

Game áp dụng góc nhìn **Orthographic Isometric** kết hợp hệ thống **ode Pathfinding** (không dùng NavMesh) để tạo cơ chế ảo thị (Perspective Illusion).

**N**ROUND 1: Tìm đồ trong phòng chính

> * Nhân vật xuất phát từ tầng 1, đi theo đường màu đỏ hướng lên cầu thang.  
> * Tại đầu cầu thang, đường đi bị **đứt đoạn** so với ban công dẫn vào phòng chính.  
> * **Cơ chế giải đố:** Người chơi xoay góc nhìn toàn bộ tòa nhà quanh trục Y. Khi góc quay đạt giá trị căn chỉnh chuẩn, hai mảng đường đứt sẽ tạo cảm giác thị giác thẳng hàng & liền mạch. Hệ thống tự động nối Node đầu cầu thang với Node cửa phòng.  
> * Nhân vật đi vào phòng chính và nhặt kỷ vật/vật phẩm nhiệm vụ.

### **ROUND 2: Thoát hiểm lên ban công tầng 3**

> * Nhân vật đi ngược ra theo đường màu xanh dương để quay lại lối cầu thang.  
> * Xuất hiện **vật cản con chó dữ** tuần tra / chắn đường tại vị trí cầu thang.  
> * **Cơ chế giải đố:** Người chơi tương tác click vào cầu thang để **lật xoay 180 độ** (Stair Flip). Cầu thang chuyển sang mặt sạch không có con chó, đồng thời nối liền đường dẫn lên ban công tầng 3\.  
> * Nhân vật bước lên tầng 3 và ra ngoài ban công hóng gió (Hoàn thành màn chơi).

## ---

**2\. NGUYÊN TẮC LÀM VIỆC & PHÒNG TRÁNH CONFLICT GIT**

**QUY TẮC BẮT BUỘC:**

1. **Tuyệt đối không làm việc chung trên 1 Scene:** Dev 2 làm việc trên Scene Assets/Scenes/Dev2\_Stage.unity. Dev 1 quản lý Scene tổng hợp MainGame.unity.  
2. **Làm việc 100% bằng Prefab:** Tất cả đối tượng (nhà, nhân vật, cầu thang, chướng ngại vật) phải được chuyển thành Prefab và lưu tại Assets/Prefabs/ trước khi đưa vào Scene.  
3. **Thống nhất phiên bản Unity:** Hai người dùng chính xác cùng phiên bản Unity để tránh crash shader và metadata.

## ---

**3\. BẢNG PHÂN CHIA NHIỆM VỤ CHI TIẾT**

| Thành viên | Hạng mục phụ trách | Yêu cầu đầu ra & Tiêu chuẩn kỹ thuật   |
| :---- | :---- | :---- |
| **DEV 2** *(Thực hiện Ban ngày)* | **1\. Tách Prefab từ Asset FBX:** \- Khung nhà chính (Building\_Base) \- Con chó (Obstacle\_Dog) \- Đồ vật nhặt (Item\_Quest) \- Cầu thang xoay (Stair\_Flippable) \- Nhân vật (Player) **2\. Setup Camera & Visual:** \- Orthographic Camera \- Test góc nối đường **3\. Setup Character & Script tương tác:** \- Animator Walk / Idle \- Script xoay map & lật cầu thang | **CỰC KỲ QUAN TRỌNG:** Stair\_Flippable phải được đặt lại tâm **Pivot nằm chính giữa khối cầu thang** để khi xoay 180 độ không bị văng lệch vị trí. Camera để chế độ **Orthographic**, góc nghiêng Isometric (Rotation: X \= 30° hoặc 35.26°, Y \= 45°). Thử nghiệm xoay tòa nhà và ghi lại chính xác **góc Y** mà 2 đoạn đường đứt nhìn thẳng hàng nhau (để Dev 1 nạp thông số vào script nối node). Nhân vật Ch09\_nonPBR cấu hình Rig Humanoid, Animator Controller có bool isMoving chuyển trạng thái Walk/Idle. Đẩy code/prefab lên branch dev2-stage trước 18:00. |
| **DEV 1 (Lead)** *(Setup sáng \+ Code tối)* | **1\. Quản trị hạ tầng dự án:** \- Setup GitHub Repo \+ Git LFS \- Cài đặt DOTween & MCP Tooling **2\. Lập trình Core Logic (Tối nay):** \- PathNode.cs & PlayerMovement.cs \- PerspectiveConnector.cs \- StairFlipper.cs \- GameFlowManager.cs | Track Git LFS cho file \*.fbx nặng \~80MB, cấu hình .gitignore chuẩn Unity. Xây dựng hệ thống tìm đường theo Node (Point & Click), hỗ trợ animation di chuyển mượt mà qua DOTween. Logic Perspective: Tự động phát hiện khi tòa nhà xoay về góc chuẩn (tolerance ±2°) để nối 2 node bị đứt; ngắt kết nối khi xoay lệch. Quản lý State Machine: Round 1 (Tìm đồ) → Round 2 (Né chó lật cầu thang) → Kết thúc (Ban công). Tích hợp Prefab của Dev 2 vào Scene chính MainGame.unity. |

## ---

**4\. KẾ HOẠCH NƯỚC RÚT VÒNG SƠ THẨM (06/10 \- 11/10)**

> * **06/10 (Hôm nay):** Hoàn thành khung prefab, hệ thống Node di chuyển và các script tương tác cơ bản.  
> * **07/10:** Ghép hoàn chỉnh luồng chơi Round 1 (Xoay map nối đường & vào phòng nhặt đồ).  
> * **08/10:** Ghép hoàn chỉnh luồng chơi Round 2 (Lật cầu thang, tránh né con chó, lên ban công tầng 3).  
> * **09/10:** Đánh bóng sản phẩm: Thêm âm thanh (SFX bước chân, tiếng xoay đá/gỗ, tiếng gió ban công), ánh sáng và camera effect.  
> * **10/10:** Test build file cài đặt (.exe / WebGL), quay video demo gameplay chất lượng cao (1 \- 2 phút) và viết bản thuyết minh ý tưởng kiến trúc (PDF Concept).  
> * **11/10:** Kiểm tra lần cuối và nộp bài trước 20:00 (Hạn chót hệ thống: 24:00).