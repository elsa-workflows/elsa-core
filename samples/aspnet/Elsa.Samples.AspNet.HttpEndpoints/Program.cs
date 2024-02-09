using Elsa.Extensions;
using Elsa.Samples.AspNet.HttpEndpoints.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddElsa(elsa =>
{
    // Add activities.
    elsa.AddActivitiesFrom<Program>();
    
    // Add workflows.
    elsa.AddWorkflowsFrom<Program>();
    
    // Enable the HTTP feature for HTTP activities.
    elsa.UseHttp();
});

// Add AutoMapper for demo purposes.
builder.Services.AddAutoMapper(autoMapper => autoMapper.CreateMap<CustomerDto, Customer>());

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseWorkflows();
app.Run();