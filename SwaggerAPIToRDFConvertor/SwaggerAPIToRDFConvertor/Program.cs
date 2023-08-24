using System;
using System.IO;
using System.Xml.Linq;
using Newtonsoft.Json;
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
        IGraph rdfGraph = new Graph();

        // Extract properties from Swagger JSON and create RDF nodes
        foreach (var path in swaggerObject["paths"])
        {
            foreach (var method in path.Children<JProperty>())
            {
                // Extract properties for RDF nodes
                string pathUri = "http://example.org" + path.Path;
                string methodUri = pathUri + "#" + method.Name;

                // Create RDF nodes
                IUriNode subjectNode = rdfGraph.CreateUriNode(UriFactory.Create(pathUri));
                IUriNode predicateNode = rdfGraph.CreateUriNode("ex:hasMethod");
                IUriNode objectNode = rdfGraph.CreateUriNode(UriFactory.Create(methodUri));

                // Assert RDF triples
                rdfGraph.Assert(new Triple(subjectNode, predicateNode, objectNode));

                // Add other properties as needed
            }
        }

        // Serialize RDF graph to Turtle format
        CompressingTurtleWriter turtleWriter = new(); 
        turtleWriter.Save(rdfGraph, "output.ttl");

        // Save Turtle content to a file
        Console.WriteLine("Conversion done.");
    }
}
