using System.Reflection;
using Elsa.Authorization;
using Elsa.Permissions;
using Elsa.UserTasks.Permissions;
using Elsa.UserTasks.Services;
using FastEndpoints;
using NSubstitute;
using System.Threading.Tasks;

namespace Elsa.UserTasks.UnitTests.Authorization;

/// <summary>
/// Pins what each User Tasks endpoint requires, and checks that what they require is something the catalog
/// advertises.
/// </summary>
/// <remarks>
/// The module reached production declaring access through the legacy string channel, which compared claims for
/// exact equality and appeared in no catalog. Nothing could see the gap: the coverage gate only asks whether an
/// endpoint declares <em>something</em>, and the migration guide's consistency check reads the guide, so a
/// permission absent from both the guide and the catalog was invisible from every direction. Asserting the
/// declarations against the descriptors closes that: an endpoint requiring a verb the module never advertises
/// cannot be granted through the role editor, and fails here rather than in a deployment.
/// </remarks>
public class EndpointPermissionTests
{
    private static readonly Assembly Module = typeof(DefaultUserTaskAccessPolicy).Assembly;

    /// <summary>What each endpoint is expected to require. The rows are the migration guide's table, in code.</summary>
    private static readonly (string Endpoint, string Resource, string Verb)[] Expected =
    [
        ("FeatureCapabilitiesEndpoint", UserTasksResourcePermissions.UserTasks, CoreVerbs.View),
        ("ListEndpoint", UserTasksResourcePermissions.UserTasks, CoreVerbs.View),
        ("GetEndpoint", UserTasksResourcePermissions.UserTasks, CoreVerbs.View),
        ("CapabilitiesEndpoint", UserTasksResourcePermissions.UserTasks, CoreVerbs.View),
        ("ListEventsEndpoint", UserTasksResourcePermissions.UserTasks, CoreVerbs.View),
        ("RevealFieldEndpoint", UserTasksResourcePermissions.UserTasks, CoreVerbs.View),
        ("ClaimEndpoint", UserTasksResourcePermissions.UserTasks, UserTaskVerbs.Claim),
        ("ReleaseEndpoint", UserTasksResourcePermissions.UserTasks, UserTaskVerbs.Claim),
        ("AssignEndpoint", UserTasksResourcePermissions.UserTasks, UserTaskVerbs.Assign),
        ("ScheduleEndpoint", UserTasksResourcePermissions.UserTasks, CoreVerbs.Update),
        ("CompleteEndpoint", UserTasksResourcePermissions.UserTasks, UserTaskVerbs.Complete),
        ("CancelEndpoint", UserTasksResourcePermissions.UserTasks, UserTaskVerbs.Cancel),
        ("RetryResolutionEndpoint", UserTasksResourcePermissions.UserTasks, UserTaskVerbs.Supervise),
        ("IssueInvitationEndpoint", UserTasksResourcePermissions.UserTasks, UserTaskVerbs.Invite),
        ("ListInvitationsEndpoint", UserTasksResourcePermissions.UserTasks, UserTaskVerbs.Invite),
        ("RevokeInvitationEndpoint", UserTasksResourcePermissions.UserTasks, UserTaskVerbs.Invite),
        ("ListParticipantsEndpoint", UserTasksResourcePermissions.Participants, CoreVerbs.View)
    ];

    public static IEnumerable<(string, string, string)> Declarations => Expected;

    [Test]
    [MethodDataSource(nameof(Declarations))]
    public async Task EndpointDeclaresItsExpectedPermission(string endpointName, string resource, string verb)
    {
        var declared = await Declare(endpointName);

        await Assert.That(declared).IsEqualTo(new Permission(resource, verb));
    }

