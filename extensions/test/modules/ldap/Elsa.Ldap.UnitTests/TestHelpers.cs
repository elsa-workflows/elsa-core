using System.DirectoryServices.Protocols;
using System.Reflection;

namespace Elsa.Ldap.UnitTests;

internal static class TestHelpers
{
    /// <summary>
    /// Dirty workaround to create a <see cref="SearchResultEntry"/> instance with the specified attributes,
    /// since the class does not have a public constructor and its properties are read-only."/>
    /// </summary>
    public static SearchResultEntry CreateSearchResultEntry(string distinguishedName, params DirectoryAttribute[] attributes)
    {
        var attributeCollectionType = typeof(SearchResultAttributeCollection);
        var attributeCollectionCtor = attributeCollectionType!.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).First();
        var attributeCollection = (SearchResultAttributeCollection)attributeCollectionCtor.Invoke(null);

        var addMethod = attributeCollectionType.GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var attribute in attributes)
        {
            addMethod!.Invoke(attributeCollection, [attribute.Name, attribute]);
        }

        var entryType = typeof(SearchResultEntry);
        var entryCtor = entryType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).First(x => x.GetParameters().Count() == 2);
        var entry = (SearchResultEntry)entryCtor.Invoke([distinguishedName, attributeCollection]);

        return entry;
    }

    /// <summary>
    /// Dirty workaround to create a <see cref="SearchResponse"/> instance with the specified entries,
    /// since the class does not have a public constructor and its properties are read-only."/>
    /// </summary>
    public static SearchResponse CreateSearchResponse(params SearchResultEntry[] entries)
    {
        var responseType = typeof(SearchResponse);
        var responseCtor = responseType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).First();
        var response = (SearchResponse)responseCtor.Invoke(["", new DirectoryControl[0], ResultCode.Success, "", new Uri[0]]);

        var entriesCollectionType = typeof(SearchResultEntryCollection);
        var entriesCollectionCtor = entriesCollectionType!.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).First();
        var entriesCollection = (SearchResultEntryCollection)entriesCollectionCtor.Invoke(null);
        var addMethod = entriesCollectionType.GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var entry in entries)
        {
            addMethod!.Invoke(entriesCollection, [entry]);
        }

        var entriesField = responseType.GetField("_entryCollection", BindingFlags.NonPublic | BindingFlags.Instance);
        entriesField!.SetValue(response, entriesCollection);

        return response;
    }

    /// <summary>
    /// Dirty workaround to create a <see cref="AddResponse"/> instance,
    /// since the class does not have a public constructor and its properties are read-only."/>
    /// </summary>
    public static AddResponse CreateAddResponse(ResultCode resultCode)
    {
        var responseType = typeof(AddResponse);
        var responseCtor = responseType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).First();
        return (AddResponse)responseCtor.Invoke(["", new DirectoryControl[0], resultCode, "", new Uri[0]]);
    }

    /// <summary>
    /// Dirty workaround to create a <see cref="CompareResponse"/> instance,
    /// since the class does not have a public constructor and its properties are read-only."/>
    /// </summary>
    public static CompareResponse CreateCompareResponse(ResultCode resultCode)
    {
        var responseType = typeof(CompareResponse);
        var responseCtor = responseType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).First();
        return (CompareResponse)responseCtor.Invoke(["", new DirectoryControl[0], resultCode, "", new Uri[0]]);
    }

    /// <summary>
    /// Dirty workaround to create a <see cref="DeleteResponse"/> instance,
    /// since the class does not have a public constructor and its properties are read-only."/>
    /// </summary>
    public static DeleteResponse CreateDeleteResponse(ResultCode resultCode)
    {
        var responseType = typeof(DeleteResponse);
        var responseCtor = responseType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).First();
        return (DeleteResponse)responseCtor.Invoke(["", new DirectoryControl[0], resultCode, "", new Uri[0]]);
    }

    /// <summary>
    /// Dirty workaround to create a <see cref="ModifyResponse"/> instance,
    /// since the class does not have a public constructor and its properties are read-only."/>
    /// </summary>
    public static ModifyResponse CreateModifyResponse(ResultCode resultCode)
    {
        var responseType = typeof(ModifyResponse);
        var responseCtor = responseType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).First();
        return (ModifyResponse)responseCtor.Invoke(["", new DirectoryControl[0], resultCode, "", new Uri[0]]);
    }

    /// <summary>
    /// Dirty workaround to create a <see cref="ModifyDNResponse"/> instance,
    /// since the class does not have a public constructor and its properties are read-only."/>
    /// </summary>
    public static ModifyDNResponse CreateModifyDNResponse(ResultCode resultCode)
    {
        var responseType = typeof(ModifyDNResponse);
        var responseCtor = responseType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).First();
        return (ModifyDNResponse)responseCtor.Invoke(["", new DirectoryControl[0], resultCode, "", new Uri[0]]);
    }
}