# وقت‌نگار | VaghtNegar

سامانه نوبت‌دهی و مدیریت خدمات مراکز درمانی و کلینیک‌ها

## چشم‌انداز
وقت‌نگار بستری برای مدیریت کلینیک، پزشکان، تخصص‌ها، برنامه کاری، نوبت‌ها، بیماران، پرداخت‌ها و پیگیری مراجعات است.

## فناوری و معماری
- Backend: ASP.NET Core Web API و Clean Architecture
- Frontend: Blazor Web App (.NET 10، Interactive Server)
- Database: SQL Server با Entity Framework Core
- Identity: Keycloak، OpenID Connect و PKCE
- UI: فارسی، RTL و نمایش تاریخ شمسی
- API Documentation: OpenAPI و Swagger

## ساختار راهکار
- `src/VaghtNegar.Domain`: مدل دامنه و قواعد کسب‌وکار
- `src/VaghtNegar.Application`: سناریوها، قراردادها و اعتبارسنجی
- `src/VaghtNegar.Infrastructure`: پایگاه داده و سرویس‌های بیرونی
- `src/VaghtNegar.API`: نقطه ورود HTTP و پیکربندی برنامه
- `src/VaghtNegar.Web`: رابط کاربری Blazor و پنل‌های نقش‌محور
- `tests/`: آزمون‌های واحد و یکپارچه

## اجرای رابط کاربری
پیش‌نیاز: .NET SDK 10، API روی `https://localhost:7180` و Keycloak روی `http://localhost:8080`.

از ریشه مخزن اجرا کنید:

```powershell
dotnet restore src/VaghtNegar.Web/VaghtNegar.Web.csproj
dotnet dev-certs https --trust
dotnet run --project src/VaghtNegar.Web/VaghtNegar.Web.csproj
```

آدرس پیش‌فرض UI: `https://localhost:7290`. راهنمای تنظیم Client در [راهنمای Keycloak](docs/KEYCLOAK_SETUP.md) و [راهنمای UI](src/VaghtNegar.Web/README.md) آمده است.

## وضعیت توسعه UI
اسکلت مشترک و صفحات نقش‌محور بیمار، پزشک، پذیرش و مدیریت ثبت شده‌اند. داشبورد و فهرست‌های کلینیک/پزشک/نوبت از API خوانده می‌شوند. تکمیل فرم‌های CRUD، تقویم انتخاب تاریخ شمسی، گردش کار رزرو و APIهای اختصاصی پزشک و بیمار در مراحل بعدی انجام می‌شود.
