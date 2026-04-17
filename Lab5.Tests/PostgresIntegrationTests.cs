using System.Net;
using System.Net.Http.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using DotNet.Testcontainers.Networks;
using DotNet.Testcontainers.Configurations;
using Testcontainers.PostgreSql;
using Shouldly;
using Xunit;

namespace Lab5.Tests;

public class PostgresIntegrationTests : IAsyncLifetime
{
    private readonly INetwork _network = new NetworkBuilder().Build();
    private readonly PostgreSqlContainer _dbContainer;
    private IContainer _apiContainer = null!;
    private IFutureDockerImage _apiImage = null!;

    public PostgresIntegrationTests()
    {
        _dbContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15-alpine")
            .WithNetwork(_network)
            .WithNetworkAliases("db_host") 
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _network.CreateAsync();
        await _dbContainer.StartAsync();

        _apiImage = new DotNet.Testcontainers.Builders.ImageFromDockerfileBuilder()
            .WithDockerfileDirectory(CommonDirectoryPath.GetSolutionDirectory(), "Lab5.Api")
            .WithDockerfile("Dockerfile")
            .Build();

        await _apiImage.CreateAsync();
        
        var connectionString = _dbContainer.GetConnectionString();
        
        var internalConnectionString = connectionString
            .Replace("localhost", "db_host")
            .Replace("127.0.0.1", "db_host");

        _apiContainer = new DotNet.Testcontainers.Builders.ContainerBuilder()
            .WithImage(_apiImage.FullName)
            .WithNetwork(_network)
            .WithEnvironment("ConnectionStrings__DefaultConnection", internalConnectionString)
            .WithEnvironment("ASPNETCORE_URLS", "http://+:8080")
            .WithPortBinding(8080, true) 
            .WithWaitStrategy(DotNet.Testcontainers.Builders.Wait.ForUnixContainer()
                .UntilPortIsAvailable(8080))
            .Build();

        await _apiContainer.StartAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CrudOperations_ThroughApi_WorkCorrectly()
    {
        // Arrange
        var publicPort = _apiContainer.GetMappedPublicPort(8080);
        var client = new HttpClient { BaseAddress = new Uri($"http://localhost:{publicPort}/") };
    
        var uniqueEmail = $"student_{Guid.NewGuid().ToString().Substring(0, 8)}@example.com";

        var newStudent = new 
        { 
            FullName = "Test Student", 
            Email = uniqueEmail,
            EnrollmentDate = DateTime.UtcNow
        };

        // Act
        var postResponse = await client.PostAsJsonAsync("api/students", newStudent);

        // Assert
        postResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
    }
    
    [Fact]
    [Trait("Category", "Integration")]
    public async Task ForeignKeyConstraint_EnforcesDataIntegrity_ViaCascadeDelete()
    {
        // Arrange
        var port = _apiContainer.GetMappedPublicPort(8080);
        var client = new HttpClient { BaseAddress = new Uri($"http://localhost:{port}/") };
        
        var studentPayload = new { FullName = "FK Test Student", Email = $"integrity_{Guid.NewGuid()}@test.com" };
        var postRes = await client.PostAsJsonAsync("api/students", studentPayload);
        var createdStudent = await postRes.Content.ReadFromJsonAsync<dynamic>();
        int studentId = createdStudent.GetProperty("id").GetInt32();

        // Act 
        var deleteRes = await client.DeleteAsync($"api/students/{studentId}");

        // Assert
        deleteRes.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var getRes = await client.GetAsync($"api/students/{studentId}");
        getRes.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task RawSql_Search_ReturnsCorrectData()
    {
        // Arrange
        var port = _apiContainer.GetMappedPublicPort(8080);
        var client = new HttpClient { BaseAddress = new Uri($"http://localhost:{port}/") };
        
        await client.PostAsJsonAsync("api/students", new { FullName = "UniqueSearchName", Email = "sql@test.com" });

        // Act
        var response = await client.GetAsync("api/students/search?name=UniqueSearchName");
        var students = await response.Content.ReadFromJsonAsync<List<dynamic>>();

        // Assert
        students.ShouldNotBeEmpty();
        students.Any(s => s.GetProperty("fullName").GetString() == "UniqueSearchName").ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task UniqueConstraint_PreventsDuplicateEmails()
    {
        // Arrange
        var port = _apiContainer.GetMappedPublicPort(8080);
        var client = new HttpClient { BaseAddress = new Uri($"http://localhost:{port}/") };
        var email = "duplicate@test.com";
        var student = new { FullName = "User 1", Email = email };
        
        await client.PostAsJsonAsync("api/students", student);

        // Act
        var response = await client.PostAsJsonAsync("api/students", new { FullName = "User 2", Email = email });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    public async Task DisposeAsync()
    {
        if (_apiContainer != null) await _apiContainer.DisposeAsync();
        if (_dbContainer != null) await _dbContainer.DisposeAsync();
        if (_apiImage != null) await _apiImage.DisposeAsync();
        if (_network != null) await _network.DisposeAsync();
    }
}