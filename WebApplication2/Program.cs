using Microsoft.AspNetCore.Antiforgery;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Data;
using WebApplication2.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. ��������� ��
builder.Services.AddDbContext<ApplicationDBContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. ��������� ������
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2); // �������� �����, ����� ������ �� ������� ��� ������ ������ ������
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".MyClassroom.Session"; // ���������� ��� ����
});

builder.Services.AddControllersWithViews();

// fetch() отправляет заголовок RequestVerificationToken для [ValidateAntiForgeryToken].
builder.Services.Configure<AntiforgeryOptions>(o => o.HeaderName = "RequestVerificationToken");

builder.Services.AddScoped<CertificatePdfService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// ���������� �������: ������� ������, ����� ��������������/�����������
app.UseSession();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    // ���� ���� ������� � MyCourses, � CourseConstructor, � ��������� ����������� �������������
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}"
    );
});

app.Run();