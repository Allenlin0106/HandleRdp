using HandleRdp.Worker;
using HandleRdp.Worker.Messaging;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<MsmqOptions>(builder.Configuration.GetSection(MsmqOptions.SectionName));
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
