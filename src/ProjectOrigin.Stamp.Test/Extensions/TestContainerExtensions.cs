using System.Net;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Xunit.Abstractions;

namespace ProjectOrigin.Stamp.Test.Extensions;

public static class TestContainerExtensions
{
    public static async Task StartAsyncWithLogsAsync(this IContainer container, ITestOutputHelper? outputHelper = null)
    {
        try
        {
            await container.StartAsync();
        }
        catch (Exception e)
        {
            var (stdout, stderr) = await container.GetLogsAsync();

            outputHelper?.WriteLine($"Container logs: Stdout: {stdout}\nStderr:{stderr}\nError: {e}");
            throw;
        }

    }

    public static IWaitForContainerOS UntilGrpcEndpointIsReady(this IWaitForContainerOS container, ushort grpcPort, string path, Action<IWaitStrategy>? waitStrategyModifier = null)
    {
        // This is a workaround to check if a grpc endpoint is ready.
        // GRPC uses http2 requests which are not supported as a WaitStrategy.
        // However, the message 'An HTTP/1.x request was sent to an HTTP/2 only endpoint.' indicates that endpoint is actually ready.

        // Default timeout is 1 minute
        waitStrategyModifier ??= ws => ws.WithTimeout(TimeSpan.FromMinutes(1));

        return container.UntilHttpRequestIsSucceeded(s =>
                    s.ForPath(path)
                        .ForPort(grpcPort)
                        .ForStatusCode(HttpStatusCode.BadRequest)
                        .ForResponseMessageMatching(async r =>
                            {
                                var content = await r.Content.ReadAsStringAsync();
                                var isHttp2ServerReady = "An HTTP/1.x request was sent to an HTTP/2 only endpoint.".Equals(content);
                                return isHttp2ServerReady;
                            })
                    , waitStrategyModifier
        );
    }
}
