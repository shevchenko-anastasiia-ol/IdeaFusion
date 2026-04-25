namespace GrpcClients.Models;

public record ValidatedTokenResult(
    bool   IsValid,
    string UserId,
    string UserName,
    string Email);