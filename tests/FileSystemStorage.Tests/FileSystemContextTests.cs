// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using CopilotChat.WebApi.Storage;
using Xunit;

namespace FileSystemStorage.Tests;

public sealed class FileSystemContextTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "copilot-storage-tests-" + Guid.NewGuid());

    [Theory]
    [InlineData(4096, 8)]
    [InlineData(8, 4096)]
    public async Task UpdatedEntityCanBeReloadedWhenSerializedSizeChanges(int initialLength, int updatedLength)
    {
        var context = this.OpenContext();
        var entity = new TestEntity { Id = "first", Content = new string('X', initialLength) };
        await context.CreateAsync(entity);
        // Reopen an existing store, as the application does after a restart.
        context = this.OpenContext();
        entity.Content = new string('Y', updatedLength);

        await context.UpsertAsync(entity);

        var reloaded = this.OpenContext();
        var saved = await reloaded.ReadAsync(entity.Id, entity.Partition);
        Assert.Equal(entity.Content, saved.Content);
        Assert.Single(await reloaded.QueryEntitiesAsync(_ => true));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeletingAnEntityLeavesReloadableStorage(bool deleteLastEntity)
    {
        var context = this.OpenContext();
        var removed = new TestEntity { Id = "removed", Content = new string('X', 4096) };
        await context.CreateAsync(removed);
        if (!deleteLastEntity)
        {
            await context.CreateAsync(new TestEntity { Id = "remaining", Content = "keep this" });
        }

        context = this.OpenContext();
        await context.DeleteAsync(removed);

        var reloaded = this.OpenContext();
        var entities = await reloaded.QueryEntitiesAsync(_ => true);
        if (deleteLastEntity)
        {
            Assert.Empty(entities);
        }
        else
        {
            var saved = Assert.Single(entities);
            Assert.Equal("remaining", saved.Id);
            Assert.Equal("keep this", saved.Content);
        }
    }

    private FileSystemContext<TestEntity> OpenContext() =>
        new(new FileInfo(Path.Combine(this._directory, "entities.json")));

    public void Dispose()
    {
        if (Directory.Exists(this._directory))
        {
            Directory.Delete(this._directory, recursive: true);
        }
    }

    public sealed class TestEntity : IStorageEntity
    {
        public string Id { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        [JsonIgnore]
        public string Partition => this.Id;
        public object Clone() => this.MemberwiseClone();
    }
}
