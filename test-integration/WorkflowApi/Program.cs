// dapr run --app-id workflow-test --app-port 5177 --dapr-http-port 3500 --dapr-grpc-port 50001 --log-level debug
//
//
using System.Diagnostics;
using System.Text.Json.Serialization;
using Dapr.Workflow;

// Debugger.Launch();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDaprWorkflow(o =>
{
    o.RegisterWorkflow<TestWorkflow>();
    o.RegisterActivity<TextActivity1>();
    o.RegisterActivity<TextActivity2>();
});

var app = builder.Build();

app.MapPost("/workflow", async (StartWorkflowRequest req, DaprWorkflowClient client) =>
{
    var id = await client.ScheduleNewWorkflowAsync(nameof(TestWorkflow), input: req.Input , startTime: req.StartTime?.UtcDateTime);
    return Results.Ok(new { instance_id = id });
});

app.Run();

public record StartWorkflowRequest(
    [property: JsonPropertyName("input")] string Input,
    [property: JsonPropertyName("start_time")] DateTimeOffset? StartTime);

public class TestWorkflow : Workflow<string, string>
{
    public override async Task<string> RunAsync(WorkflowContext context, string input)
    {
        var logger = context.CreateReplaySafeLogger<TestWorkflow>();

        var response1 = await context.CallActivityAsync<string>(nameof(TextActivity1), input);

        logger.LogInformation("Response from TextActivity1: {Response}", response1);

        var response2 = await context.CallActivityAsync<string>(nameof(TextActivity2), input);

        logger.LogInformation("Response from TextActivity2: {Response}", response2);

        return $"Results were: {response1} | {response2}";
    }
}

public class TextActivity1 : WorkflowActivity<string, string>
{
    public override Task<string> RunAsync(WorkflowActivityContext context, string input)
    {
        return Task.FromResult($"TextActivity1 processed: {input}");
    }
}

public class TextActivity2 : WorkflowActivity<string, string>
{
    public override Task<string> RunAsync(WorkflowActivityContext context, string input)
    {
        return Task.FromResult($"TextActivity2 processed: {input}");
    }
}
