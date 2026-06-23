# Ghi chú các file test bảo mật trong thư mục security-tests

Thư mục `security-tests/` chứa toàn bộ công cụ kiểm tra bảo mật Docker cho dự án GymBro. Dưới đây là giải thích vai trò và nội dung của từng file.

---

## Danh sách file

| File | Vai trò | Dung lượng |
|------|---------|-----------|
| `run-docker-attack-tests.ps1` | Script PowerShell chạy các bài test tấn công | |
| `docker-attack-targets.json` | Danh sách image/container cần test | |
| `attack-cases.md` | Mô tả ngắn gọn từng kịch bản test | |
| `GIAI_THICH_TAN_CONG_DOCKER.md` | **Giải thích chi tiết** từng phương thức tấn công | ~620 dòng |
| `GIAI_THICH_PHUONG_THUC_TAN_CONG.md` | Bảng tổng hợp tất cả test case | ~120 dòng |
| `HUONG_DAN_CHAY_TEST.md` | Hướng dẫn chạy từng loại test | |
| `README.md` | Tổng quan thư mục security-tests | |

---

## Giải thích chi tiết từng file

### 1. `run-docker-attack-tests.ps1`

**Script chính** để chạy tất cả các bài kiểm tra bảo mật.

**Cấu trúc:**
- **Hàm helper** (dòng 41-168): `Invoke-External`, `Add-TestResult`, `Redact-Secrets`, `Escape-MarkdownCell`, `Test-DockerAvailable`, `Test-ImageExists`, `Test-ContainerExists`, `Invoke-ImageShell`, `Get-ImageSizeText`
- **TẤN CÔNG 1: `Test-StaticImage`** (dòng 170-296): Quét image tĩnh — 11 test case (IMG-00 đến IMG-10)
- **TẤN CÔNG 2: `Test-ContainerRuntime`** (dòng 300-384): Quét container đang chạy — 6 test case (RUN-00 đến RUN-06)
- **TẤN CÔNG 3: `Test-ComposeSecurity`** (dòng 388-468): Quét file docker-compose — 4 test case (CMP-00 đến CMP-04)
- **BÁO CÁO: `Write-ComparisonReport`** (dòng 470-550): Tạo file Markdown so sánh Single vs Multi
- **Main execution** (dòng 556-593): Gọi tất cả hàm tấn công

**Cách chạy:**
```powershell
cd security-tests
.\run-docker-attack-tests.ps1 -Profile all
.\run-docker-attack-tests.ps1 -Profile single
.\run-docker-attack-tests.ps1 -Profile multi
```

---

### 2. `docker-attack-targets.json`

File JSON định nghĩa danh sách image và container cần test cho mỗi profile (single/multi):

```json
{
  "targets": [
    { "name": "GymBro Web - baseline single-stage", "profile": "single", "image": "gymbro-web:single", "container": "gymbro_web_single" },
    { "name": "GymBro Web - current multi-stage",  "profile": "multi",  "image": "gymbro-web:multi",  "container": "gymbro_web_multi" },
    ...
  ]
}
```

Có tổng cộng **10 target** (5 single + 5 multi).

---

### 3. `attack-cases.md`

File mô tả ngắn gọn tất cả test case dưới dạng bảng:
- Nhóm kiểm tra image runtime (IMG-00 đến IMG-09)
- Nhóm kiểm tra container runtime (RUN-00 đến RUN-05)
- Nhóm kiểm tra Docker Compose (CMP-01 đến CMP-03)
- Cách đọc kết quả (PASS, FAIL, WARN, SKIP, INFO)

---

### 4. `GIAI_THICH_TAN_CONG_DOCKER.md` (⭐ QUAN TRỌNG NHẤT)

File giải thích **chi tiết** từng phương thức tấn công — dành cho báo cáo nghiên cứu khoa học.

**Cấu trúc (9 phần):**
1. Mô hình đe dọa (5 tình huống)
2. Tóm tắt 6 nhóm tấn công
3. NHÓM 1: Image content — Root user, SDK, Source code, Debug files, Build tools
4. NHÓM 2: Runtime privilege — Read-only FS, Capability drop, no-new-privileges
5. NHÓM 3: Secret leakage — Image history, Image ENV, Compose secrets
6. NHÓM 4: Network exposure — Over-published ports
7. NHÓM 5: Security hardening — HEALTHCHECK, Memory limits, Compose hardening
8. Bảng tổng hợp 20 phương thức tấn công + kết quả Single vs Multi
9. Đề xuất cải thiện + Kết quả tổng kết

Mỗi test case đều giải thích: **Nguyên lý tấn công**, **Cách test**, **Docker phòng vệ**, **Hiện trạng GymBro**.

---

### 5. `GIAI_THICH_PHUONG_THUC_TAN_CONG.md`

File bảng tổng hợp ngắn gọn — phù hợp để in hoặc đưa vào phụ lục báo cáo.

**Cấu trúc (6 phần):**
1. Tổng quan — 3 hàm tấn công chính
2. TẤN CÔNG 1: Test-StaticImage — 11 test case + kết quả Single/Multi
3. TẤN CÔNG 2: Test-ContainerRuntime — 6 test case + kết quả Single/Multi
4. TẤN CÔNG 3: Test-ComposeSecurity — 4 test case + kết quả Single/Multi
5. Bảng so sánh Single vs Multi (20 hàng)
6. Kết luận

---

### 6. `HUONG_DAN_CHAY_TEST.md`

Hướng dẫn từng bước chạy test:
- Build + Run Single-stage
- Build + Run Multi-stage
- Chạy script test cho từng profile
- Cách đọc kết quả

---

### 7. `README.md`

File tổng quan thư mục `security-tests/`, liệt kê tất cả file và cách chạy nhanh.

---

## Tổng kết: Nên đọc file nào?

| Bạn là... | Nên đọc file này |
|-----------|-----------------|
| **Người mới, muốn chạy test** | `HUONG_DAN_CHAY_TEST.md` |
| **Muốn hiểu test case là gì** | `attack-cases.md` |`GIAI_THICH_PHUONG_THUC_TAN_CONG.md` |
| **Muốn hiểu sâu từng phương thức tấn công** | `GIAI_THICH_TAN_CONG_DOCKER.md` |
| **Muốn viết báo cáo NCKH** | `GIAI_THICH_TAN_CONG_DOCKER.md` + chạy script lấy kết quả |
| **Muốn biết script chạy thế nào** | Đọc comment trong `run-docker-attack-tests.ps1` |

---

## Kết quả tổng kết hiện tại

| Chỉ số | Single-stage (baseline) | Multi-stage hardened |
|--------|:-----------------------:|:--------------------:|
| **PASS** | 3/20 | **13/20** |
| **WARN** | 10/20 | 6/20 |
| **FAIL** | 6/20 | 0/20 |
| **Kích thước image** | ~1.7 GB | ~350 MB |
| **Runtime hardening** | 0/5 | 4/5 |

Multi-stage hardened cải thiện đáng kể so với Single-stage baseline, chứng minh hiệu quả của multi-stage build + runtime hardening trong bảo mật Docker.