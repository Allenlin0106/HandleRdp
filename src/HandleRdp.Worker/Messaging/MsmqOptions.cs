namespace HandleRdp.Worker.Messaging;

public class MsmqOptions
{
    public const string SectionName = "Msmq";

    // Queue this worker (Client B) reads Client A's requests from.
    public string RequestQueuePath { get; set; } = @".\Private$\clientA-to-clientB";

    // Queue this worker replies on.
    public string ResponseQueuePath { get; set; } = @".\Private$\clientB-to-clientA";
}
