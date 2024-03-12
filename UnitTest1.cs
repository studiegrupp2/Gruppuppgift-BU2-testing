using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace TestProject1;

public class ApplicationFactory : WebApplicationFactory<Gruppuppgift_BU2.Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.AddDbContext<Gruppuppgift_BU2.ApplicationContext>(options =>
            {
                var path = Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData
                );
                options.UseSqlite($"Data Source={Path.Join(path, "TestDb.db")}");
            });

            services
                .AddAuthentication("TestScheme")
                .AddScheme<AuthenticationSchemeOptions, MyAuthHandler>(
                    "TestScheme",
                    options => { }
                );

            var context = CreateDbContext(services);

            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            CreateFakeUser(context);
        });
    }

    public static Gruppuppgift_BU2.ApplicationContext CreateDbContext(IServiceCollection services)
    {
        var provider = services.BuildServiceProvider();

        var scope = provider.CreateScope();
        return scope.ServiceProvider.GetRequiredService<Gruppuppgift_BU2.ApplicationContext>();
    }

    public static void CreateFakeUser(Gruppuppgift_BU2.ApplicationContext context)
    {
        Gruppuppgift_BU2.User user = new Gruppuppgift_BU2.User();
        user.Id = "test-user-id";
        user.Email = "test user";

        context.Users.Add(user);
        context.SaveChanges();
    }
}

public class MyAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public MyAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock
    )
        : base(options, logger, encoder, clock) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "test user"),
            new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
            new Claim(ClaimTypes.Role, "manager")
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "TestScheme");

        var result = AuthenticateResult.Success(ticket);
        return Task.FromResult(result);
    }
}
public class Test1 : IClassFixture<ApplicationFactory>
{
    ApplicationFactory factory;
    public Test1(ApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task CreateProduct()
    {
        //given
        var client = factory.CreateClient();
        var title = "Byxor";
        var description = "tighta";
        var category = "byxor";
        var size = "medium";
        var color = "blue";
        var price = 259.99;
        var dto = new Gruppuppgift_BU2.CreateProductDto(title, description, category, size, color, price);

        var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<Gruppuppgift_BU2.ApplicationContext>();
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        ApplicationFactory.CreateFakeUser(context);

        //when
        var request = await client.PostAsJsonAsync("/manager/create", dto);
        var response = await request.Content.ReadFromJsonAsync<Gruppuppgift_BU2.Product>();

        //then
        request.EnsureSuccessStatusCode();
        Assert.NotNull(response);
        Assert.Equal(title, response.Title);
        Assert.Equal(description, response.Description);
        Assert.Equal(category, response.Category);
        Assert.Equal(size, response.Size);
        Assert.Equal(color, response.Color);
        Assert.True(response.Id >= 0);
    }

    [Fact]
    public async Task RemoveProduct()
    {
        // given
        var client = factory.CreateClient();
        var title = "Byxor";
        var description = "tighta";
        var category = "byxor";
        var size = "medium";
        var color = "blue";
        var price = 259.99;

        // Rensa databasen för testet.
        var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<Gruppuppgift_BU2.ApplicationContext>();
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        ApplicationFactory.CreateFakeUser(context);

        var ProductService = scope.ServiceProvider.GetRequiredService<Gruppuppgift_BU2.ProductService>();

        ProductService.CreateProduct(title, description, category, size, color, price);

        // when
        var request = await client.DeleteAsync("/manager/delete/1");
        var response = await request.Content.ReadFromJsonAsync<Gruppuppgift_BU2.Product>();

        // then
        request.EnsureSuccessStatusCode();
        Assert.NotNull(response);
        Assert.Equal(title, response.Title);
        Assert.Equal(description, response.Description);
        Assert.Equal(category, response.Category);
        Assert.Equal(size, response.Size);
        Assert.Equal(color, response.Color);
        Assert.True(response.Id == 1);
    }
}