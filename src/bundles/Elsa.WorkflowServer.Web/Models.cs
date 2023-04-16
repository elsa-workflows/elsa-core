namespace Elsa.WorkflowServer.Web;

public record ApiResponse<T>(T Data, Support Support);
public record Support(string Url, string Text);
public record User(int Id, string FirstName, string LastName, string Email, string Avatar);