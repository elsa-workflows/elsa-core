using System.Net;
using Elsa.Studio.Workflows.Domain.Extensions;
using Refit;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

public class ValidationApiExceptionExtensionsTests
{
    [Fact]
    public async Task GetValidationErrors_ReadsGeneralErrorsFromApiExceptionContent()
    {
        const string content = """
        {
          "statusCode": 400,
          "message": "One or more errors occurred!",
          "errors": {
            "generalErrors": [
              "The /absence/created path and post method are already in use by another workflow!"
            ]
          }
        }
        """;
        var exception = await CreateApiExceptionAsync(content);

        var errors = exception.GetValidationErrors();

        var error = Assert.Single(errors.Errors);
        Assert.Equal("The /absence/created path and post method are already in use by another workflow!", error.ErrorMessage);
    }

    [Fact]
    public async Task GetValidationErrors_UsesMessageWhenErrorsAreMissing()
    {
        const string content = """
        {
          "statusCode": 400,
          "message": "The workflow definition is invalid."
        }
        """;
        var exception = await CreateApiExceptionAsync(content);

        var errors = exception.GetValidationErrors();

        var error = Assert.Single(errors.Errors);
        Assert.Equal("The workflow definition is invalid.", error.ErrorMessage);
    }

    [Fact]
    public async Task GetValidationErrors_PreservesPlainTextContent()
    {
        const string content = "The /absence/created path and post method are already in use by another workflow!";
        var exception = await CreateApiExceptionAsync(content);

        var errors = exception.GetValidationErrors();

        var error = Assert.Single(errors.Errors);
        Assert.Equal(content, error.ErrorMessage);
    }

    private static async Task<ApiException> CreateApiExceptionAsync(string content)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/workflow-definitions");
        using var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            RequestMessage = request,
            ReasonPhrase = "Bad Request",
            Content = new StringContent(content)
        };

        return await ApiException.Create(request, HttpMethod.Post, response, new RefitSettings());
    }
}
