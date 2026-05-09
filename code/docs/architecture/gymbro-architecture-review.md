# GymBro Architecture Review

## 1. Kết luận ngắn

GymBro **đã có kiến trúc ở mức tổ chức solution**:

- `GymBro.Core`
- `GymBro.Infrastructure`
- `GymBro.API`
- `GymBro.Web`

Nhưng hiện tại đây mới là **layered monolith theo tên project**, chưa phải kiến trúc sạch theo dependency và use case. Vì vậy:

- Dễ bắt đầu làm nhanh.
- Khó giữ ổn định khi thêm nhiều tính năng.
- Chi phí bảo trì sẽ tăng mạnh ở các flow như `Cart`, `Order`, `Payment`, `Inventory`.

Đánh giá thực tế:

- Mức độ "đã có kiến trúc": **Có, nhưng mới ở mức khung**
- Mức độ dễ quản lý lâu dài: **Trung bình thấp**
- Mức độ dễ nâng cấp tương lai: **Thấp nếu giữ nguyên cách tổ chức hiện tại**

## 2. Điểm tốt hiện tại

Project hiện tại vẫn có vài nền tốt để phát triển tiếp:

- Đã tách riêng `Web` và `API` thành hai entry point.
- Đã có `Infrastructure` chứa `DbContext` và migrations.
- Đã bắt đầu gom file theo `Features/`.
- Một số nghiệp vụ đã được nhận diện thành service riêng như payment/inventory workflow.

Điều này có nghĩa là repo **không cần rewrite toàn bộ**, mà có thể refactor dần sang kiến trúc tốt hơn.

## 3. Vì sao kiến trúc hiện tại khó quản lý và khó nâng cấp

### 3.1 Domain chưa độc lập

`GymBro.Core` đang tham chiếu EF Core và BCrypt:

- `GymBro.Core/GymBro.Core.csproj`

Điều này làm `Core` không còn là domain thuần. Khi domain biết quá nhiều về ORM và kỹ thuật lưu trữ, việc đổi persistence, test business rule, hoặc tái sử dụng domain sẽ khó hơn.

Ngoài ra entity đang gắn trực tiếp annotation của database như:

- `[Table]`, `[Key]`, `[ForeignKey]`, `[Column]`

Điều này khiến domain phụ thuộc mạnh vào EF mapping.

### 3.2 Controller đang chứa cả orchestration + business rule + data access

Nhiều controller thao tác trực tiếp với:

- `GymBroDbContext`
- `Include(...)`
- `SaveChangesAsync()`
- transaction
- session
- file system

Ví dụ rõ nhất:

- `GymBro.Web/Features/Cart/CartController.cs`
- `GymBro.Web/Features/Products/ProductsController.cs`
- `GymBro.Web/Features/Orders/OrdersController.cs`
- `GymBro.API/Features/Auth/AuthController.cs`

Hệ quả:

- Rule bị rải ra nhiều nơi.
- Rất khó test độc lập.
- Khi đổi business flow, dễ sót chỗ.
- Web và API khó dùng chung nghiệp vụ.

### 3.3 View đang truy cập database trực tiếp

Hai layout đang inject `GymBroDbContext`:

- `GymBro.Web/Views/Shared/_Layout.cshtml`
- `GymBro.Web/Views/Shared/_LayoutAdmin.cshtml`

Đây là dấu hiệu presentation layer đang đi xuyên qua boundary. Nếu sau này thay đổi cách lấy dữ liệu menu, user summary, wishlist count hoặc caching, chi phí sửa sẽ lan rộng.

### 3.4 Service chưa đi qua DI và contract rõ ràng

Một số service đang được khởi tạo bằng `new` ngay trong controller:

- `OrderInventoryService`
- `AdminPaymentAuditService`
- `AdminPaymentWorkflowService`

Khi service được tạo thủ công:

- Khó mock/test.
- Khó thay implementation.
- Khó quản lý transaction boundary.
- Controller biết quá nhiều về wiring nội bộ.

### 3.5 Chưa có Application layer/use-case layer

Hiện tại solution thiếu một tầng trung gian kiểu:

- `Application`
- `UseCases`
- `Services`
- `Handlers`

Đây là lý do lớn nhất khiến logic bị dồn vào controller. Khi không có application layer, mọi flow đều bị kéo lên presentation hoặc đẩy thẳng xuống infrastructure.

### 3.6 Auth và session đang gắn chặt vào UI flow

Ví dụ:

