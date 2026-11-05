// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using CopilotChat.WebApi.Auth;
using CopilotChat.WebApi.Controllers;
using CopilotChat.WebApi.Hubs;
using CopilotChat.WebApi.Models;
using CopilotChat.WebApi.Models.Request;
using CopilotChat.WebApi.Models.Response;
using CopilotChat.WebApi.Models.Storage;
using CopilotChat.WebApi.Options;
using CopilotChat.WebApi.Services;
using CopilotChat.WebApi.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.KernelMemory;
using Moq;
using Xunit;

namespace CopilotChatWebApi.Tests;

public sealed class MetapromptPersistenceTests : IDisposable
{
    private const string Description = "TEST_ONLY_SYSTEM_DESCRIPTION";
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "copilot-metaprompt-tests-" + Guid.NewGuid());
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SanitizingAResponseDoesNotModifyItsSource(bool disclose)
    {
        var original = new ChatSession("Test", Description)
        {
            CtfdAuth = new CtfdAuthApi { Cookie = "test-cookie", Nonce = "test-nonce" },
            EnabledPlugins = new HashSet<string> { "test-plugin" },
            RagDocument = "test-document", RagUserInput = "test-input",
            IsLocked = true, MaxTurnReached = true, XssTriggered = true, MemoryBalance = 0.25F
        };
        var snapshot = JsonSerializer.Serialize(original);

        var response = Sanitizer(disclose).ChatSession(original);

        Assert.NotSame(original, response);
        Assert.Equal(snapshot, JsonSerializer.Serialize(original));
        Assert.Equal("test-cookie", original.CtfdAuth?.Cookie);
        Assert.Equal("test-nonce", original.CtfdAuth?.Nonce);
        Assert.Equal(disclose ? Description : "", response.SystemDescription);
        Assert.Null(response.CtfdAuth);
        Assert.Equal(original.Id, response.Id);
        Assert.Equal(original.EnabledPlugins, response.EnabledPlugins);
        Assert.Equal(original.RagDocument, response.RagDocument);
        Assert.Equal(original.RagUserInput, response.RagUserInput);
        Assert.Equal(original.MemoryBalance, response.MemoryBalance);
        Assert.Equal(original.IsLocked, response.IsLocked);
        Assert.Equal(original.MaxTurnReached, response.MaxTurnReached);
        Assert.Equal(original.XssTriggered, response.XssTriggered);
    }

    [Fact]
    public void SessionClonePreservesAllSerializedFields()
    {
        var original = new ChatSession("Test", Description)
        {
            CreatedOn = DateTimeOffset.Parse("2025-01-02T03:04:05+00:00"),
            MemoryBalance = 0.25F, EnabledPlugins = new HashSet<string> { "test-plugin" },
            Version = "test-version", IsDeleted = true, IsLocked = true,
            MaxTurnReached = true, XssTriggered = true, RagDocument = "document",
            RagUserInput = "input", CtfdAuth = new CtfdAuthApi { Cookie = "cookie", Nonce = "nonce" }
        };
        var clone = Assert.IsType<ChatSession>(original.Clone());
        Assert.Equal(JsonSerializer.Serialize(original), JsonSerializer.Serialize(clone));
        Assert.NotSame(original.EnabledPlugins, clone.EnabledPlugins);
        Assert.NotSame(original.CtfdAuth, clone.CtfdAuth);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SanitizingAStoredSessionPreservesItAcrossReadAndReload(bool disclose)
    {
        var storage = Context<ChatSession>("sessions");
        var original = new ChatSession("Test", Description)
        {
            CtfdAuth = new CtfdAuthApi { Cookie = "test-cookie", Nonce = "test-nonce" }
        };
        await storage.CreateAsync(original);
        Sanitizer(disclose).ChatSession(await storage.ReadAsync(original.Id, original.Id));
        Assert.Equal(Description, (await storage.ReadAsync(original.Id, original.Id)).SystemDescription);
        Assert.NotNull((await storage.ReadAsync(original.Id, original.Id)).CtfdAuth);

        // Saving a second record serializes the complete dictionary. It also
        // keeps this reproduction independent of the separate truncation bug.
        await storage.CreateAsync(new ChatSession("Second", "second description"));
        var reloaded = Context<ChatSession>("sessions");
        var persisted = await reloaded.ReadAsync(original.Id, original.Id);
        Assert.Equal(Description, persisted.SystemDescription);
        Assert.Equal("test-cookie", persisted.CtfdAuth!.Cookie);
    }

    [Fact]
    public async Task CreatingAndReadingAHiddenChatDoesNotEraseItsStoredDescription()
    {
        var fixture = Controller(false);
        var result = Assert.IsType<CreatedAtRouteResult>(await fixture.Controller.CreateChatSessionAsync(new CreateChatParameters { Title = "Test" }));
        var created = Assert.IsType<CreateChatResponse>(result.Value);
        var id = Guid.Parse(created.ChatSession.Id);
        Assert.Empty(created.ChatSession.SystemDescription);
        Assert.Equal(Description, (await fixture.Storage.ReadAsync(id.ToString(), id.ToString())).SystemDescription);

        var item = Assert.IsType<OkObjectResult>(await fixture.Controller.GetChatSessionByIdAsync(id));
        Assert.Empty(Assert.IsType<ChatSession>(item.Value).SystemDescription);
        var all = Assert.IsType<OkObjectResult>(await fixture.Controller.GetAllChatSessionsAsync());
        var listed = Assert.Single(Assert.IsAssignableFrom<IEnumerable<ChatSession>>(all.Value));
        Assert.Equal(id.ToString(), listed.Id);
        Assert.Empty(listed.SystemDescription);

        // In addition to disk storage, check the repository's cached object.
        Assert.Equal(Description, (await fixture.Sessions.FindByIdAsync(id.ToString())).SystemDescription);
        Assert.Equal(Description, (await fixture.Storage.ReadAsync(id.ToString(), id.ToString())).SystemDescription);
    }

    [Theory]
    [InlineData(false, "", Description)]
    [InlineData(false, null, Description)]
    [InlineData(false, "client replacement", "client replacement")]
    [InlineData(true, "", "")]
    [InlineData(true, null, Description)]
    [InlineData(true, "client replacement", "client replacement")]
    public async Task EditingAChatPreservesInstructionsAndRedactsNotifications(bool disclose, string? submittedDescription, string expectedStored)
    {
        var fixture = Controller(disclose);
        var session = new ChatSession("Old title", Description)
        {
            CtfdAuth = new CtfdAuthApi { Cookie = "test-cookie", Nonce = "test-nonce" }
        };
        await fixture.Sessions.CreateAsync(session);
        var proxy = new Mock<IClientProxy>();
        ChatSession? notification = null;
        proxy.Setup(p => p.SendCoreAsync(ChatHistoryController.ChatEditedClientCall, It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Callback<string, object[], CancellationToken>((_, args, _) => notification = Assert.IsType<ChatSession>(args[0]))
            .Returns(Task.CompletedTask);
        var clients = new Mock<IHubClients>();
        clients.Setup(c => c.Group(session.Id)).Returns(proxy.Object);
        var hub = new Mock<IHubContext<MessageRelayHub>>();
        hub.SetupGet(h => h.Clients).Returns(clients.Object);

        var result = Assert.IsType<OkObjectResult>(await fixture.Controller.EditChatSessionAsync(hub.Object,
            new EditChatParameters { Title = "New title", SystemDescription = submittedDescription, MemoryBalance = 0.25F }, Guid.Parse(session.Id)));

        var stored = await fixture.Storage.ReadAsync(session.Id, session.Id);
        Assert.Equal(expectedStored, stored.SystemDescription);
        Assert.Equal(expectedStored, (await fixture.Sessions.FindByIdAsync(session.Id)).SystemDescription);
        Assert.Equal("New title", stored.Title);
        Assert.Equal(0.25F, stored.MemoryBalance);
        Assert.Equal("test-cookie", stored.CtfdAuth?.Cookie);
        Assert.Equal("test-nonce", stored.CtfdAuth?.Nonce);
        Assert.Equal("test-cookie", (await fixture.Sessions.FindByIdAsync(session.Id)).CtfdAuth?.Cookie);
        Assert.Equal("test-nonce", (await fixture.Sessions.FindByIdAsync(session.Id)).CtfdAuth?.Nonce);
        var response = Assert.IsType<ChatSession>(result.Value);
        Assert.Equal(disclose ? expectedStored : "", response.SystemDescription);
        Assert.Null(response.CtfdAuth);
        Assert.NotNull(notification);
        Assert.Equal(disclose ? expectedStored : "", notification!.SystemDescription);
        Assert.Null(notification.CtfdAuth);
        proxy.Verify(p => p.SendCoreAsync(ChatHistoryController.ChatEditedClientCall, It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static MetapromptSanitizationService Sanitizer(bool disclose) =>
        new(Options.Create(new ChallengeOptions { MetapromptLeak = disclose }));

    private FileSystemContext<T> Context<T>(string name) where T : IStorageEntity =>
        new(new FileInfo(Path.Combine(this._directory, name)));

    private (ChatHistoryController Controller, ChatSessionRepository Sessions, FileSystemContext<ChatSession> Storage) Controller(bool disclose)
    {
        var storage = Context<ChatSession>("sessions");
        var sessions = new ChatSessionRepository(storage, this._cache);
        var messages = new ChatMessageRepository(Context<CopilotChatMessage>("messages"), this._cache);
        var participants = new ChatParticipantRepository(Context<ChatParticipant>("participants"), this._cache);
        var sources = new ChatMemorySourceRepository(Context<MemorySource>("sources"), this._cache);
        var auth = Mock.Of<IAuthInfo>(a => a.UserId == "test-user" && a.Name == "Test user");
        var controller = new ChatHistoryController(NullLogger<ChatHistoryController>.Instance,
            Mock.Of<IKernelMemory>(), sessions, messages, participants, sources,
            Options.Create(new PromptsOptions { SystemDescription = Description, InitialBotMessage = "Test greeting" }),
            Sanitizer(disclose), new Dictionary<string, Plugin>(), Options.Create(new ChallengeOptions { MetapromptLeak = disclose }),
            auth, Mock.Of<IPrometheusTelemetryService>());
        return (controller, sessions, storage);
    }

    public void Dispose()
    {
        this._cache.Dispose();
        if (Directory.Exists(this._directory))
        {
            Directory.Delete(this._directory, recursive: true);
        }
    }
}
