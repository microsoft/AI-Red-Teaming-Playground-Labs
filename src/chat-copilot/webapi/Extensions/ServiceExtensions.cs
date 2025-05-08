// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using CopilotChat.Shared;
using CopilotChat.WebApi.Auth;
using CopilotChat.WebApi.Models.Storage;
using CopilotChat.WebApi.Options;
using CopilotChat.WebApi.Plugins.Chat;
using CopilotChat.WebApi.Services;
using CopilotChat.WebApi.Services.Ctfd;
using CopilotChat.WebApi.Services.MemoryMigration;
using CopilotChat.WebApi.Storage;
using CopilotChat.WebApi.Utilities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Microsoft.KernelMemory;
using Microsoft.SemanticKernel.Diagnostics;
using StackExchange.Redis;

namespace CopilotChat.WebApi.Extensions;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/>.
/// Add options and services for Chat Copilot.
/// </summary>
public static class CopilotChatServiceExtensions
{
    /// <summary>
    /// Parse configuration into options.
    /// </summary>
    public static IServiceCollection AddOptions(this IServiceCollection services, ConfigurationManager configuration)
    {
        // General configuration
        AddOptions<ServiceOptions>(ServiceOptions.PropertyName);

        // Authentication configuration
        AddOptions<ChatAuthenticationOptions>(ChatAuthenticationOptions.PropertyName);

        // Chat storage configuration
        AddOptions<ChatStoreOptions>(ChatStoreOptions.PropertyName);

        // Azure speech token configuration
        AddOptions<AzureSpeechOptions>(AzureSpeechOptions.PropertyName);

        AddOptions<DocumentMemoryOptions>(DocumentMemoryOptions.PropertyName);

        // Chat prompt options
        AddOptions<PromptsOptions>(PromptsOptions.PropertyName);

        AddOptions<PlannerOptions>(PlannerOptions.PropertyName);

        AddOptions<ContentSafetyOptions>(ContentSafetyOptions.PropertyName);

        AddOptions<KernelMemoryConfig>(MemoryConfiguration.KernelMemorySection);

        AddOptions<FrontendOptions>(FrontendOptions.PropertyName);

        //OpenTelemetry configuration
        AddOptions<PrometheusTelemetryOptions>(PrometheusTelemetryOptions.PropertyName);

        return services;

        void AddOptions<TOptions>(string propertyName)
            where TOptions : class
        {
            services.AddOptions<TOptions>(configuration.GetSection(propertyName));
        }
    }

    internal static void AddOptions<TOptions>(this IServiceCollection services, IConfigurationSection section)
        where TOptions : class
    {
        services.AddOptions<TOptions>()
            .Bind(section)
            .ValidateDataAnnotations()
            .ValidateOnStart()
            .PostConfigure(TrimStringProperties);
    }

    internal static IServiceCollection AddUtilities(this IServiceCollection services)
    {
        return services.AddScoped<AskConverter>();
    }

    internal static IServiceCollection AddChallenge(this IServiceCollection services, IConfiguration configuration)
    {
        //Set the settings here because the settings are needed for the authorization policies.
        var settingsSection = configuration.GetSection(ChallengeOptions.PropertyName);
        var settings = settingsSection.Get<ChallengeOptions>() ?? new ChallengeOptions();

        services.AddOptions<ChallengeOptions>(settingsSection);

        services.AddSingleton<IMetapromptSanitizationService, MetapromptSanitizationService>();

        //We add a redis connection only if a CTFd integration is enabled
        if (settings.Ctfd != null)
        {
            // Add the following code inside the AddChallenge method
            services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(settings.Ctfd.RedisConnectionString));
            services.AddSingleton<IItsDangerousSignerService, ItsDangerousSignerService>();

            if (settings.AuthType != AuthType.CTFd)
            {
                throw new InvalidOperationException("CTFd settings are present but the authentication type is not CTFd.");
            }
        }

        // We check if ChallengeHome settings are set
        if (settings.ChallengeHome != null)
        {
            services.AddSingleton<IItsDangerousSignerService, ItsDangerousSignerService>();
            if (settings.AuthType != AuthType.ChallengeHome)
            {
                throw new InvalidOperationException("ChallengeHome settings are present but the authentication type is not ChallengeHome.");
            }
        }