- `BaseAdminController` dùng session để chặn quyền admin.
- `CartController`, `AccountController`, `WishlistController` dùng session object trực tiếp.

Điều này làm:

- Quyền truy cập không đi qua policy rõ ràng.
- API và Web không thống nhất auth model.
- Sau này thêm mobile app hoặc SPA sẽ khó mở rộng.

### 3.7 Chưa có test architecture và test use case

Hiện tại mình chưa thấy test project trong solution. Đây là rủi ro lớn vì refactor kiến trúc mà không có test sẽ rất dễ làm vỡ flow checkout, order, payment, inventory.

### 3.8 Namespace và feature boundary chưa đồng nhất

File được đặt trong `Features/...` nhưng nhiều namespace vẫn là:

- `GymBro.Web.Controllers`
- `GymBro.API.Controllers`

Điều này khiến feature folder mới chỉ là tổ chức file, chưa phải module thực sự.

### 3.9 Có project/phần không thuộc core business chính

Thư mục `Producing/` đang nằm cùng repo nhưng không thuộc luồng GymBro chính. Nếu giữ lâu dài trong cùng workspace mà không tách rõ, nó sẽ làm solution khó đọc và khó giữ kỷ luật kiến trúc.

## 4. Kiến trúc mục tiêu đề xuất

Mình khuyến nghị chọn:

## Modular Monolith + Clean Boundaries + Feature Slices

Lý do chọn mô hình này:

- Phù hợp quy mô hiện tại.
- Dễ quản lý hơn microservices.
- Tách rõ business/use case/persistence.
- Cho phép refactor dần từng module.
- Sau này nếu cần tách service, có thể tách theo module mà không phải làm lại từ đầu.

## 5. Nguyên tắc dependency bắt buộc

Luật phụ thuộc nên là:

- `GymBro.Web` -> `GymBro.Application`
- `GymBro.API` -> `GymBro.Application`
- `GymBro.Infrastructure` -> `GymBro.Application` + `GymBro.Domain`
- `GymBro.Application` -> `GymBro.Domain`
- `GymBro.Domain` -> không phụ thuộc project nào khác trong solution

Không cho phép:

- `Web` gọi trực tiếp `DbContext` trong action/view
- `API` viết business rule trực tiếp trong controller
- `Domain` tham chiếu EF/BCrypt/JWT
- `View` truy cập database

## 6. Cấu trúc solution nên chuyển tới

```text
src/
  GymBro.Domain/
    Catalog/
    Identity/
    Orders/
    Payments/
    Inventory/
    Purchasing/
    Reviews/
    Wishlist/
    Shared/

  GymBro.Application/
    Abstractions/
      Persistence/
      Security/
      Files/
      Time/
      Messaging/
    Catalog/
      Commands/
      Queries/
      DTOs/
      Validators/
    Identity/
    Orders/
    Payments/
    Inventory/
    Purchasing/
    Reviews/
    Wishlist/

  GymBro.Infrastructure/
    Persistence/
      GymBroDbContext.cs
      Configurations/
      Repositories/
      Migrations/
    Security/
      BCryptPasswordHasher.cs
      JwtTokenService.cs
    Files/
      LocalImageStorage.cs
    Payments/
      VietQrPaymentGateway.cs
    DependencyInjection.cs

  GymBro.Web/
    Features/
      Account/
      Catalog/
      Cart/
      Orders/
      Admin/
      Shared/
    ViewModels/
    DependencyInjection/
    Program.cs

  GymBro.API/
    Features/
      Auth/
      Products/
      Cart/
      Orders/
    Contracts/
    DependencyInjection/
    Program.cs

tests/
  GymBro.ArchitectureTests/
  GymBro.Application.UnitTests/
  GymBro.IntegrationTests/
```

## 7. Trách nhiệm của từng tầng

### 7.1 `GymBro.Domain`

Chỉ chứa:

- Entity
- Value Object
- Enum
- Domain rule
- Domain service thuần business
- Domain event nếu cần

Không chứa:

- EF Core
- BCrypt
- JWT
- `HttpContext`
- `Session`
- `IFormFile`
- `DbContext`

Ví dụ rule nên nằm ở domain/application thay vì controller:

- Một đơn hàng có thể chuyển từ trạng thái nào sang trạng thái nào
- Hủy đơn thì hoàn kho theo điều kiện gì
- Payment nào được coi là final
- Product có hợp lệ để bán không

### 7.2 `GymBro.Application`

