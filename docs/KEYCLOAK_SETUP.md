# راه‌اندازی Keycloak برای وقت‌نگار

## ۱. اجرای محلی
Keycloak را با نصب فعلی ویندوز اجرا کنید. آدرس کنسول مدیریت:

- Admin: http://localhost:8080/admin/
- Realm: http://localhost:8080/realms/vaghtnegar

Realm باید `vaghtnegar` و Client شناسه `vaghtnegar-web` از نوع Public باشد. Standard Flow روشن و PKCE method برابر `S256` باشد.

نقش‌های Realm مورد استفاده برنامه:
- `Patient`
- `Doctor`
- `Receptionist`
- `ClinicAdmin`
- `SuperAdmin`

## ۲. تنظیم API
تنظیمات پیش‌فرض API:
- Authority: `http://localhost:8080/realms/vaghtnegar`
- Audience: `vaghtnegar-api`

در محیط عملیاتی از Issuer مبتنی بر HTTPS استفاده کنید و حالت توسعه Keycloak را در معرض اینترنت قرار ندهید.

## ۳. تنظیم Client برای Swagger
URIهای Swagger را نگه دارید:
- Redirect: `https://localhost:7180/swagger/oauth2-redirect.html`
- Web origin: `https://localhost:7180`

## ۴. تنظیم Client برای Blazor UI
در همان Client با شناسه `vaghtnegar-web`، موارد زیر را به مقادیر فعلی اضافه کنید؛ URIهای Swagger را حذف نکنید:

- Valid redirect URIs: `https://localhost:7290/signin-oidc`
- Valid post logout redirect URIs: `https://localhost:7290/signout-callback-oidc`
- Web origins: `https://localhost:7290`
- Client authentication: OFF (Public)
- Standard flow: ON
- PKCE method: S256

Blazor از OpenID Connect Authorization Code + PKCE استفاده می‌کند. Access Token در مرورگر در اختیار JavaScript قرار نمی‌گیرد؛ درخواست‌های API از Backend-for-Frontend سمت سرور عبور می‌کنند و توکن با نشست کاربر به API ارسال می‌شود.

تنظیمات UI در `src/VaghtNegar.Web/appsettings.json` است:
- `Keycloak:Authority`
- `Keycloak:ClientId`
- `Api:BaseUrl`

## ۵. اتصال پرونده بیمار به حساب Keycloak
۱. در کنسول Keycloak وارد Realm با نام `vaghtnegar` شوید و از بخش Users کاربر موردنظر را باز کنید.
۲. مقدار شناسه کاربر (ID یا `sub`) را از جزئیات همان کاربر بردارید؛ از نام کاربری یا ایمیل به‌جای sub استفاده نکنید.
۳. در وقت‌نگار به بخش «مدیریت بیماران» بروید، پرونده بیمار را پیدا کنید و «اتصال حساب» را انتخاب کنید.
۴. شماره موبایل بیمار را با روش معتبر بررسی کنید، sub را وارد و تأیید احراز موبایل را علامت بزنید.
۵. در Keycloak از بخش Role mapping نقش Realm با نام دقیق `Patient` را به همان کاربر اختصاص دهید. اتصال پرونده به‌تنهایی نقش Keycloak را ایجاد یا اعطا نمی‌کند.
۶. بیمار از حساب خارج شود و دوباره وارد شود تا نقش جدید در توکن نشست او قرار گیرد.

توجه: شناسه sub داده هویتی حساس است. آن را فقط از پنل مدیریت Keycloak دریافت و در اختیار کارکنان مجاز قرار دهید. اتصال یک sub به بیش از یک پرونده مجاز نیست.

## ۶. کاربر آزمایشی
در Realm کاربر آزمایشی بسازید، حساب را فعال کنید، رمز عبور تعیین کنید و نقش Realm متناسب را به او بدهید. برای ورود به پنل‌ها، نقش متناظر لازم است.

## ۶. ملاحظات امنیتی
- ثبت‌نام عمومی تا زمان پیاده‌سازی تأیید شماره موبایل و اتصال امن پرونده بیمار غیرفعال بماند.
- نقش‌های UI فقط برای نمایش و مسیریابی هستند؛ کنترل دسترسی نهایی باید در API اعمال شود.
- Client و تنظیمات تولید باید با HTTPS، مدیریت امن Secretها و تنظیمات عملیاتی مناسب مستقر شوند.
- توکن‌های کامل یا رمز عبور را در گفتگو، لاگ یا مخزن Git قرار ندهید.