        // We check if the secret key is set when the authentication type is ChallengeHome
        if (settings.AuthType == AuthType.ChallengeHome)
        {
            if (string.IsNullOrEmpty(settings.ChallengeHome?.SecretKey))
            {
                throw new InvalidOperationException("ChallengeHome SecretKey is required when AuthType is ChallengeHome.");
            }
        }


        var plugins = configuration.GetSection("Plugins").Get<List<Plugin>>() ?? new List<Plugin>();

        //Set the authorization framework for the flag system.
        services.AddAuthorization(o =>
        {
            //Add authentication policies for the flags in appsettings.json
            o.AddPolicy(AuthzChallenge.Upload, p => p.RequireAssertion(c => settings.Upload));
            o.AddPolicy(AuthzChallenge.PluginsControl, p => p.RequireAssertion(c => settings.PluginsControl));
            o.AddPolicy(AuthzChallenge.Plugins, p => p.RequireAssertion(c => plugins.Count > 0));
            o.AddPolicy(AuthzChallenge.HumanScorer, p => p.RequireAssertion(c => settings.HumanScorer != null));
            o.AddPolicy(AuthzChallenge.XssVulnerable, p => p.RequireAssertion(c => settings.XssVulnerable));
            o.AddPolicy(AuthzChallenge.RagInput, p => p.RequireAssertion(c => settings.RagInput != null));
        });

        services.AddSingleton<Scorer>();

        //Set the open telemetry metrics
        services.AddSingleton<IPrometheusTelemetryService, PrometheusTelemetryService>();
        services.AddSingleton<ISessionMetricService, SessionMetricService>();

        //Add the ctfd services
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, CtfdAuthorizationMiddlewareResultHandler>();
        services.AddSingleton<ICtfdFlagSubmissionService, CtfdFlagSubmissionService>();
        services.AddSingleton<ICtfdScorerMessageService, CtfdScorerMessageService>();

