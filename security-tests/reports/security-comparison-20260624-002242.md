# GymBro Docker Security Comparison Report

**Time:** 2026-06-24 00:22:42

## Executive Summary

This report compares Docker security test results between:
- **Single-stage** (baseline): SDK image, no hardening
- **Multi-stage hardened** (secure): aspnet runtime, non-root user, capability drop, no-new-privileges, memory limits, HEALTHCHECK

## Summary Comparison

| Test Case | Single-stage | Multi-stage hardened | Improvement |
|-----------|:-----------:|:-------------------:|-------------|
| CMP-01 no hard-coded secrets | N/A | FAIL | N/A |
| CMP-02 not Development for secure deploy | N/A | WARN | N/A |
| CMP-03 internal ports are not over-published | N/A | WARN | N/A |
| CMP-04 security hardening in compose | N/A | PASS | N/A |
| IMG-00 image size | INFO | INFO | âž¡ï¸ Same |
| IMG-01 runtime user is non-root | FAIL | PASS | âœ… Major improvement |
| IMG-02 no .NET SDK in runtime | FAIL | PASS | âœ… Major improvement |
| IMG-03 no source/build files leaked | FAIL | PASS | âœ… Major improvement |
| IMG-04 no debug/development files | WARN | WARN | âž¡ï¸ Same |
| IMG-05 no common build tools | FAIL | PASS | âœ… Major improvement |
| IMG-06 simulated write to /app | WARN | WARN | âž¡ï¸ Same |
| IMG-07 no secrets in image history | PASS | PASS | âž¡ï¸ Both pass |
| IMG-08 no secrets in image env | PASS | PASS | âž¡ï¸ Both pass |
| IMG-09 HEALTHCHECK defined | WARN | PASS | âœ… Improved |
| RUN-01 container user is non-root | FAIL | PASS | âœ… Major improvement |
| RUN-02 root filesystem is read-only | WARN | WARN | âž¡ï¸ Same |
| RUN-03 Linux capabilities dropped | WARN | PASS | âœ… Improved |
| RUN-04 no-new-privileges enabled | WARN | PASS | âœ… Improved |
| RUN-05 memory limit set | WARN | PASS | âœ… Improved |
| RUN-06 published ports | INFO | INFO | âž¡ï¸ Same |

## Detailed Results

