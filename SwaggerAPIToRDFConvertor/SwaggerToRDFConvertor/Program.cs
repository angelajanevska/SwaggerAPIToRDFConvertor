using System;
using System.IO;
using Newtonsoft.Json.Linq;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Writing;

class Program
{
    static void Main(string[] args)
    {
        string swaggerJson = File.ReadAllText("swagger.json");
        JObject swaggerObject = JObject.Parse(swaggerJson);

        IGraph graph = new Graph();
        graph.NamespaceMap.AddNamespace("rdf", new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#"));
        graph.NamespaceMap.AddNamespace("ex", new Uri("http://example.org/"));

        INode subject;
        INode obj;

        INode methodProperty = graph.CreateUriNode("ex:isMethodType");

        foreach (var pathProperty in swaggerObject["paths"].Children<JProperty>())
        {
            string path = pathProperty.Name.Replace("{", "_").Replace("}", "_");
            string pathUri = "http://example.org/" + path;

            foreach (var method in pathProperty.Value.Children<JProperty>())
            {
                string methodName = System.Text.RegularExpressions.Regex.Unescape(method.Name);

                subject = graph.CreateUriNode(new Uri(pathUri));
                obj = graph.CreateUriNode(new Uri("http://example.org/" + methodName));

                // Create the RDF triple
                Triple triple = new Triple(subject, methodProperty, obj);
                graph.Assert(triple);
            }
        }

        // Serialize the RDF graph to the desired format
        CompressingTurtleWriter writer = new CompressingTurtleWriter();
        System.IO.StringWriter sw = new System.IO.StringWriter();
        writer.Save(graph, sw);

        // Output the RDF triples in the desired format
        Console.WriteLine(sw.ToString());

        Console.WriteLine($"Conversion done.");
    }
}
