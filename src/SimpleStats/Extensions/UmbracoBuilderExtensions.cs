using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SimpleStats.Configuration;
using SimpleStats.Data;
using SimpleStats.Jobs;
using SimpleStats.Middleware;
using SimpleStats.NotificationHandlers;
using SimpleStats.OpenApi;
using SimpleStats.Services;
using Umbraco.Cms.Api.Common.OpenApi;
using Umbraco.Cms.Api.Management.OpenApi;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Web.Common.ApplicationBuilder;
using Umbraco.Extensions;

namespace SimpleStats.Extensions;

/// <summary>
/// Extension methods for <see cref="IUmbracoBuilder"/> to register SimpleStats services.
/// </summary>
public static class UmbracoBuilderExtensions
{
    /// <summary>
    /// Registers all SimpleStats services and configuration with the Umbraco DI container.
    /// </summary>
    /// <param name="builder">The <see cref="IUmbracoBuilder"/> instance.</param>
    /// <returns>The same <see cref="IUmbracoBuilder"/> to allow chaining.</returns>
    public static IUmbracoBuilder AddSimpleStats(this IUmbracoBuilder builder)
    {
        builder.Services.AddOptions<SimpleStatsSettings>()
            .BindConfiguration(SimpleStatsSettings.SectionName);

        builder.Services.AddUmbracoDbContext<SimpleStatsContext>(
            (IServiceProvider serviceProvider, DbContextOptionsBuilder options, string? connectionString, string? providerName) =>
                options.UseUmbracoDatabaseProvider(serviceProvider),
            shareUmbracoConnection: true);

        builder.Services.AddSingleton<ISimpleStatsService, SimpleStatsService>();
        builder.Services.AddRecurringBackgroundJob<SimpleStatsCommitJob>();
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, EnsureSimpleStatsDatabaseCreated>();

        builder.AddBackOfficeOpenApiDocument(SimpleStatsApiConfiguration.ApiName, document => document
            .WithTitle("SimpleStats API")
            .WithBackOfficeAuthentication()
            .ConfigureOpenApiOptions(options => options.AddDocumentTransformer((openApiDocument, _, _) =>
            {
                openApiDocument.Info.Version = "1.0";
                openApiDocument.Info.Description = "API for reading SimpleStats page-view data.";
                return Task.CompletedTask;
            })));

        builder.Services.Configure<UmbracoPipelineOptions>(options =>
            options.AddFilter(new UmbracoPipelineFilter(nameof(SimpleStats))
            {
                PostPipeline = app => app.UseMiddleware<SimpleStatsMiddleware>(),
            }));

        return builder;
    }
}