| Profile | Target | Case | Severity | Status | Evidence |
| --- | --- | --- | --- | --- | --- |
| MULTI | docker-compose-multistage.yml | CMP-01 no hard-coded secrets | High | FAIL | line 9: - MSSQL_SA_PASSWORD=*** \| line 30: RABBITMQ_DEFAULT_PASS: *** \| line 50: - ConnectionStrings__DefaultConnection=Server=db;Database=GymBro_Identity;User Id=sa;Password=***;TrustServerCertificate=True;MultipleActiveResultSets=true \| line 79: - ConnectionStrings__DefaultConnection=Server=db;Database=GymBro_Product;User Id=sa;Password=***;TrustServerCertificate=True;MultipleActiveResultSets=true \| line 109: - ConnectionStrings__DefaultConnection=Server=db;Database=GymBro_Order;User Id=sa;Password=***;TrustServerCertificate=True;MultipleActiveResultSets=true |
| MULTI | docker-compose-multistage.yml | CMP-02 not Development for secure deploy | Medium | WARN | Compose declares ASPNETCORE_ENVIRONMENT=Development |
| MULTI | docker-compose-multistage.yml | CMP-03 internal ports are not over-published | Medium | WARN | 1433:1433, 5672:5672, 15672:15672, 7001:8080, 7002:8080, 7003:8080 |
| MULTI | docker-compose-multistage.yml | CMP-04 security hardening in compose | Medium | PASS | cap_drop, no-new-privileges, memory-limits |
| SINGLE | GymBro Gateway - baseline single-stage | IMG-00 image size | Info | INFO | gymbro-gateway:single = 342.7 MB |
| SINGLE | GymBro Gateway - baseline single-stage | IMG-01 runtime user is non-root | High | FAIL | uid=0 user=root |
| SINGLE | GymBro Gateway - baseline single-stage | IMG-02 no .NET SDK in runtime | High | FAIL | 8.0.420 [/usr/share/dotnet/sdk] |
| SINGLE | GymBro Gateway - baseline single-stage | IMG-03 no source/build files leaked | High | FAIL | /src/GymBro.Gateway/Program.cs \| /src/GymBro.Gateway/GymBro.Gateway.csproj \| /src/GymBro.Gateway/obj/Release/net8.0/GymBro.Gateway.GlobalUsings.g.cs \| /src/GymBro.Gateway/obj/Release/net8.0/.NETCoreApp,Version=v8.0.AssemblyAttributes.cs \| /src/GymBro.Gateway/obj/Release/net8.0/GymBro.Gateway.AssemblyInfo.cs \| /src/GymBro.Gateway/obj/Release/net8.0/GymBro.Gateway.MvcApplicationPartsAssemblyInfo.cs \| /src/GymBro.Web/Helpers/SessionExtensions.cs \| /src/GymBro.Web/Helpers/PasswordHelper.cs \| /src/GymBro.Web/Controllers/CategoriesController.cs \| /src/GymBro.Web/Controllers/PaymentMethodsController.cs \| /src/GymBro.Web/Controllers/OrdersController.cs \| /src/GymBro.Web/Controllers/CartController.cs \| /src/GymBro.Web/Controllers/AdminSetupController.cs \| /src/GymBro.Web/Controllers/HomeController.cs \| /src/GymBro.Web/Controllers/AdminChatController.cs \| /src/GymBro.Web/Controllers/AccountController.cs \| /src/GymBro.Web/Controllers/OrderDetailsController.cs \| /src/GymBro.Web/Controllers/PaymentsController.cs \| /src/GymBro.Web/Controllers/InventoryController.cs \| /src/GymBro.Web/Controllers/WishlistController.cs |
| SINGLE | GymBro Gateway - baseline single-stage | IMG-04 no debug/development files | Medium | WARN | /app/publish/appsettings.Development.json \| /app/publish/GymBro.Gateway.pdb |
| SINGLE | GymBro Gateway - baseline single-stage | IMG-05 no common build tools | Medium | FAIL | /usr/bin/git |
| SINGLE | GymBro Gateway - baseline single-stage | IMG-06 simulated write to /app | Medium | WARN | WRITABLE |
| SINGLE | GymBro Gateway - baseline single-stage | IMG-07 no secrets in image history | High | PASS | No secret pattern found in docker history |
| SINGLE | GymBro Gateway - baseline single-stage | IMG-08 no secrets in image env | High | PASS | No secret pattern found in image env |
| SINGLE | GymBro Gateway - baseline single-stage | IMG-09 HEALTHCHECK defined | Medium | WARN | No HEALTHCHECK found |
| SINGLE | GymBro Gateway - baseline single-stage | RUN-01 container user is non-root | High | FAIL | Config.User='' |
| SINGLE | GymBro Gateway - baseline single-stage | RUN-02 root filesystem is read-only | Medium | WARN | ReadonlyRootfs=false |
| SINGLE | GymBro Gateway - baseline single-stage | RUN-03 Linux capabilities dropped | Medium | WARN | null |
| SINGLE | GymBro Gateway - baseline single-stage | RUN-04 no-new-privileges enabled | Medium | WARN | null |
| SINGLE | GymBro Gateway - baseline single-stage | RUN-05 memory limit set | Low | WARN | Memory=unlimited |
| SINGLE | GymBro Gateway - baseline single-stage | RUN-06 published ports | Info | INFO | {"8080/tcp":[{"HostIp":"0.0.0.0","HostPort":"8001"},{"HostIp":"::","HostPort":"8001"}]} |
| MULTI | GymBro Gateway - current multi-stage | IMG-00 image size | Info | INFO | gymbro-gateway:multi = 91.6 MB |
| MULTI | GymBro Gateway - current multi-stage | IMG-01 runtime user is non-root | High | PASS | uid=999 user=gymbro |
| MULTI | GymBro Gateway - current multi-stage | IMG-02 no .NET SDK in runtime | High | PASS | dotnet --list-sdks returned no SDK |
| MULTI | GymBro Gateway - current multi-stage | IMG-03 no source/build files leaked | High | PASS | No .cs/.csproj/.sln files found under /app or /src |
| MULTI | GymBro Gateway - current multi-stage | IMG-04 no debug/development files | Medium | WARN | /app/appsettings.Development.json |
| MULTI | GymBro Gateway - current multi-stage | IMG-05 no common build tools | Medium | PASS | dotnet-ef/gcc/make/git were not found |
| MULTI | GymBro Gateway - current multi-stage | IMG-06 simulated write to /app | Medium | WARN | WRITABLE |
| MULTI | GymBro Gateway - current multi-stage | IMG-07 no secrets in image history | High | PASS | No secret pattern found in docker history |
| MULTI | GymBro Gateway - current multi-stage | IMG-08 no secrets in image env | High | PASS | No secret pattern found in image env |
| MULTI | GymBro Gateway - current multi-stage | IMG-09 HEALTHCHECK defined | Medium | PASS | {"Test":["CMD-SHELL","curl -f http://localhost:8080/ \|\| exit 1"],"Interval":30000000000,"Timeout":5000000000,"StartPeriod":15000000000,"Retries":3} |
| MULTI | GymBro Gateway - current multi-stage | RUN-01 container user is non-root | High | PASS | Config.User='gymbro' |
| MULTI | GymBro Gateway - current multi-stage | RUN-02 root filesystem is read-only | Medium | WARN | ReadonlyRootfs=false |
| MULTI | GymBro Gateway - current multi-stage | RUN-03 Linux capabilities dropped | Medium | PASS | ["ALL"] |
| MULTI | GymBro Gateway - current multi-stage | RUN-04 no-new-privileges enabled | Medium | PASS | ["no-new-privileges:true"] |
| MULTI | GymBro Gateway - current multi-stage | RUN-05 memory limit set | Low | PASS | Memory=256 MB |
| MULTI | GymBro Gateway - current multi-stage | RUN-06 published ports | Info | INFO | {"8080/tcp":[{"HostIp":"0.0.0.0","HostPort":"8000"},{"HostIp":"::","HostPort":"8000"}]} |
| SINGLE | GymBro Identity API - baseline single-stage | IMG-00 image size | Info | INFO | gymbro-identity:single = 711.8 MB |
| SINGLE | GymBro Identity API - baseline single-stage | IMG-01 runtime user is non-root | High | FAIL | uid=0 user=root |
| SINGLE | GymBro Identity API - baseline single-stage | IMG-02 no .NET SDK in runtime | High | FAIL | 8.0.420 [/usr/share/dotnet/sdk] |
| SINGLE | GymBro Identity API - baseline single-stage | IMG-03 no source/build files leaked | High | FAIL | /src/GymBro.Web/Helpers/SessionExtensions.cs \| /src/GymBro.Web/Helpers/PasswordHelper.cs \| /src/GymBro.Web/Controllers/CategoriesController.cs \| /src/GymBro.Web/Controllers/PaymentMethodsController.cs \| /src/GymBro.Web/Controllers/OrdersController.cs \| /src/GymBro.Web/Controllers/CartController.cs \| /src/GymBro.Web/Controllers/AdminSetupController.cs \| /src/GymBro.Web/Controllers/HomeController.cs \| /src/GymBro.Web/Controllers/AdminChatController.cs \| /src/GymBro.Web/Controllers/AccountController.cs \| /src/GymBro.Web/Controllers/OrderDetailsController.cs \| /src/GymBro.Web/Controllers/PaymentsController.cs \| /src/GymBro.Web/Controllers/InventoryController.cs \| /src/GymBro.Web/Controllers/WishlistController.cs \| /src/GymBro.Web/Controllers/UsersController.cs \| /src/GymBro.Web/Controllers/ReviewsController.cs \| /src/GymBro.Web/Controllers/SupplierController.cs \| /src/GymBro.Web/Controllers/BaseAdminController.cs \| /src/GymBro.Web/Controllers/ProductsController.cs \| /src/GymBro.Web/Controllers/AdminController.cs |
| SINGLE | GymBro Identity API - baseline single-stage | IMG-04 no debug/development files | Medium | WARN | /app/publish/GymBro.Service.pdb \| /app/publish/GymBro.Contracts.pdb \| /app/publish/GymBro.Web.pdb \| /app/publish/GymBro.Core.pdb \| /app/publish/GymBro.Order.API.pdb \| /app/publish/GymBro.Product.API.pdb \| /app/publish/GymBro.Identity.API.pdb \| /app/publish/GymBro.Infrastructure.pdb \| /app/publish/appsettings.Development.json |
| SINGLE | GymBro Identity API - baseline single-stage | IMG-05 no common build tools | Medium | FAIL | /usr/bin/git |
| SINGLE | GymBro Identity API - baseline single-stage | IMG-06 simulated write to /app | Medium | WARN | WRITABLE |
| SINGLE | GymBro Identity API - baseline single-stage | IMG-07 no secrets in image history | High | PASS | No secret pattern found in docker history |
| SINGLE | GymBro Identity API - baseline single-stage | IMG-08 no secrets in image env | High | PASS | No secret pattern found in image env |
| SINGLE | GymBro Identity API - baseline single-stage | IMG-09 HEALTHCHECK defined | Medium | WARN | No HEALTHCHECK found |
| SINGLE | GymBro Identity API - baseline single-stage | RUN-01 container user is non-root | High | FAIL | Config.User='' |
| SINGLE | GymBro Identity API - baseline single-stage | RUN-02 root filesystem is read-only | Medium | WARN | ReadonlyRootfs=false |
| SINGLE | GymBro Identity API - baseline single-stage | RUN-03 Linux capabilities dropped | Medium | WARN | null |
| SINGLE | GymBro Identity API - baseline single-stage | RUN-04 no-new-privileges enabled | Medium | WARN | null |
| SINGLE | GymBro Identity API - baseline single-stage | RUN-05 memory limit set | Low | WARN | Memory=unlimited |
| SINGLE | GymBro Identity API - baseline single-stage | RUN-06 published ports | Info | INFO | {"8080/tcp":[{"HostIp":"0.0.0.0","HostPort":"7011"},{"HostIp":"::","HostPort":"7011"}]} |
| MULTI | GymBro Identity API - current multi-stage | IMG-00 image size | Info | INFO | gymbro-identity:multi = 115.5 MB |
| MULTI | GymBro Identity API - current multi-stage | IMG-01 runtime user is non-root | High | PASS | uid=999 user=gymbro |
| MULTI | GymBro Identity API - current multi-stage | IMG-02 no .NET SDK in runtime | High | PASS | dotnet --list-sdks returned no SDK |
| MULTI | GymBro Identity API - current multi-stage | IMG-03 no source/build files leaked | High | PASS | No .cs/.csproj/.sln files found under /app or /src |
| MULTI | GymBro Identity API - current multi-stage | IMG-04 no debug/development files | Medium | WARN | /app/appsettings.Development.json |
| MULTI | GymBro Identity API - current multi-stage | IMG-05 no common build tools | Medium | PASS | dotnet-ef/gcc/make/git were not found |
| MULTI | GymBro Identity API - current multi-stage | IMG-06 simulated write to /app | Medium | WARN | WRITABLE |
| MULTI | GymBro Identity API - current multi-stage | IMG-07 no secrets in image history | High | PASS | No secret pattern found in docker history |
| MULTI | GymBro Identity API - current multi-stage | IMG-08 no secrets in image env | High | PASS | No secret pattern found in image env |
| MULTI | GymBro Identity API - current multi-stage | IMG-09 HEALTHCHECK defined | Medium | PASS | {"Test":["CMD-SHELL","curl -f http://localhost:8080/swagger/index.html \|\| exit 1"],"Interval":30000000000,"Timeout":5000000000,"StartPeriod":15000000000,"Retries":3} |
| MULTI | GymBro Identity API - current multi-stage | RUN-01 container user is non-root | High | PASS | Config.User='gymbro' |
| MULTI | GymBro Identity API - current multi-stage | RUN-02 root filesystem is read-only | Medium | WARN | ReadonlyRootfs=false |
| MULTI | GymBro Identity API - current multi-stage | RUN-03 Linux capabilities dropped | Medium | PASS | ["ALL"] |
| MULTI | GymBro Identity API - current multi-stage | RUN-04 no-new-privileges enabled | Medium | PASS | ["no-new-privileges:true"] |
| MULTI | GymBro Identity API - current multi-stage | RUN-05 memory limit set | Low | PASS | Memory=512 MB |
| MULTI | GymBro Identity API - current multi-stage | RUN-06 published ports | Info | INFO | {"8080/tcp":[{"HostIp":"0.0.0.0","HostPort":"7001"},{"HostIp":"::","HostPort":"7001"}]} |
| SINGLE | GymBro Order API - baseline single-stage | IMG-00 image size | Info | INFO | gymbro-order:single = 595.6 MB |
| SINGLE | GymBro Order API - baseline single-stage | IMG-01 runtime user is non-root | High | FAIL | uid=0 user=root |
| SINGLE | GymBro Order API - baseline single-stage | IMG-02 no .NET SDK in runtime | High | FAIL | 8.0.420 [/usr/share/dotnet/sdk] |
| SINGLE | GymBro Order API - baseline single-stage | IMG-03 no source/build files leaked | High | FAIL | /src/GymBro.Service/GymBro.Service.csproj \| /src/GymBro.Service/Services/IProductService.cs \| /src/GymBro.Service/Services/SupplierService.cs \| /src/GymBro.Service/Services/IWishlistService.cs \| /src/GymBro.Service/Services/ISupplierService.cs \| /src/GymBro.Service/Services/IUserService.cs \| /src/GymBro.Service/Services/WishlistService.cs \| /src/GymBro.Service/Services/OrderService.cs \| /src/GymBro.Service/Services/ICategoryService.cs \| /src/GymBro.Service/Services/IdentityService.cs \| /src/GymBro.Service/Services/UserService.cs \| /src/GymBro.Service/Services/CategoryService.cs \| /src/GymBro.Service/Services/ReviewService.cs \| /src/GymBro.Service/Services/ProductService.cs \| /src/GymBro.Service/Services/IIdentityService.cs \| /src/GymBro.Service/Services/IPaymentService.cs \| /src/GymBro.Service/Services/PaymentService.cs \| /src/GymBro.Service/Services/IOrderService.cs \| /src/GymBro.Service/Services/IReviewService.cs \| /src/GymBro.Service/obj/Release/net8.0/.NETCoreApp,Version=v8.0.AssemblyAttributes.cs |
| SINGLE | GymBro Order API - baseline single-stage | IMG-04 no debug/development files | Medium | WARN | /app/publish/GymBro.Service.pdb \| /app/publish/GymBro.Contracts.pdb \| /app/publish/GymBro.Core.pdb \| /app/publish/GymBro.Order.API.pdb \| /app/publish/GymBro.Infrastructure.pdb \| /app/publish/appsettings.Development.json |
| SINGLE | GymBro Order API - baseline single-stage | IMG-05 no common build tools | Medium | FAIL | /usr/bin/git |
| SINGLE | GymBro Order API - baseline single-stage | IMG-06 simulated write to /app | Medium | WARN | WRITABLE |
| SINGLE | GymBro Order API - baseline single-stage | IMG-07 no secrets in image history | High | PASS | No secret pattern found in docker history |
| SINGLE | GymBro Order API - baseline single-stage | IMG-08 no secrets in image env | High | PASS | No secret pattern found in image env |
| SINGLE | GymBro Order API - baseline single-stage | IMG-09 HEALTHCHECK defined | Medium | WARN | No HEALTHCHECK found |
| SINGLE | GymBro Order API - baseline single-stage | RUN-01 container user is non-root | High | FAIL | Config.User='' |
| SINGLE | GymBro Order API - baseline single-stage | RUN-02 root filesystem is read-only | Medium | WARN | ReadonlyRootfs=false |
| SINGLE | GymBro Order API - baseline single-stage | RUN-03 Linux capabilities dropped | Medium | WARN | null |
| SINGLE | GymBro Order API - baseline single-stage | RUN-04 no-new-privileges enabled | Medium | WARN | null |
| SINGLE | GymBro Order API - baseline single-stage | RUN-05 memory limit set | Low | WARN | Memory=unlimited |
| SINGLE | GymBro Order API - baseline single-stage | RUN-06 published ports | Info | INFO | {"8080/tcp":[{"HostIp":"0.0.0.0","HostPort":"7013"},{"HostIp":"::","HostPort":"7013"}]} |
| MULTI | GymBro Order API - current multi-stage | IMG-00 image size | Info | INFO | gymbro-order:multi = 103.7 MB |
| MULTI | GymBro Order API - current multi-stage | IMG-01 runtime user is non-root | High | PASS | uid=999 user=gymbro |
| MULTI | GymBro Order API - current multi-stage | IMG-02 no .NET SDK in runtime | High | PASS | dotnet --list-sdks returned no SDK |
| MULTI | GymBro Order API - current multi-stage | IMG-03 no source/build files leaked | High | PASS | No .cs/.csproj/.sln files found under /app or /src |
| MULTI | GymBro Order API - current multi-stage | IMG-04 no debug/development files | Medium | WARN | /app/appsettings.Development.json |
| MULTI | GymBro Order API - current multi-stage | IMG-05 no common build tools | Medium | PASS | dotnet-ef/gcc/make/git were not found |
| MULTI | GymBro Order API - current multi-stage | IMG-06 simulated write to /app | Medium | WARN | WRITABLE |
| MULTI | GymBro Order API - current multi-stage | IMG-07 no secrets in image history | High | PASS | No secret pattern found in docker history |
| MULTI | GymBro Order API - current multi-stage | IMG-08 no secrets in image env | High | PASS | No secret pattern found in image env |
| MULTI | GymBro Order API - current multi-stage | IMG-09 HEALTHCHECK defined | Medium | PASS | {"Test":["CMD-SHELL","curl -f http://localhost:8080/swagger/index.html \|\| exit 1"],"Interval":30000000000,"Timeout":5000000000,"StartPeriod":15000000000,"Retries":3} |
| MULTI | GymBro Order API - current multi-stage | RUN-01 container user is non-root | High | PASS | Config.User='gymbro' |
| MULTI | GymBro Order API - current multi-stage | RUN-02 root filesystem is read-only | Medium | WARN | ReadonlyRootfs=false |
| MULTI | GymBro Order API - current multi-stage | RUN-03 Linux capabilities dropped | Medium | PASS | ["ALL"] |
| MULTI | GymBro Order API - current multi-stage | RUN-04 no-new-privileges enabled | Medium | PASS | ["no-new-privileges:true"] |
| MULTI | GymBro Order API - current multi-stage | RUN-05 memory limit set | Low | PASS | Memory=512 MB |
| MULTI | GymBro Order API - current multi-stage | RUN-06 published ports | Info | INFO | {"8080/tcp":[{"HostIp":"0.0.0.0","HostPort":"7003"},{"HostIp":"::","HostPort":"7003"}]} |
| SINGLE | GymBro Product API - baseline single-stage | IMG-00 image size | Info | INFO | gymbro-product:single = 610.7 MB |
| SINGLE | GymBro Product API - baseline single-stage | IMG-01 runtime user is non-root | High | FAIL | uid=0 user=root |
| SINGLE | GymBro Product API - baseline single-stage | IMG-02 no .NET SDK in runtime | High | FAIL | 8.0.420 [/usr/share/dotnet/sdk] |
| SINGLE | GymBro Product API - baseline single-stage | IMG-03 no source/build files leaked | High | FAIL | /src/GymBro.Product.API/Controllers/CategoryController.cs \| /src/GymBro.Product.API/Controllers/HomeController.cs \| /src/GymBro.Product.API/Controllers/ProductController.cs \| /src/GymBro.Product.API/Controllers/ReviewsController.cs \| /src/GymBro.Product.API/Program.cs \| /src/GymBro.Product.API/GymBro.Product.API.csproj \| /src/GymBro.Product.API/Migrations/20260512075139_InitialMasterDb.cs \| /src/GymBro.Product.API/Migrations/20260512075139_InitialMasterDb.Designer.cs \| /src/GymBro.Product.API/Migrations/GymBroDbContextModelSnapshot.cs \| /src/GymBro.Product.API/obj/Release/net8.0/.NETCoreApp,Version=v8.0.AssemblyAttributes.cs \| /src/GymBro.Product.API/obj/Release/net8.0/GymBro.Product.API.GlobalUsings.g.cs \| /src/GymBro.Product.API/obj/Release/net8.0/GymBro.Product.API.MvcApplicationPartsAssemblyInfo.cs \| /src/GymBro.Product.API/obj/Release/net8.0/GymBro.Product.API.AssemblyInfo.cs \| /src/GymBro.Service/GymBro.Service.csproj \| /src/GymBro.Service/Services/IProductService.cs \| /src/GymBro.Service/Services/SupplierService.cs \| /src/GymBro.Service/Services/IWishlistService.cs \| /src/GymBro.Service/Services/ISupplierService.cs \| /src/GymBro.Service/Services/IUserService.cs \| /src/GymBro.Service/Services/WishlistService.cs |
| SINGLE | GymBro Product API - baseline single-stage | IMG-04 no debug/development files | Medium | WARN | /app/publish/GymBro.Service.pdb \| /app/publish/GymBro.Contracts.pdb \| /app/publish/GymBro.Core.pdb \| /app/publish/GymBro.Order.API.pdb \| /app/publish/GymBro.Product.API.pdb \| /app/publish/GymBro.Infrastructure.pdb \| /app/publish/appsettings.Development.json |
| SINGLE | GymBro Product API - baseline single-stage | IMG-05 no common build tools | Medium | FAIL | /usr/bin/git |
| SINGLE | GymBro Product API - baseline single-stage | IMG-06 simulated write to /app | Medium | WARN | WRITABLE |
| SINGLE | GymBro Product API - baseline single-stage | IMG-07 no secrets in image history | High | PASS | No secret pattern found in docker history |
| SINGLE | GymBro Product API - baseline single-stage | IMG-08 no secrets in image env | High | PASS | No secret pattern found in image env |
| SINGLE | GymBro Product API - baseline single-stage | IMG-09 HEALTHCHECK defined | Medium | WARN | No HEALTHCHECK found |
| SINGLE | GymBro Product API - baseline single-stage | RUN-01 container user is non-root | High | FAIL | Config.User='' |
| SINGLE | GymBro Product API - baseline single-stage | RUN-02 root filesystem is read-only | Medium | WARN | ReadonlyRootfs=false |
| SINGLE | GymBro Product API - baseline single-stage | RUN-03 Linux capabilities dropped | Medium | WARN | null |
| SINGLE | GymBro Product API - baseline single-stage | RUN-04 no-new-privileges enabled | Medium | WARN | null |
| SINGLE | GymBro Product API - baseline single-stage | RUN-05 memory limit set | Low | WARN | Memory=unlimited |
| SINGLE | GymBro Product API - baseline single-stage | RUN-06 published ports | Info | INFO | {"8080/tcp":[{"HostIp":"0.0.0.0","HostPort":"7012"},{"HostIp":"::","HostPort":"7012"}]} |
| MULTI | GymBro Product API - current multi-stage | IMG-00 image size | Info | INFO | gymbro-product:multi = 103.8 MB |
| MULTI | GymBro Product API - current multi-stage | IMG-01 runtime user is non-root | High | PASS | uid=999 user=gymbro |
| MULTI | GymBro Product API - current multi-stage | IMG-02 no .NET SDK in runtime | High | PASS | dotnet --list-sdks returned no SDK |
| MULTI | GymBro Product API - current multi-stage | IMG-03 no source/build files leaked | High | PASS | No .cs/.csproj/.sln files found under /app or /src |
| MULTI | GymBro Product API - current multi-stage | IMG-04 no debug/development files | Medium | WARN | /app/appsettings.Development.json |
| MULTI | GymBro Product API - current multi-stage | IMG-05 no common build tools | Medium | PASS | dotnet-ef/gcc/make/git were not found |
| MULTI | GymBro Product API - current multi-stage | IMG-06 simulated write to /app | Medium | WARN | WRITABLE |
| MULTI | GymBro Product API - current multi-stage | IMG-07 no secrets in image history | High | PASS | No secret pattern found in docker history |
| MULTI | GymBro Product API - current multi-stage | IMG-08 no secrets in image env | High | PASS | No secret pattern found in image env |
| MULTI | GymBro Product API - current multi-stage | IMG-09 HEALTHCHECK defined | Medium | PASS | {"Test":["CMD-SHELL","curl -f http://localhost:8080/swagger/index.html \|\| exit 1"],"Interval":30000000000,"Timeout":5000000000,"StartPeriod":15000000000,"Retries":3} |
| MULTI | GymBro Product API - current multi-stage | RUN-01 container user is non-root | High | PASS | Config.User='gymbro' |
| MULTI | GymBro Product API - current multi-stage | RUN-02 root filesystem is read-only | Medium | WARN | ReadonlyRootfs=false |
| MULTI | GymBro Product API - current multi-stage | RUN-03 Linux capabilities dropped | Medium | PASS | ["ALL"] |
| MULTI | GymBro Product API - current multi-stage | RUN-04 no-new-privileges enabled | Medium | PASS | ["no-new-privileges:true"] |
| MULTI | GymBro Product API - current multi-stage | RUN-05 memory limit set | Low | PASS | Memory=512 MB |
| MULTI | GymBro Product API - current multi-stage | RUN-06 published ports | Info | INFO | {"8080/tcp":[{"HostIp":"0.0.0.0","HostPort":"7002"},{"HostIp":"::","HostPort":"7002"}]} |
| SINGLE | GymBro Web - baseline single-stage | IMG-00 image size | Info | INFO | gymbro-web:single = 559.9 MB |
| SINGLE | GymBro Web - baseline single-stage | IMG-01 runtime user is non-root | High | FAIL | uid=0 user=root |
| SINGLE | GymBro Web - baseline single-stage | IMG-02 no .NET SDK in runtime | High | FAIL | 8.0.420 [/usr/share/dotnet/sdk] |
| SINGLE | GymBro Web - baseline single-stage | IMG-03 no source/build files leaked | High | FAIL | /src/GymBro.Web/Helpers/SessionExtensions.cs \| /src/GymBro.Web/Helpers/PasswordHelper.cs \| /src/GymBro.Web/Controllers/CategoriesController.cs \| /src/GymBro.Web/Controllers/PaymentMethodsController.cs \| /src/GymBro.Web/Controllers/OrdersController.cs \| /src/GymBro.Web/Controllers/CartController.cs \| /src/GymBro.Web/Controllers/AdminSetupController.cs \| /src/GymBro.Web/Controllers/HomeController.cs \| /src/GymBro.Web/Controllers/AdminChatController.cs \| /src/GymBro.Web/Controllers/AccountController.cs \| /src/GymBro.Web/Controllers/OrderDetailsController.cs \| /src/GymBro.Web/Controllers/PaymentsController.cs \| /src/GymBro.Web/Controllers/InventoryController.cs \| /src/GymBro.Web/Controllers/WishlistController.cs \| /src/GymBro.Web/Controllers/UsersController.cs \| /src/GymBro.Web/Controllers/ReviewsController.cs \| /src/GymBro.Web/Controllers/SupplierController.cs \| /src/GymBro.Web/Controllers/BaseAdminController.cs \| /src/GymBro.Web/Controllers/ProductsController.cs \| /src/GymBro.Web/Controllers/AdminController.cs |
| SINGLE | GymBro Web - baseline single-stage | IMG-04 no debug/development files | Medium | WARN | /app/publish/GymBro.Service.pdb \| /app/publish/GymBro.Contracts.pdb \| /app/publish/GymBro.Web.pdb \| /app/publish/GymBro.Core.pdb \| /app/publish/GymBro.Infrastructure.pdb \| /app/publish/appsettings.Development.json |
| SINGLE | GymBro Web - baseline single-stage | IMG-05 no common build tools | Medium | FAIL | /usr/bin/git |
| SINGLE | GymBro Web - baseline single-stage | IMG-06 simulated write to /app | Medium | WARN | WRITABLE |
| SINGLE | GymBro Web - baseline single-stage | IMG-07 no secrets in image history | High | PASS | No secret pattern found in docker history |
| SINGLE | GymBro Web - baseline single-stage | IMG-08 no secrets in image env | High | PASS | No secret pattern found in image env |
| SINGLE | GymBro Web - baseline single-stage | IMG-09 HEALTHCHECK defined | Medium | WARN | No HEALTHCHECK found |
| SINGLE | GymBro Web - baseline single-stage | RUN-01 container user is non-root | High | FAIL | Config.User='' |
| SINGLE | GymBro Web - baseline single-stage | RUN-02 root filesystem is read-only | Medium | WARN | ReadonlyRootfs=false |
| SINGLE | GymBro Web - baseline single-stage | RUN-03 Linux capabilities dropped | Medium | WARN | null |
| SINGLE | GymBro Web - baseline single-stage | RUN-04 no-new-privileges enabled | Medium | WARN | null |
| SINGLE | GymBro Web - baseline single-stage | RUN-05 memory limit set | Low | WARN | Memory=unlimited |
| SINGLE | GymBro Web - baseline single-stage | RUN-06 published ports | Info | INFO | {"8080/tcp":[{"HostIp":"0.0.0.0","HostPort":"5001"},{"HostIp":"::","HostPort":"5001"}]} |
| MULTI | GymBro Web - current multi-stage | IMG-00 image size | Info | INFO | gymbro-web:multi = 106.0 MB |
| MULTI | GymBro Web - current multi-stage | IMG-01 runtime user is non-root | High | PASS | uid=999 user=gymbro |
| MULTI | GymBro Web - current multi-stage | IMG-02 no .NET SDK in runtime | High | PASS | dotnet --list-sdks returned no SDK |
| MULTI | GymBro Web - current multi-stage | IMG-03 no source/build files leaked | High | PASS | No .cs/.csproj/.sln files found under /app or /src |
| MULTI | GymBro Web - current multi-stage | IMG-04 no debug/development files | Medium | WARN | /app/appsettings.Development.json |
| MULTI | GymBro Web - current multi-stage | IMG-05 no common build tools | Medium | PASS | dotnet-ef/gcc/make/git were not found |
| MULTI | GymBro Web - current multi-stage | IMG-06 simulated write to /app | Medium | WARN | WRITABLE |
| MULTI | GymBro Web - current multi-stage | IMG-07 no secrets in image history | High | PASS | No secret pattern found in docker history |
| MULTI | GymBro Web - current multi-stage | IMG-08 no secrets in image env | High | PASS | No secret pattern found in image env |
| MULTI | GymBro Web - current multi-stage | IMG-09 HEALTHCHECK defined | Medium | PASS | {"Test":["CMD-SHELL","curl -f http://localhost:8080/ \|\| exit 1"],"Interval":30000000000,"Timeout":5000000000,"StartPeriod":15000000000,"Retries":3} |
| MULTI | GymBro Web - current multi-stage | RUN-01 container user is non-root | High | PASS | Config.User='gymbro' |
| MULTI | GymBro Web - current multi-stage | RUN-02 root filesystem is read-only | Medium | WARN | ReadonlyRootfs=false |
| MULTI | GymBro Web - current multi-stage | RUN-03 Linux capabilities dropped | Medium | PASS | ["ALL"] |
| MULTI | GymBro Web - current multi-stage | RUN-04 no-new-privileges enabled | Medium | PASS | ["no-new-privileges:true"] |
| MULTI | GymBro Web - current multi-stage | RUN-05 memory limit set | Low | PASS | Memory=512 MB |
| MULTI | GymBro Web - current multi-stage | RUN-06 published ports | Info | INFO | {"8080/tcp":[{"HostIp":"0.0.0.0","HostPort":"5000"},{"HostIp":"::","HostPort":"5000"}]} |

## What Multi-stage Hardening Adds

| Feature | Description | Security Benefit |
|---------|-------------|-----------------|
| **Multi-stage build** | Separate SDK (build) from aspnet (runtime) | No SDK, source code, or build tools in runtime image |
| **Non-root user** | USER gymbro in Dockerfile | Attacker cannot modify app files even if they gain shell access |
| **Capability drop** | cap_drop: ALL + cap_add: NET_BIND_SERVICE | Container has minimal kernel capabilities |
| **no-new-privileges** | security_opt: no-new-privileges:true | Prevents privilege escalation via setuid binaries |
| **Memory limits** | deploy.resources.limits.memory | Prevents one container from exhausting host RAM (DoS protection) |
| **HEALTHCHECK** | HEALTHCHECK instruction in Dockerfile | Docker auto-restarts unhealthy containers |
| **Image size reduction** | aspnet ~200MB vs SDK ~1.7GB | Smaller image = smaller attack surface, faster deployment |
