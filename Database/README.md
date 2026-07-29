# Quy Trình Khởi Tạo Cơ Sở Dữ Liệu (Database Initialization Guide)

Hệ thống backend chính của `TMPMS` sử dụng **Microsoft SQL Server** thông qua Entity Framework Core. Tất cả cấu trúc bảng được kiểm soát bằng EF Migrations.

Hãy làm theo các bước sau để thiết lập cơ sở dữ liệu từ đầu:

### Bước 1: Tạo Schema (Tạo Cấu Trúc Bảng)
Khởi chạy lệnh sau tại thư mục dự án `TMPMS_BE` để tự động tạo toàn bộ bảng và mối quan hệ trong cơ sở dữ liệu SQL Server:
```bash
dotnet ef database update
```

### Bước 2: Chèn Dữ Liệu Mẫu (Seed Data)
Sau khi schema được thiết lập thành công, hãy chạy tệp dữ liệu hạt giống để điền các danh mục, nhà cung cấp, sản phẩm thuốc đông y & thảo dược, cũng như các mã giảm giá mặc định vào SQL Server:
```bash
# Chạy tệp tin seed.sql
# Bạn có thể thực hiện chạy tệp này thông qua SQL Server Management Studio (SSMS), Azure Data Studio, 
# hoặc các công cụ dòng lệnh khác kết nối với database của bạn.
Database/seed.sql
```

---

> [!NOTE]
> Thư mục `archive/` chứa các tệp tin SQL cũ (bao gồm cả các bản nháp PostgreSQL và các lệnh PostgREST cũ). Chúng chỉ được giữ lại để làm tài liệu đối chiếu và **không được dùng** trong quá trình vận hành hệ backend hiện tại.