        services.AddSingleton<IRagService, RagService>();
        return services;
    }

    internal static IServiceCollection AddPlugins(this IServiceCollection services, IConfiguration configuration)
    {
        var plugins = configuration.GetSection("Plugins").Get<List<Plugin>>() ?? new List<Plugin>();
        var logger = services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();
        logger.LogDebug("Found {0} plugins.", plugins.Count);

        // Validate the plugins
        // TODO don't load external plugins only internal one.
        Dictionary<string, Plugin> validatedPlugins = new();
        foreach (Plugin plugin in plugins)
        {
            if (validatedPlugins.ContainsKey(plugin.Name))
            {
                logger.LogWarning("Plugin '{0}' is defined more than once. Skipping...", plugin.Name);
                continue;
            }

            var pluginManifestUrl = PluginUtils.GetPluginManifestUri(plugin.ManifestDomain);
            using var request = new HttpRequestMessage(HttpMethod.Get, pluginManifestUrl);
            // Need to set the user agent to avoid 403s from some sites.
            request.Headers.Add("User-Agent", Telemetry.HttpUserAgent);
            try
            {
                logger.LogInformation("Adding plugin: {0}.", plugin.Name);
                using var httpClient = new HttpClient();
                var response = httpClient.SendAsync(request).Result;
                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException($"Plugin '{plugin.Name}' at '{pluginManifestUrl}' returned status code '{response.StatusCode}'.");
                }
                validatedPlugins.Add(plugin.Name, plugin);
                logger.LogInformation("Added plugin: {0}.", plugin.Name);
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is AggregateException)
            {
                logger.LogWarning(ex, "Plugin '{0}' at {1} responded with error. Skipping...", plugin.Name, pluginManifestUrl);
            }
            catch (Exception ex) when (ex is UriFormatException)
            {
                logger.LogWarning("Plugin '{0}' at {1} is not a valid URL. Skipping...", plugin.Name, pluginManifestUrl);
            }
        }

        // Add the plugins
        services.AddSingleton<IDictionary<string, Plugin>>(validatedPlugins);

        return services;
    }

    internal static IServiceCollection AddMaintenanceServices(this IServiceCollection services)
    {
        // Inject migration services
        services.AddSingleton<IChatMigrationMonitor, ChatMigrationMonitor>();
        services.AddSingleton<IChatMemoryMigrationService, ChatMemoryMigrationService>();

        // Inject actions so they can be part of the action-list.
        services.AddSingleton<ChatMigrationMaintenanceAction>();
        services.AddSingleton<IReadOnlyList<IMaintenanceAction>>(
            sp =>
                (IReadOnlyList<IMaintenanceAction>)
                new[]
                {
                    sp.GetRequiredService<ChatMigrationMaintenanceAction>(),
                });

        return services;
    }

    /// <summary>
    /// Add CORS settings.
    /// </summary>
    internal static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        string[] allowedOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        if (allowedOrigins.Length > 0)
        {
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(
                    policy =>
                    {
                        policy.WithOrigins(allowedOrigins)
                            .WithMethods("POST", "GET", "PUT", "DELETE", "PATCH")
                            .AllowAnyHeader()
                            .AllowCredentials(); //For cookies
                    });
            });
        }

        return services;
    }

    /// <summary>
    /// Add persistent chat store services.
    /// </summary>
    public static IServiceCollection AddPersistentChatStore(this IServiceCollection services)
    {
        IStorageContext<ChatSession> chatSessionStorageContext;
        IStorageContext<CopilotChatMessage> chatMessageStorageContext;
        IStorageContext<MemorySource> chatMemorySourceStorageContext;
        IStorageContext<ChatParticipant> chatParticipantStorageContext;
        var sp = services.BuildServiceProvider();

        ChatStoreOptions chatStoreConfig = sp.GetRequiredService<IOptions<ChatStoreOptions>>().Value;

        switch (chatStoreConfig.Type)
        {
            case ChatStoreOptions.ChatStoreType.Volatile:
                {
                    chatSessionStorageContext = new VolatileContext<ChatSession>();
                    chatMessageStorageContext = new VolatileContext<CopilotChatMessage>();
                    chatMemorySourceStorageContext = new VolatileContext<MemorySource>();
                    chatParticipantStorageContext = new VolatileContext<ChatParticipant>();
                    break;
                }

            case ChatStoreOptions.ChatStoreType.Filesystem:
                {
                    if (chatStoreConfig.Filesystem == null)
                    {
                        throw new InvalidOperationException("ChatStore:Filesystem is required when ChatStore:Type is 'Filesystem'");
                    }

                    string fullPath = Path.GetFullPath(chatStoreConfig.Filesystem.FilePath);
                    string directory = Path.GetDirectoryName(fullPath) ?? string.Empty;
                    chatSessionStorageContext = new FileSystemContext<ChatSession>(
                        new FileInfo(Path.Combine(directory, $"{Path.GetFileNameWithoutExtension(fullPath)}_sessions{Path.GetExtension(fullPath)}")));
                    chatMessageStorageContext = new FileSystemContext<CopilotChatMessage>(
                        new FileInfo(Path.Combine(directory, $"{Path.GetFileNameWithoutExtension(fullPath)}_messages{Path.GetExtension(fullPath)}")));
                    chatMemorySourceStorageContext = new FileSystemContext<MemorySource>(
                        new FileInfo(Path.Combine(directory, $"{Path.GetFileNameWithoutExtension(fullPath)}_memorysources{Path.GetExtension(fullPath)}")));
                    chatParticipantStorageContext = new FileSystemContext<ChatParticipant>(
                        new FileInfo(Path.Combine(directory, $"{Path.GetFileNameWithoutExtension(fullPath)}_participants{Path.GetExtension(fullPath)}")));
                    break;
                }

            case ChatStoreOptions.ChatStoreType.Cosmos:
                {
                    if (chatStoreConfig.Cosmos == null)
                    {
                        throw new InvalidOperationException("ChatStore:Cosmos is required when ChatStore:Type is 'Cosmos'");
                    }
#pragma warning disable CA2000 // Dispose objects before losing scope - objects are singletons for the duration of the process and disposed when the process exits.
                    chatSessionStorageContext = new CosmosDbContext<ChatSession>(
                        chatStoreConfig.Cosmos.ConnectionString, chatStoreConfig.Cosmos.Database, chatStoreConfig.Cosmos.ChatSessionsContainer, chatStoreConfig.Cosmos.IsEndpoint);
                    chatMessageStorageContext = new CosmosDbContext<CopilotChatMessage>(
                        chatStoreConfig.Cosmos.ConnectionString, chatStoreConfig.Cosmos.Database, chatStoreConfig.Cosmos.ChatMessagesContainer, chatStoreConfig.Cosmos.IsEndpoint);
                    chatMemorySourceStorageContext = new CosmosDbContext<MemorySource>(
                        chatStoreConfig.Cosmos.ConnectionString, chatStoreConfig.Cosmos.Database, chatStoreConfig.Cosmos.ChatMemorySourcesContainer, chatStoreConfig.Cosmos.IsEndpoint);
                    chatParticipantStorageContext = new CosmosDbContext<ChatParticipant>(
                        chatStoreConfig.Cosmos.ConnectionString, chatStoreConfig.Cosmos.Database, chatStoreConfig.Cosmos.ChatParticipantsContainer, chatStoreConfig.Cosmos.IsEndpoint);
#pragma warning restore CA2000 // Dispose objects before losing scope
                    break;
                }

            default:
                {
                    throw new InvalidOperationException(
                        "Invalid 'ChatStore' setting 'chatStoreConfig.Type'.");
                }
        }

        var memoryCache = sp.GetRequiredService<IMemoryCache>();

        services.AddSingleton<ChatSessionRepository>(new ChatSessionRepository(chatSessionStorageContext, memoryCache!));
        services.AddSingleton<ChatMessageRepository>(new ChatMessageRepository(chatMessageStorageContext, memoryCache!));
        services.AddSingleton<ChatMemorySourceRepository>(new ChatMemorySourceRepository(chatMemorySourceStorageContext, memoryCache!));
        services.AddSingleton<ChatParticipantRepository>(new ChatParticipantRepository(chatParticipantStorageContext, memoryCache!));

        return services;
    }

    /// <summary>
    /// Add authorization services
    /// </summary>
    public static IServiceCollection AddCopilotChatAuthorization(this IServiceCollection services)
    {
        return services.AddScoped<IAuthorizationHandler, ChatParticipantAuthorizationHandler>()
            .AddAuthorizationCore(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                options.AddPolicy(AuthPolicyName.RequireChatParticipant, builder =>
                {
                    builder.RequireAuthenticatedUser()
                        .AddRequirements(new ChatParticipantRequirement());
                });
            });
    }

    /// <summary>
    /// Add authentication services
    /// </summary>
    public static IServiceCollection AddCopilotChatAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IAuthInfo, AuthInfo>();
        var config = services.BuildServiceProvider().GetRequiredService<IOptions<ChatAuthenticationOptions>>().Value;
        switch (config.Type)
        {
            case ChatAuthenticationOptions.AuthenticationType.AzureAd:
                services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddMicrosoftIdentityWebApi(configuration.GetSection($"{ChatAuthenticationOptions.PropertyName}:AzureAd"));
                break;

            case ChatAuthenticationOptions.AuthenticationType.None:
                services.AddAuthentication(PassThroughAuthenticationHandler.AuthenticationScheme)
                    .AddScheme<AuthenticationSchemeOptions, PassThroughAuthenticationHandler>(
                        authenticationScheme: PassThroughAuthenticationHandler.AuthenticationScheme,
                        configureOptions: null);
                break;

            default:
                throw new InvalidOperationException($"Invalid authentication type '{config.Type}'.");
        }

        return services;
    }

    /// <summary>
    /// Trim all string properties, recursively.
    /// </summary>
    private static void TrimStringProperties<T>(T options) where T : class
    {
        Queue<object> targets = new();
        targets.Enqueue(options);

        while (targets.Count > 0)
        {
            object target = targets.Dequeue();
            Type targetType = target.GetType();
            foreach (PropertyInfo property in targetType.GetProperties())
            {
                // Skip enumerations
                if (property.PropertyType.IsEnum)
                {
                    continue;
                }

                // Skip index properties
                if (property.GetIndexParameters().Length == 0)
                {
                    continue;
                }

                // Property is a built-in type, readable, and writable.
                if (property.PropertyType.Namespace == "System" &&
                    property.CanRead &&
                    property.CanWrite)
                {
                    // Property is a non-null string.
                    if (property.PropertyType == typeof(string) &&
                        property.GetValue(target) != null)
                    {
                        property.SetValue(target, property.GetValue(target)!.ToString()!.Trim());
                    }
                }
                else
                {
                    // Property is a non-built-in and non-enum type - queue it for processing.
                    if (property.GetValue(target) != null)
                    {
                        targets.Enqueue(property.GetValue(target)!);
                    }
                }
            }
        }
    }
}
