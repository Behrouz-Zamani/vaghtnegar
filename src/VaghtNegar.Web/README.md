# رابط کاربری وقت‌نگار (Blazor)

رابط کاربری مستقل سامانه وقت‌نگار با Blazor Web App و Interactive Server، طراحی فارسی و راست‌چین.

## پیش‌نیازها
- .NET SDK 10
- اجرای API روی `https://localhost:7180`
- اجرای Keycloak روی `http://localhost:8080` و Realm با نام `vaghtnegar`

## اجرا در ویندوز
از ریشه مخزن:

```powershell
dotnet restore src/VaghtNegar.Web/VaghtNegar.Web.csproj
dotnet dev-certs https --trust
dotnet run --project src/VaghtNegar.Web/VaghtNegar.Web.csproj
```

آدرس رابط کاربری: `https://localhost:7290`

## تنظیم Keycloak
در Client با شناسه `vaghtnegar-web` این مقادیر را اضافه کنید:

- Valid redirect URIs: `https://localhost:7290/signin-oidc`
- Valid post logout redirect URIs: `https://localhost:7290/signout-callback-oidc`
- Web origins: `https://localhost:7290`
- Client authentication: خاموش (Public client)
- Standard flow: روشن
- PKCE method: `S256`

Client از قبل برای Swagger استفاده می‌شود؛ URIهای فعلی Swagger را حذف نکنید. آدرس بازگشت Blazor را به همان فهرست اضافه کنید.

## پیکربندی
مقادیر پیش‌فرض در `appsettings.json` قرار دارند و با متغیرهای محیطی قابل جایگزینی هستند:

- `Keycloak__Authority`
- `Keycloak__ClientId`
- `Api__BaseUrl`

## معماری
- احراز هویت با OpenID Connect Authorization Code + PKCE و کوکی امن.
- نقش‌های Realm در claimهای .NET به نقش‌های برنامه تبدیل می‌شوند.
- مسیرهای API از طریق BFF سمت سرور به API ارسال می‌شوند؛ Access Token در مرورگر در دسترس JavaScript قرار نمی‌گیرد.
- مقصد BFF فقط از تنظیمات سرور خوانده می‌شود و ورودی مرورگر نمی‌تواند host مقصد را تعیین کند.
- نقش‌ها در منو و صفحات کنترل می‌شوند؛ مجوز نهایی همچنان باید در API enforce شود.

## وضعیت صفحات
- داشبورد مشترک و دریافت تعداد کلینیک‌ها و پزشکان از API
- پنل بیمار (ساختار و نقش آماده؛ اتصال کامل به Patient Portal پس از ثبت endpointها در API)
- پنل پزشک (ساختار آماده؛ نیازمند endpointهای اختصاصی پزشک)
- پذیرش و فهرست نوبت‌ها
- مدیریت کلینیک و پزشکان

تاریخ امروز در هدر به تقویم شمسی نمایش داده می‌شود. تقویم انتخاب تاریخ شمسی و فرم‌های کامل CRUD در گام بعدی به صفحات مربوط اضافه می‌شوند.
