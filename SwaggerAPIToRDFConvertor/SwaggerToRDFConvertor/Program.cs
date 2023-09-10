using Newtonsoft.Json.Linq;
using VDS.RDF;
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
        INode pathTagsProperty = graph.CreateUriNode("ex:IsPathTag");
        INode summaryProperty = graph.CreateUriNode("ex:HasSummary");

        if (swaggerObject.ContainsKey("tags"))
        {
            foreach (var tag in swaggerObject["tags"])
            {
                string tagName = tag.Value<string>("name");
                string tagDescription = tag.Value<string>("description");
                Uri tagUri = new Uri("http://example.org/tag/" + tagName);
                INode tagSubject = graph.CreateUriNode(tagUri);

                // Create RDF triples for tags
                graph.Assert(new Triple(tagSubject, graph.CreateUriNode("rdf:type"), graph.CreateUriNode("ex:Tag")));
                graph.Assert(new Triple(tagSubject, graph.CreateUriNode("rdfs:label"), graph.CreateLiteralNode(tagName)));
                graph.Assert(new Triple(tagSubject, graph.CreateUriNode("rdfs:comment"), graph.CreateLiteralNode(tagDescription)));
            }
        }
       
        if (swaggerObject.ContainsKey("schemes"))
        {
            foreach (var scheme in swaggerObject["schemes"])
            {
                string schemeName = scheme.Value<string>();
                Uri schemeUri = new Uri("http://example.org/scheme/" + schemeName.ToLower());
                INode schemeSubject = graph.CreateUriNode(schemeUri);

                // Create RDF triples for schemes
                graph.Assert(new Triple(schemeSubject, graph.CreateUriNode("rdf:type"), graph.CreateUriNode("ex:Scheme")));
                graph.Assert(new Triple(schemeSubject, graph.CreateUriNode("rdfs:label"), graph.CreateLiteralNode(schemeName)));
            }
        }

        foreach (var pathProperty in swaggerObject["paths"].Children<JProperty>())
        {
            string path = pathProperty.Name.Replace("{", "_").Replace("}", "_");
            string pathUri = "http://example.org/" + path;

            foreach (var method in pathProperty.Value.Children<JProperty>())
            {
                string methodName = System.Text.RegularExpressions.Regex.Unescape(method.Name);

                var tags = method.Value["tags"].ToObject<string[]>();

                foreach (var tag in tags)
                {
                    subject = graph.CreateUriNode(new Uri(pathUri));
                    obj = graph.CreateUriNode(new Uri("http://example.org/" + tag));

                    // Create the RDF triple for each tag
                    Triple tagsTriple = new Triple(subject, pathTagsProperty, obj);
                    graph.Assert(tagsTriple);
                }

                var summary = method.Value["summary"].ToObject<string>();
                Triple summaryTriple = new (graph.CreateUriNode(new Uri(pathUri)), summaryProperty, graph.CreateUriNode(new Uri("http://example.org/" + summary.Replace(" ", "_").Replace(" ", "_"))));
                graph.Assert(summaryTriple);

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
