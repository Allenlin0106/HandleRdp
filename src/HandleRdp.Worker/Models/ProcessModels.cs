namespace HandleRdp.Worker.Models;

// Mirrors WebService.Api.Models.ProcessRequest/ProcessResponse — the shared
// wire contract carried as JSON in the MSMQ message body.
public record ProcessRequest(string Payload);

public record ProcessResponse(bool Success, string? Result, string? Error);