Đây là tầng quan trọng nhất để repo dễ nâng cấp.

Chứa:

- Use case
- Command/Query
- Handler
- DTO
- Validator
- Interface/Port

Ví dụ interface:

- `IProductRepository`
- `IOrderRepository`
- `IPaymentRepository`
- `IUnitOfWork`
- `ICurrentUser`
- `IPasswordHasher`
- `ITokenService`
- `IImageStorage`
- `IDateTimeProvider`

Ví dụ use case:

- `RegisterUser`
- `LoginUser`
- `CreateProduct`
- `UpdateProduct`
- `PlaceOrder`
- `ConfirmPayment`
- `CancelExpiredOrder`
- `AdjustInventory`
- `ApprovePurchaseOrder`

Controller chỉ còn làm:

- nhận request
- gọi use case
- trả response/view

### 7.3 `GymBro.Infrastructure`

Chứa toàn bộ phần thay đổi theo công nghệ:

- EF Core mapping
- repository implementation
- JWT token implementation
- password hashing implementation
- file/image storage implementation
- QR payment adapter
- migrations

Nếu sau này đổi:

- SQL Server -> PostgreSQL
- local file -> cloud storage
- VietQR -> cổng thanh toán khác

thì đa số thay đổi sẽ nằm ở tầng này.

### 7.4 `GymBro.Web` và `GymBro.API`

Chỉ là adapter/presentation layer.

`Web`:

- Controller MVC
- ViewModel
- Razor View
- mapping request/view model sang command/query

`API`:

- HTTP contract
- request/response DTO
- auth endpoint
- serialization

Hai project này không nên giữ business rule lõi.

## 8. Module business nên chia như sau

Để dễ quản lý lâu dài, mình đề xuất chia theo domain thay vì chỉ theo controller:

### 8.1 Catalog

- Product
- Category
- Product image
- Search/filter

### 8.2 Identity

- Register
- Login
- Admin authorization
- Current user

### 8.3 Cart & Checkout

- Session cart hoặc cart store
- Validate stock trước checkout
- Tạo order từ cart

### 8.4 Ordering

- Order aggregate
- Order status transition
- Order history
- Cancel/restore

### 8.5 Payments

- Payment method
- Payment state machine
- Customer confirm payment
- Admin verify payment
- Audit log

### 8.6 Inventory

- Reserve stock
- Release stock
- Manual adjustment
- Inventory transaction history

### 8.7 Purchasing

- Supplier
- Purchase order
- Approve receiving

### 8.8 Reviews & Wishlist

- Review
- Wishlist

## 9. Flow chuẩn sau khi refactor

Ví dụ luồng `PlaceOrder`:

1. `CartController` nhận request.
2. Controller map sang `PlaceOrderCommand`.
3. `PlaceOrderHandler` xử lý toàn bộ use case.
4. Handler gọi:
   - `IProductRepository`
   - `IOrderRepository`
   - `IPaymentRepository`
   - `IInventoryService`
   - `IUnitOfWork`
5. `Infrastructure` hiện thực các interface trên bằng EF Core.
6. Controller chỉ nhận kết quả rồi redirect/view.

Như vậy:

- Web không biết transaction chạy thế nào.
- API có thể tái dùng cùng use case.
- Test nghiệp vụ không cần chạy full MVC.

## 10. Những thay đổi cụ thể nên làm đầu tiên

### Ưu tiên 1: Tạo `GymBro.Application`

Đây là bước quan trọng nhất. Chưa có tầng này thì mọi refactor khác sẽ chỉ là dọn file.

Refactor đầu tiên nên chuyển các flow có rủi ro cao vào application:

- `Auth`
- `PlaceOrder`
- `ConfirmPayment`
- `CancelExpiredOrder`
- `UpdateOrderStatus`
- `Create/Update/DeleteProduct`

### Ưu tiên 2: Làm sạch `Domain`

Tách khỏi `GymBro.Core`:

- EF annotations
- BCrypt dependency
- bất kỳ logic hạ tầng nào

Đổi hướng:

- Entity thuần business
- Mapping EF chuyển sang `Infrastructure/Persistence/Configurations`

### Ưu tiên 3: Chuyển service hiện tại sang interface + DI

Các service như:

- `OrderInventoryService`
- `AdminPaymentAuditService`
- `AdminPaymentWorkflowService`

nên được:

- đưa về `Application` nếu là use case/business orchestration
- hoặc `Infrastructure` nếu là adapter
- đăng ký DI
- inject qua constructor

