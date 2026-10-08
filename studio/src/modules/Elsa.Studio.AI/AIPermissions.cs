namespace Elsa.Studio.AI;

/// <summary>Backend permission resources guarding the AI APIs.</summary>
public static class AIPermissions
{
    /// <summary>Weaver capability discovery.</summary>
    public const string Capabilities = "ai/capabilities";

    /// <summary>Weaver tool discovery.</summary>
    public const string Tools = "ai/tools";

    /// <summary>Weaver chat.</summary>
    public const string Chat = "ai/chat";
}
