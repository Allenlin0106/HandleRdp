# HandleRdp

Client B: the MSMQ consumer that receives Client A's requests (relayed by
`WebService`), processes them, and replies over MSMQ.

## Flow

1. Reads messages from the `clientA-to-clientB` MSMQ queue.
2. Processes the payload (see `Worker.Process` — currently a placeholder;
   replace with the real RDP handling logic).
3. Sends a reply to the `clientB-to-clientA` queue with `Message.CorrelationId`
   set to the original request's `Message.Id`, so `WebService` can match the
   reply back to the right waiting HTTP call.

See the `WebService` repo's README for the full end-to-end picture and
sequence diagram.

## Requirements

- Windows with the "Message Queuing" (MSMQ) feature installed
  (`Enable-WindowsOptionalFeature -Online -FeatureName MSMQ-Server`).
- .NET 8 SDK.
- `WebService` running (or at least configured) against the same two queue
  paths.

Queues are private (`.\Private$\...`) and are auto-created on first run by
either process if they don't already exist.

## Configuration

`src/HandleRdp.Worker/appsettings.json`:

```json
"Msmq": {
  "RequestQueuePath": ".\\Private$\\clientA-to-clientB",
  "ResponseQueuePath": ".\\Private$\\clientB-to-clientA"
}
```

## Run

```
cd src/HandleRdp.Worker
dotnet run
```
