using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Elsa.Activities.Http.Services;


namespace Elsa.Activities.Http.Formatters
{
    public static class XMLContentFormatterCache
    {
        public static List<Type> TypeCache { get; set; }
    }


    public class XMLContentFormatter : IContentFormatter
    {
        public int Priority => 1;
        public IEnumerable<string> SupportedContentTypes => new[] { "application/xml", "text/xml" };

        public Task<object> ParseAsync(byte[] content, string contentType)
        {

            GetTypeCache();

            string rootNodeTypeName = GetRootTypeName(ref content);

            if (rootNodeTypeName != "")
            {
                Type rootType = GetRootType(rootNodeTypeName);
                XmlSerializer xmlSerializer = new XmlSerializer(rootType);
                var model = xmlSerializer.Deserialize(new MemoryStream(content));
                return Task.FromResult<object>(model);
            }

            // this is not clear - what will be returned if theres no deserialization?
            return null;
        }

        private static Type GetRootType(string rootNodeName)
        {
            return XMLContentFormatterCache.TypeCache.Where(t => t.Name == rootNodeName).FirstOrDefault();
        }

        private static void GetTypeCache()
        {
            if (XMLContentFormatterCache.TypeCache == null)
            {
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                foreach (string fileName in Directory.EnumerateFiles(baseDirectory, "*.dll", SearchOption.AllDirectories))
                {
                    AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(baseDirectory, fileName));
                }

                List<string> elsaLibs =
                    AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => a.GetName().Name.StartsWith("Elsa"))
                    .Select(a => a.FullName).ToList();

                elsaLibs.AddRange(
                    AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => a.GetName().Name.StartsWith("Elsa"))
                    .SelectMany(a => a.GetReferencedAssemblies())
                    .Select(a => a.FullName).ToList());



                XMLContentFormatterCache.TypeCache = 
                    AppDomain.CurrentDomain.GetAssemblies().
                    Where(a =>
                        {
                            string name = a.GetName().Name;
                            return !name.StartsWith("System") &&
                                    !name.StartsWith("Microsoft") &&
                                    !elsaLibs.Contains(a.FullName);
                        }
                        ).SelectMany(a => a.GetTypes()).ToList();

            }
        }

        private static string GetRootTypeName(ref byte[] content)
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
    }
}