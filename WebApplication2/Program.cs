using Microsoft.EntityFrameworkCore;
using WebApplication2.Data;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDBContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(1);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}


app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.UseSession();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}"
    );
    endpoints.MapControllerRoute(
        name: "registration",
        pattern: "{controller=Register}/{action=Index}/{id?}"
    );
    endpoints.MapControllerRoute(
        name: "authorization",
        pattern: "{controller=Authorization}/{action=Index}/{id?}"
    );
    endpoints.MapControllerRoute(
        name: "courses",
        pattern: "{controller=Courses}/{action=Index}/{id?}"
    );
    endpoints.MapControllerRoute(
        name: "teacher_account",
        pattern: "{controller=TeacherAccount}/{action=Index}/{id?}"
    );
    endpoints.MapControllerRoute(
        name: "create_course",
        pattern: "{controller=CreateCourse}/{action=Index}/{id?}"
    );
    endpoints.MapControllerRoute(
        name: "my_courses",
        pattern: "{controller=MyCourses}/{action=Index}/{id?}"
    );
});

app.Run();
