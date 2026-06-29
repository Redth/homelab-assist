using System.ComponentModel;
using ModelContextProtocol.Server;

namespace NpmMcp.Tools;

[McpServerToolType]
internal sealed class CertificateTools(NpmClient client)
{
    [McpServerTool(Name = "npm_list_certificates"),
     Description("List all SSL certificates known to nginx-proxy-manager (Let's Encrypt and custom), including their domains, provider, and expiry. Useful for picking a certificate_id when creating/updating a proxy host or for spotting expiring certs. Read-only.")]
    public Task<string> ListCertificates(CancellationToken ct = default)
        => client.GetAsync("nginx/certificates?expand=owner", ct);

    [McpServerTool(Name = "npm_get_certificate"),
     Description("Get a single certificate by its numeric id, with full detail (domains, expiry, provider). Read-only.")]
    public Task<string> GetCertificate(
        [Description("Numeric certificate id")] int id,
        CancellationToken ct = default)
        => client.GetAsync($"nginx/certificates/{id}", ct);
}
