using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace ApiGateway.Transforms;

public class CorrelationIdTransformProvider : ITransformProvider
{
    private const string Header = "X-Correlation-Id";
    private const string ItemKey = "CorrelationId";

    public void ValidateRoute(TransformRouteValidationContext context)
    {
        // no validation required
    }

    public void ValidateCluster(TransformClusterValidationContext context)
    {
        // no validation required
    }

    public void Apply(TransformBuilderContext context)
    {
        context.AddRequestTransform(async transformContext =>
        {
            var httpContext = transformContext.HttpContext;

            var correlationId =
                httpContext.Items[Header]?.ToString() ??
                httpContext.Items[ItemKey]?.ToString();

            if (!string.IsNullOrWhiteSpace(correlationId))
            {
                transformContext.ProxyRequest.Headers.Remove(Header);
                transformContext.ProxyRequest.Headers.Add(Header, correlationId);
            }
        });
    }
}