### Ưu tiên 4: Cấm `DbContext` trong view

Thay bằng:

- `LayoutViewModel`
- `NavigationViewComponent`
- `HeaderSummaryQuery`

### Ưu tiên 5: Chuẩn hóa auth

Web có thể vẫn giữ session trong giai đoạn đầu, nhưng nên bọc qua abstraction:

- `ICurrentUser`
- `IAdminAuthorizationService`

Sau đó tiến tới:

- cookie auth/policy cho MVC
- JWT/policy cho API

### Ưu tiên 6: Thêm test

Tối thiểu cần:

- Unit test cho `PlaceOrder`, `UpdateOrderStatus`, `ConfirmPayment`
- Integration test cho checkout và payment flow
- Architecture test để chặn:
  - `Web` reference `Infrastructure` ngoài composition root
  - `Domain` reference EF

## 11. Lộ trình refactor an toàn

Không nên rewrite toàn bộ một lần. Lộ trình an toàn:

### Phase 1

- Thêm `GymBro.Application`
- Định nghĩa interfaces/ports
- Chuyển 1-2 use case quan trọng đầu tiên

### Phase 2

- Tách domain khỏi EF attributes
- Chuyển EF mapping sang `Infrastructure`

### Phase 3

- Chuyển toàn bộ order/payment/inventory sang application layer
- Chuẩn hóa transaction boundary

### Phase 4

- Chuyển product/catalog và auth
- Xóa dần business logic khỏi controller

### Phase 5

- Thêm tests
- Thêm architecture rules
- Chuẩn hóa namespace/module naming

## 12. Quy ước code nên áp dụng sau khi chuyển kiến trúc

### Controller

- Không query trực tiếp `DbContext`
- Không gọi `SaveChangesAsync()`
- Không mở transaction
- Không chứa business rule dài

### Application

- Mỗi use case là một đơn vị độc lập
- Rule nghiệp vụ ở đây hoặc domain
- Trả result rõ ràng, không phụ thuộc MVC

### Infrastructure

- Chỉ chứa implementation chi tiết
- Không chứa policy nghiệp vụ lõi

### Domain

- Không phụ thuộc framework
- Tên class phản ánh business

## 13. Đề xuất thực tế cho repo này

Với quy mô hiện tại, mình **không khuyên tách microservice**.

Giải pháp phù hợp nhất là:

**Một modular monolith với application layer rõ ràng, domain sạch, infrastructure tách adapter, Web/API mỏng.**

Đây là điểm cân bằng tốt nhất giữa:

- dễ quản lý
- dễ học
- dễ test
- dễ nâng cấp
- không quá nặng về vận hành

## 14. Ưu tiên refactor module nào trước

Thứ tự nên là:

1. `Orders + Payments + Inventory`
2. `Cart + Checkout`
3. `Auth + Users`
4. `Products + Categories`
5. `Purchasing`
6. `Reviews + Wishlist`

Lý do:

- Đây là các module đang gắn chặt với transaction và business rule nhất.
- Nếu không tách sớm, mọi thay đổi sau này sẽ tiếp tục đổ vào controller.

## 15. Một số quyết định kiến trúc nên chốt ngay

### Quyết định 1

Giữ monolith, không tách microservice.

### Quyết định 2

Thêm `Application` layer trước mọi refactor lớn.

### Quyết định 3

`Domain` phải thuần, không tham chiếu EF/BCrypt.

### Quyết định 4

Không cho `View` và `Controller` chạm trực tiếp vào persistence details ngoài composition root.

### Quyết định 5

Tất cả flow nghiệp vụ chính phải có test trước khi refactor sâu.

## 16. Kết luận cuối

GymBro hiện tại **có kiến trúc sơ khai**, nhưng chưa đủ sạch để được xem là dễ bảo trì và dễ nâng cấp lâu dài.

Nếu tiếp tục phát triển theo cách hiện tại:

- controller sẽ ngày càng phình to
- business rule sẽ tiếp tục bị trùng và rải rác
- Web/API sẽ khó tái sử dụng logic chung
- rủi ro vỡ flow khi nâng cấp sẽ cao

Nếu chuyển sang kiến trúc đề xuất trong tài liệu này:

- quản lý code sẽ đơn giản hơn
- thêm tính năng mới dễ hơn
- dễ thay đổi hạ tầng hơn
- dễ test và dễ onboarding hơn
- có đường nâng cấp dài hạn rõ ràng mà chưa cần tách microservice
