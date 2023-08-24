using System;
using System.IO;
using System.Web;
using Newtonsoft.Json.Linq;
using VDS.RDF;
using VDS.RDF.Writing;

class Program
{
    static void Main(string[] args)
    {
        string swaggerJson = File.ReadAllText("swagger.json");
        JObject swaggerObject = JObject.Parse(swaggerJson);

        // Create an RDF graph
        IGraph graph = new Graph(new UriNode(UriFactory.Create("http://example.org/")));

        // Define namespaces
        //TODO: change to read from json
        graph.NamespaceMap.AddNamespace("rdf", new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#"));
        graph.NamespaceMap.AddNamespace("ex", new Uri("http://example.org/"));

        INode subject;
        INode obj;

        // Extract properties 
        INode methodProperty = graph.CreateUriNode("ex:isMethodType");

        foreach (var pathProperty in swaggerObject["paths"].Children<JProperty>())
        {
            string pathUri = "ex:" + pathProperty.Name;

            foreach (var method in pathProperty.Value.Children<JProperty>())
            {
                
                subject =  graph.CreateUriNode(pathUri);
                obj = graph.CreateUriNode("ex:"+method.Name);
                graph.Assert(new Triple(subject, methodProperty, obj));
            }
        }

        foreach (Triple t in graph.Triples)
        {
            Console.WriteLine(t.ToString());
        }


        // Save 
        //string outputPath = "output.ttl";
        //File.WriteAllText(outputPath, content.ToString());

        //Console.WriteLine($"Convertion done.");
    }
}
