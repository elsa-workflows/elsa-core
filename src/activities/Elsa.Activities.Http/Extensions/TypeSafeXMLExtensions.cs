using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using System.Text.RegularExpressions;
using System.Xml;

namespace Elsa.Activities.Http.Extensions
{
    public static class TypeSafeXMLExtensions
    {
        public static string GetRootTypeName(ref byte[] content)
        {
            string rootNodeName = "";

            // we need to strip off the doctype since 
            // we dont want to load dtd (security) and 
            // with doctype xmlreader wont read the content

            string contentString = System.Text.Encoding.Default.GetString(content);
            contentString = Regex.Replace(contentString, "<!doctype.*?>", "", RegexOptions.IgnoreCase);
            content = System.Text.Encoding.Default.GetBytes(contentString);

            using (XmlReader xmlReader = XmlReader.Create(new MemoryStream(content)))
            {
                if (xmlReader.MoveToContent() == XmlNodeType.Element)
                    rootNodeName = xmlReader.Name;
            }

            return rootNodeName;
        }

        public static void LoadAssembly(string typeName, string assemblyName)
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

            var assembly =
                AssemblyLoadContext.Default.LoadFromAssemblyPath(
                    Directory.EnumerateFiles(
                        baseDirectory, assemblyName, SearchOption.AllDirectories
                        ).FirstOrDefault()
                    );

            typeCache.TryAdd(typeName, assembly.GetType(typeName));

        }

        public static ConcurrentDictionary<string, Type> typeCache = new ConcurrentDictionary<string, Type>();

        public static Type GetType(string typeName, string assemblyName = "")
        {
            Type returnType = null;

            typeCache.TryGetValue(typeName, out returnType);

            if (returnType == null && !string.IsNullOrEmpty(assemblyName))
            {
                LoadAssembly(typeName, assemblyName);
                typeCache.TryGetValue(typeName, out returnType);
            }

            return returnType;
        }
    }
}
