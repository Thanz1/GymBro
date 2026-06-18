# Docker Multi-stage Attack Test Report

- Time: 2026-06-18 19:18:33
- Profile: research
- Compose file: C:\Users\HUAN-PC\source\repos\GymBro\security-tests\..\docker-compose.yml

## Summary

| Status | Count |
| --- | ---: |
| FAIL | 10 |
| INFO | 12 |
| PASS | 54 |
| SKIP | 2 |
| WARN | 15 |

## Details

| Target | Case | Severity | Status | Evidence | Recommendation |
| --- | --- | --- | --- | --- | --- |
| docker-compose.yml | CMP-01 no hard-coded secrets | High | FAIL | line 9: - MSSQL_SA_PASSWORD=*** \| line 25: RABBITMQ_DEFAULT_PASS: *** \| line 41: - ConnectionStrings__DefaultConnection=Server=db;Database=GymBro_Identity;User Id=sa;Password=***;TrustServerCertificate=True;MultipleActiveResultSets=true \| line 60: - ConnectionStrings__DefaultConnection=Server=db;Database=GymBro_Product;User Id=sa;Password=***;TrustServerCertificate=True;MultipleActiveResultSets=true \| line 80: - ConnectionStrings__DefaultConnection=Server=db;Database=GymBro_Order;User Id=sa;Password=***;TrustServerCertificate=True;MultipleActiveResultSets=true | Move passwords/JWT/SMTP secrets to .env, Docker secrets, or a secret manager. |
| docker-compose.yml | CMP-02 not Development for secure deploy | Medium | WARN | Compose declares ASPNETCORE_ENVIRONMENT=Development | Use Production when evaluating deployment security. |
| docker-compose.yml | CMP-03 internal ports are not over-published | Medium | WARN | 1433:1433, 5672:5672, 15672:15672, 7001:8080, 7002:8080, 7003:8080 | Production should expose only Web/Gateway; keep APIs, SQL Server, and RabbitMQ internal when possible. |
| GymBro API - secure multi-stage research image | IMG-00 image size | Info | INFO | gymbro/api:secure-multistage = 102.1 MB | Use this value to compare single-stage and secure multi-stage images. |
| GymBro API - secure multi-stage research image | IMG-01 runtime user is non-root | High | PASS | uid=1000 user=gymapi | Least privilege is enabled at image level. |
| GymBro API - secure multi-stage research image | IMG-02 no .NET SDK in runtime | High | PASS | dotnet --list-sdks returned no SDK | This proves the final image does not carry the SDK. |
| GymBro API - secure multi-stage research image | IMG-03 no source/build files leaked | High | PASS | No .cs/.csproj/.sln files found under /app or /src | Only published artifacts are copied to final image. |
| GymBro API - secure multi-stage research image | IMG-04 no debug/development files | Medium | PASS | No .pdb or appsettings.Development.json found under /app | Keep DebugSymbols=false and remove development config from final image. |
| GymBro API - secure multi-stage research image | IMG-05 no common build tools | Medium | PASS | dotnet-ef/gcc/make/git were not found | Runtime image stays minimal. |
| GymBro API - secure multi-stage research image | IMG-06 simulated write to /app | Medium | WARN | WRITABLE | Use read_only: true in Compose and mount only writable folders. |
| GymBro API - secure multi-stage research image | IMG-07 no secrets in image history | High | PASS | No secret pattern found in docker history | Keep secrets out of image layers. |
| GymBro API - secure multi-stage research image | IMG-08 no secrets in image env | High | PASS | No secret pattern found in image env | Pass secrets at runtime through env/secrets manager. |
| GymBro API - secure multi-stage research image | RUN-01 container user is non-root | High | PASS | Config.User='gymbro' | Runtime least privilege is enabled. |
| GymBro API - secure multi-stage research image | RUN-02 root filesystem is read-only | Medium | PASS | ReadonlyRootfs=true | Mount volumes only for writable folders. |
| GymBro API - secure multi-stage research image | RUN-03 Linux capabilities dropped | Medium | PASS | ["ALL"] | cap_drop: ALL reduces unnecessary kernel privileges. |
| GymBro API - secure multi-stage research image | RUN-04 no-new-privileges enabled | Medium | PASS | ["no-new-privileges:true"] | Keep security_opt: no-new-privileges:true. |
| GymBro API - secure multi-stage research image | RUN-05 published ports | Info | INFO | {} | Production should expose only Web/Gateway when possible. |
| GymBro migrator - secure multi-stage research image | IMG-00 image size | Info | INFO | gymbro/migrator:secure-multistage = 111.4 MB | Use this value to compare single-stage and secure multi-stage images. |
| GymBro migrator - secure multi-stage research image | IMG-01 runtime user is non-root | High | PASS | uid=1000 user=gymmigrator | Least privilege is enabled at image level. |
| GymBro migrator - secure multi-stage research image | IMG-02 no .NET SDK in runtime | High | PASS | dotnet --list-sdks returned no SDK | This proves the final image does not carry the SDK. |
| GymBro migrator - secure multi-stage research image | IMG-03 no source/build files leaked | High | PASS | No .cs/.csproj/.sln files found under /app or /src | Only published artifacts are copied to final image. |
| GymBro migrator - secure multi-stage research image | IMG-04 no debug/development files | Medium | PASS | No .pdb or appsettings.Development.json found under /app | Keep DebugSymbols=false and remove development config from final image. |
| GymBro migrator - secure multi-stage research image | IMG-05 no common build tools | Medium | PASS | dotnet-ef/gcc/make/git were not found | Runtime image stays minimal. |
| GymBro migrator - secure multi-stage research image | IMG-06 simulated write to /app | Medium | WARN | WRITABLE | Use read_only: true in Compose and mount only writable folders. |
| GymBro migrator - secure multi-stage research image | IMG-07 no secrets in image history | High | PASS | No secret pattern found in docker history | Keep secrets out of image layers. |
| GymBro migrator - secure multi-stage research image | IMG-08 no secrets in image env | High | PASS | No secret pattern found in image env | Pass secrets at runtime through env/secrets manager. |
| GymBro migrator - secure multi-stage research image | RUN-01 container user is non-root | High | PASS | Config.User='gymmigrator' | Runtime least privilege is enabled. |
| GymBro migrator - secure multi-stage research image | RUN-02 root filesystem is read-only | Medium | PASS | ReadonlyRootfs=true | Mount volumes only for writable folders. |
| GymBro migrator - secure multi-stage research image | RUN-03 Linux capabilities dropped | Medium | PASS | ["ALL"] | cap_drop: ALL reduces unnecessary kernel privileges. |
| GymBro migrator - secure multi-stage research image | RUN-04 no-new-privileges enabled | Medium | PASS | ["no-new-privileges:true"] | Keep security_opt: no-new-privileges:true. |
| GymBro migrator - secure multi-stage research image | RUN-05 published ports | Info | INFO | {} | Production should expose only Web/Gateway when possible. |
| GymBro secure demo image | IMG-00 image size | Info | INFO | gymbro:secure = 123.7 MB | Use this value to compare single-stage and secure multi-stage images. |
| GymBro secure demo image | IMG-01 runtime user is non-root | High | PASS | uid=1000 user=gymbro | Least privilege is enabled at image level. |
| GymBro secure demo image | IMG-02 no .NET SDK in runtime | High | PASS | dotnet --list-sdks returned no SDK | This proves the final image does not carry the SDK. |
| GymBro secure demo image | IMG-03 no source/build files leaked | High | PASS | No .cs/.csproj/.sln files found under /app or /src | Only published artifacts are copied to final image. |
| GymBro secure demo image | IMG-04 no debug/development files | Medium | PASS | No .pdb or appsettings.Development.json found under /app | Keep DebugSymbols=false and remove development config from final image. |
| GymBro secure demo image | IMG-05 no common build tools | Medium | PASS | dotnet-ef/gcc/make/git were not found | Runtime image stays minimal. |
| GymBro secure demo image | IMG-06 simulated write to /app | Medium | WARN | WRITABLE | Use read_only: true in Compose and mount only writable folders. |
| GymBro secure demo image | IMG-07 no secrets in image history | High | PASS | No secret pattern found in docker history | Keep secrets out of image layers. |
| GymBro secure demo image | IMG-08 no secrets in image env | High | PASS | No secret pattern found in image env | Pass secrets at runtime through env/secrets manager. |
| GymBro secure demo image | RUN-01 container user is non-root | High | PASS | Config.User='gymbro' | Runtime least privilege is enabled. |
| GymBro secure demo image | RUN-02 root filesystem is read-only | Medium | PASS | ReadonlyRootfs=true | Mount volumes only for writable folders. |
| GymBro secure demo image | RUN-03 Linux capabilities dropped | Medium | PASS | ["ALL"] | cap_drop: ALL reduces unnecessary kernel privileges. |
| GymBro secure demo image | RUN-04 no-new-privileges enabled | Medium | PASS | ["no-new-privileges:true"] | Keep security_opt: no-new-privileges:true. |
| GymBro secure demo image | RUN-05 published ports | Info | INFO | {} | Production should expose only Web/Gateway when possible. |
| GymBro single-stage baseline image | IMG-00 image size | Info | INFO | gymbro:single-stage = 629.1 MB | Use this value to compare single-stage and secure multi-stage images. |
| GymBro single-stage baseline image | IMG-01 runtime user is non-root | High | FAIL | uid=0 user=root | Create a non-root user in final stage and set USER. |
| GymBro single-stage baseline image | IMG-02 no .NET SDK in runtime | High | FAIL | 9.0.313 [/usr/share/dotnet/sdk] | Final image should use mcr.microsoft.com/dotnet/aspnet, not dotnet/sdk. |
| GymBro single-stage baseline image | IMG-03 no source/build files leaked | High | FAIL | /src/GymBro.Web/Helpers/SessionExtensions.cs \| /src/GymBro.Web/Helpers/PasswordHelper.cs \| /src/GymBro.Web/Controllers/CategoriesController.cs \| /src/GymBro.Web/Controllers/PaymentMethodsController.cs \| /src/GymBro.Web/Controllers/OrdersController.cs \| /src/GymBro.Web/Controllers/CartController.cs \| /src/GymBro.Web/Controllers/AdminSetupController.cs \| /src/GymBro.Web/Controllers/HomeController.cs \| /src/GymBro.Web/Controllers/AccountController.cs \| /src/GymBro.Web/Controllers/OrderDetailsController.cs \| /src/GymBro.Web/Controllers/PaymentsController.cs \| /src/GymBro.Web/Controllers/InventoryController.cs \| /src/GymBro.Web/Controllers/WishlistController.cs \| /src/GymBro.Web/Controllers/UsersController.cs \| /src/GymBro.Web/Controllers/ReviewsController.cs \| /src/GymBro.Web/Controllers/SupplierController.cs \| /src/GymBro.Web/Controllers/BaseAdminController.cs \| /src/GymBro.Web/Controllers/ProductsController.cs \| /src/GymBro.Web/Controllers/AdminController.cs \| /src/GymBro.Web/Controllers/AdminReviewsController.cs | Do not copy the whole source tree into the final image. |
| GymBro single-stage baseline image | IMG-04 no debug/development files | Medium | WARN | /app/web/GymBro.Web.pdb \| /app/web/GymBro.Core.pdb \| /app/web/GymBro.Infrastructure.pdb \| /app/web/appsettings.Development.json \| /app/api/GymBro.Core.pdb \| /app/api/GymBro.API.pdb \| /app/api/GymBro.Infrastructure.pdb \| /app/api/appsettings.Development.json | Consider removing .pdb and appsettings.Development.json from runtime image. |
| GymBro single-stage baseline image | IMG-05 no common build tools | Medium | FAIL | /usr/bin/git | Remove build tools from final image with multi-stage build. |
| GymBro single-stage baseline image | IMG-06 simulated write to /app | Medium | WARN | WRITABLE | Use read_only: true in Compose and mount only writable folders. |
| GymBro single-stage baseline image | IMG-07 no secrets in image history | High | PASS | No secret pattern found in docker history | Keep secrets out of image layers. |
| GymBro single-stage baseline image | IMG-08 no secrets in image env | High | PASS | No secret pattern found in image env | Pass secrets at runtime through env/secrets manager. |
| GymBro single-stage baseline image | RUN-01 container user is non-root | High | FAIL | Config.User='' | Set USER non-root in Dockerfile final stage. |
| GymBro single-stage baseline image | RUN-02 root filesystem is read-only | Medium | WARN | ReadonlyRootfs=false | Consider read_only: true in production Compose. |
| GymBro single-stage baseline image | RUN-03 Linux capabilities dropped | Medium | WARN | null | Consider cap_drop: ALL when the service does not need special capabilities. |
| GymBro single-stage baseline image | RUN-04 no-new-privileges enabled | Medium | WARN | null | Consider security_opt: no-new-privileges:true in Compose. |
| GymBro single-stage baseline image | RUN-05 published ports | Info | INFO | {} | Production should expose only Web/Gateway when possible. |
| GymBro Web - secure multi-stage research image | IMG-00 image size | Info | INFO | gymbro/web:secure-multistage = 110.6 MB | Use this value to compare single-stage and secure multi-stage images. |
| GymBro Web - secure multi-stage research image | IMG-01 runtime user is non-root | High | PASS | uid=1000 user=gymuser | Least privilege is enabled at image level. |
| GymBro Web - secure multi-stage research image | IMG-02 no .NET SDK in runtime | High | PASS | dotnet --list-sdks returned no SDK | This proves the final image does not carry the SDK. |
| GymBro Web - secure multi-stage research image | IMG-03 no source/build files leaked | High | PASS | No .cs/.csproj/.sln files found under /app or /src | Only published artifacts are copied to final image. |
| GymBro Web - secure multi-stage research image | IMG-04 no debug/development files | Medium | PASS | No .pdb or appsettings.Development.json found under /app | Keep DebugSymbols=false and remove development config from final image. |
| GymBro Web - secure multi-stage research image | IMG-05 no common build tools | Medium | PASS | dotnet-ef/gcc/make/git were not found | Runtime image stays minimal. |
| GymBro Web - secure multi-stage research image | IMG-06 simulated write to /app | Medium | WARN | WRITABLE | Use read_only: true in Compose and mount only writable folders. |
| GymBro Web - secure multi-stage research image | IMG-07 no secrets in image history | High | PASS | No secret pattern found in docker history | Keep secrets out of image layers. |
| GymBro Web - secure multi-stage research image | IMG-08 no secrets in image env | High | PASS | No secret pattern found in image env | Pass secrets at runtime through env/secrets manager. |
| GymBro Web - secure multi-stage research image | RUN-01 container user is non-root | High | PASS | Config.User='gymbro' | Runtime least privilege is enabled. |
| GymBro Web - secure multi-stage research image | RUN-02 root filesystem is read-only | Medium | PASS | ReadonlyRootfs=true | Mount volumes only for writable folders. |
| GymBro Web - secure multi-stage research image | RUN-03 Linux capabilities dropped | Medium | PASS | ["ALL"] | cap_drop: ALL reduces unnecessary kernel privileges. |
| GymBro Web - secure multi-stage research image | RUN-04 no-new-privileges enabled | Medium | PASS | ["no-new-privileges:true"] | Keep security_opt: no-new-privileges:true. |
| GymBro Web - secure multi-stage research image | RUN-05 published ports | Info | INFO | {} | Production should expose only Web/Gateway when possible. |
| GymBro Web multi-stage report image | IMG-00 image size | Info | INFO | gymbro-web-multistage-report:latest = 111.2 MB | Use this value to compare single-stage and secure multi-stage images. |
| GymBro Web multi-stage report image | IMG-01 runtime user is non-root | High | PASS | uid=1000 user=gymuser | Least privilege is enabled at image level. |
| GymBro Web multi-stage report image | IMG-02 no .NET SDK in runtime | High | PASS | dotnet --list-sdks returned no SDK | This proves the final image does not carry the SDK. |
| GymBro Web multi-stage report image | IMG-03 no source/build files leaked | High | PASS | No .cs/.csproj/.sln files found under /app or /src | Only published artifacts are copied to final image. |
| GymBro Web multi-stage report image | IMG-04 no debug/development files | Medium | WARN | /app/GymBro.Web.pdb \| /app/GymBro.Core.pdb \| /app/GymBro.Application.pdb \| /app/GymBro.Infrastructure.pdb \| /app/appsettings.Development.json | Consider removing .pdb and appsettings.Development.json from runtime image. |
| GymBro Web multi-stage report image | IMG-05 no common build tools | Medium | PASS | dotnet-ef/gcc/make/git were not found | Runtime image stays minimal. |
| GymBro Web multi-stage report image | IMG-06 simulated write to /app | Medium | WARN | WRITABLE | Use read_only: true in Compose and mount only writable folders. |
| GymBro Web multi-stage report image | IMG-07 no secrets in image history | High | PASS | No secret pattern found in docker history | Keep secrets out of image layers. |
| GymBro Web multi-stage report image | IMG-08 no secrets in image env | High | PASS | No secret pattern found in image env | Pass secrets at runtime through env/secrets manager. |
| GymBro Web multi-stage report image | RUN-00 container exists | Info | SKIP | Container not found: | Run docker compose up -d or use -StartCompose to test runtime hardening. |
| GymBro Web single-stage report image | IMG-00 image size | Info | INFO | gymbro-web-single-report:latest = 616.8 MB | Use this value to compare single-stage and secure multi-stage images. |
| GymBro Web single-stage report image | IMG-01 runtime user is non-root | High | FAIL | uid=0 user=root | Create a non-root user in final stage and set USER. |
| GymBro Web single-stage report image | IMG-02 no .NET SDK in runtime | High | FAIL | 9.0.312 [/usr/share/dotnet/sdk] | Final image should use mcr.microsoft.com/dotnet/aspnet, not dotnet/sdk. |
| GymBro Web single-stage report image | IMG-03 no source/build files leaked | High | FAIL | /src/GymBro.Web/GymBro.Web.csproj \| /src/GymBro.Web/Features/AdminDashboard/AdminController.cs \| /src/GymBro.Web/Features/Supplier/SupplierController.cs \| /src/GymBro.Web/Features/AdminPayments/AdminPaymentsController.cs \| /src/GymBro.Web/Features/AdminPayments/AdminPaymentsIndexViewModel.cs \| /src/GymBro.Web/Features/AdminPayments/AdminPaymentsDemoSeed.cs \| /src/GymBro.Web/Features/AdminPayments/AdminPaymentDetailsViewModel.cs \| /src/GymBro.Web/Features/Wishlist/WishlistController.cs \| /src/GymBro.Web/Features/Admin/BaseAdminController.cs \| /src/GymBro.Web/Features/Orders/OrdersController.cs \| /src/GymBro.Web/Features/Orders/OrderManagementEditViewModel.cs \| /src/GymBro.Web/Features/Orders/OrderPaymentSummaryViewModel.cs \| /src/GymBro.Web/Features/Orders/OrderPresentationFactory.cs \| /src/GymBro.Web/Features/Orders/OrderDetailReadOnlyViewModel.cs \| /src/GymBro.Web/Features/Orders/OrderDetailListItemViewModel.cs \| /src/GymBro.Web/Features/Orders/OrderLineItemViewModel.cs \| /src/GymBro.Web/Features/Orders/OrderDetailIndexViewModel.cs \| /src/GymBro.Web/Features/Orders/BadgeViewModel.cs \| /src/GymBro.Web/Features/Orders/OrderListItemViewModel.cs \| /src/GymBro.Web/Features/Orders/OrderManagementIndexViewModel.cs | Do not copy the whole source tree into the final image. |
| GymBro Web single-stage report image | IMG-04 no debug/development files | Medium | WARN | /app/publish/GymBro.Web.pdb \| /app/publish/GymBro.Core.pdb \| /app/publish/GymBro.Application.pdb \| /app/publish/GymBro.Infrastructure.pdb \| /app/publish/appsettings.Development.json | Consider removing .pdb and appsettings.Development.json from runtime image. |
| GymBro Web single-stage report image | IMG-05 no common build tools | Medium | FAIL | /usr/bin/git | Remove build tools from final image with multi-stage build. |
| GymBro Web single-stage report image | IMG-06 simulated write to /app | Medium | WARN | WRITABLE | Use read_only: true in Compose and mount only writable folders. |
| GymBro Web single-stage report image | IMG-07 no secrets in image history | High | PASS | No secret pattern found in docker history | Keep secrets out of image layers. |
| GymBro Web single-stage report image | IMG-08 no secrets in image env | High | PASS | No secret pattern found in image env | Pass secrets at runtime through env/secrets manager. |
| GymBro Web single-stage report image | RUN-00 container exists | Info | SKIP | Container not found: | Run docker compose up -d or use -StartCompose to test runtime hardening. |