    [Test]
    [MethodDataSource(nameof(Declarations))]
    public async Task EveryDeclaredPermissionIsAdvertisedByTheCatalog(string endpointName, string resource, string verb)
    {
        var declared = await Declare(endpointName);
        var descriptor = new UserTasksResourcePermissionsDescriptorProvider().GetDescriptors()
            .SingleOrDefault(x => x.Resource == declared.Resource);

        await Assert.That(descriptor is not null).IsTrue().Because($"{endpointName} requires resource '{declared.Resource}', which the module contributes no descriptor for, so it cannot be granted through the role editor.");
        await Assert.That(descriptor!.Supports(declared.Verb)).IsTrue().Because($"{endpointName} requires '{declared}', but '{declared.Resource}' advertises only [{string.Join(", ", descriptor.SupportedVerbs)}].");
        // The theory data is what the migration guide's rows are written against, so it has to agree too.
        await Assert.That(declared).IsEqualTo(new Permission(resource, verb));
    }

    [Test]
    public async Task EveryEndpointInTheModuleIsCovered()
    {
        // Without this, deleting a row would silently stop testing an endpoint rather than fail.
        var declaring = EndpointNames().OrderBy(x => x, StringComparer.Ordinal).ToArray();
        var asserted = Expected.Select(x => x.Endpoint).OrderBy(x => x, StringComparer.Ordinal).ToArray();

        await Assert.That(declaring).IsEquivalentTo(asserted, TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    public async Task EveryAdvertisedVerbGuardsSomething()
    {
        // A verb nobody requires is one an administrator can grant to no effect.
        var required = Expected.Select(x => new Permission(x.Resource, x.Verb)).ToHashSet();
        var unused = new UserTasksResourcePermissionsDescriptorProvider().GetDescriptors()
            .SelectMany(x => x.SupportedVerbs.Select(verb => new Permission(x.Resource, verb)))
            .Where(x => !required.Contains(x))
            .Select(x => x.ToString())
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        await Assert.That(unused.Length == 0).IsTrue().Because($"The catalog advertises {unused.Length} permission(s) no endpoint requires: {string.Join(", ", unused)}.");
    }

    /// <summary>The Elsa endpoints this module declares, by simple name.</summary>
    private static IEnumerable<string> EndpointNames() =>
        Elsa.Testing.Shared.Authorization.EndpointCoverage.FindEndpoints(Module).Select(x => x.Name);

    /// <summary>
    /// Runs one endpoint's <c>Configure()</c> and returns what it recorded. The requirement is attached as an
    /// inline policy, which cannot be read back off the definition, so the registry is the only way to observe
    /// a declaration without booting a host.
    /// </summary>
    private static async Task<Permission> Declare(string endpointName)
    {
        var endpointType = Module.GetTypes().Single(x => x.Name == endpointName);
        var arguments = endpointType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single()
            .GetParameters()
            .Select(x => Substitute.For([x.ParameterType], []))
            .ToArray();
        var endpoint = Activator.CreateInstance(endpointType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, arguments, null)!;
        var (requestType, responseType) = DtoTypes(endpointType);

        endpointType.GetProperty("Definition", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(endpoint, new EndpointDefinition(endpointType, requestType, responseType));
        endpointType.GetMethod("Configure")!.Invoke(endpoint, null);

        var permission = EndpointPermissionRegistry.Find(endpointType);

        await Assert.That(permission.HasValue).IsTrue().Because($"{endpointName} declares no permission.");
        return permission!.Value;
    }

    private static (Type Request, Type Response) DtoTypes(Type endpointType)
    {
        for (var type = endpointType.BaseType; type is not null; type = type.BaseType)
        {
            if (!type.IsGenericType)
                continue;

            var definition = type.GetGenericTypeDefinition();
            var arguments = type.GetGenericArguments();

            if (definition == typeof(Elsa.Abstractions.ElsaEndpoint<,>))
                return (arguments[0], arguments[1]);
            if (definition == typeof(Elsa.Abstractions.ElsaEndpointWithoutRequest<>))
                return (typeof(EmptyRequest), arguments[0]);
            if (definition == typeof(Elsa.Abstractions.ElsaEndpoint<>))
                return (arguments[0], typeof(object));
        }

        throw new InvalidOperationException($"Unsupported endpoint type '{endpointType.FullName}'.");
    }
}
