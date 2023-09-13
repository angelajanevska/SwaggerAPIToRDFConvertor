using System;
using System.IO;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using VDS.RDF;
using VDS.RDF.Writing;
using StringWriter = System.IO.StringWriter;
using System.Reflection;

class Program
{
    static void Main(string[] args)
    {
        Console.Write("Enter the path to the Swagger JSON file: ");
        string filePath = Console.ReadLine();

        //string swaggerJson = File.ReadAllText("swagger.json");

        try
        {
            if (File.Exists(filePath))
            {
                string swaggerJson = File.ReadAllText(filePath);
                JObject swaggerObject = JObject.Parse(swaggerJson);

                IGraph graph = ConvertSwaggerApiToRdf(swaggerObject);

                // Serialize and output the RDF graph
                StringWriter sw = new StringWriter();
                CompressingTurtleWriter turtleWriter = new CompressingTurtleWriter();
                turtleWriter.Save(graph, sw);

                Console.WriteLine(sw.ToString());
                Console.WriteLine($"Conversion done.");
            }
            else
            {
                Console.WriteLine("The specified file does not exist.");
            }
        }
        catch (Exception ex)
        {
            // Handle exceptions here, you can log the error or take appropriate actions
            Console.WriteLine("Error occurred: " + ex.Message);
        }
    }

    static IGraph ConvertSwaggerApiToRdf(JObject swaggerObject)
    {

        IGraph graph = new Graph();
        graph.NamespaceMap.AddNamespace("rdf", new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#"));
        graph.NamespaceMap.AddNamespace("ex", new Uri("http://example.org/"));

        INode subject;
        INode obj;

        // Define the JSON-LD context for mapping JSON keys to RDF predicates
        JObject context = new JObject
        {
            ["@vocab"] = "http://example.org/",
            ["tags"] = "ex:IsTag",
            ["schemes"] = "ex:IsScheme",
            ["paths"] = "ex:HasPath",
            ["summary"] = "ex:HasSummary",
            ["description"] = "ex:HasDescription",
            ["name"] = "ex:HasName",
            ["get"] = "ex:HasGetMethod",
            ["post"] = "ex:HasPostMethod",
            ["delete"] = "ex:HasDeleteMethod",
            ["consumes"] = "ex:Consumes",
            ["produces"] = "ex:Produces",
            ["operationId"] = "ex:HasOperationId",
            ["security"] = "ex:HasSecurity",
            ["scopes"] = "ex:HasSecurityScope",
            ["definitions"] = "ex:IsDefinition",
            ["securityDefinitions"] = "ex:IsSecurityDefinition",
            ["type"] = "ex:HasType",
            ["properties"] = "ex:HasProperty",
        };

        // Create a JSON-LD context node and set it as the default context
        INode contextNode = graph.CreateUriNode(new Uri("http://example.org/context"));
        INode contextJsonLdNode = graph.CreateLiteralNode(context.ToString(Formatting.None));
        graph.Assert(new Triple(contextNode, graph.CreateUriNode(new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#value")), contextJsonLdNode));
        graph.Assert(new Triple(contextNode, graph.CreateUriNode(new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#type")), graph.CreateUriNode(new Uri("http://www.w3.org/ns/json-ld#Context"))));

        INode methodProperty = graph.CreateUriNode("ex:isMethodType");
        INode tagsProperty = graph.CreateUriNode("ex:IsTag");
        INode schemeProperty = graph.CreateUriNode("ex:IsScheme");
        INode pathTagsProperty = graph.CreateUriNode("ex:IsPathTag");
        INode definitionProperty = graph.CreateUriNode("ex:IsDefinition");
        INode securityDefinitionsProperty = graph.CreateUriNode("ex:IsSecurityDefinition");
        INode summaryProperty = graph.CreateUriNode("ex:HasSummary");
        INode descriptionProperty = graph.CreateUriNode("ex:HasDescription");
        INode consumesProperty = graph.CreateUriNode("ex:Consumes");
        INode producesProperty = graph.CreateUriNode("ex:Produces");
        INode operationIdProperty = graph.CreateUriNode("ex:HasOperationId");
        INode securityProperty = graph.CreateUriNode("ex:HasSecurity");
        INode scopeProperty = graph.CreateUriNode("ex:SecurityScope");
        INode nameProperty = graph.CreateUriNode("ex:HasName");
        INode typeProperty = graph.CreateUriNode("ex:HasType");
        INode securityDefinitionsNameProperty = graph.CreateUriNode("ex:SecurityDefinitionsName");
        INode valueProperty = graph.CreateUriNode("ex:HasValue");
        INode propertyProperty = graph.CreateUriNode("ex:HasProperty");

        try
        {

            //Tags
            if (swaggerObject.ContainsKey("tags"))
            {
                foreach (var tag in swaggerObject["tags"])
                {
                    string tagName = tag.Value<string>("name");
                    string tagDescription = tag.Value<string>("description");
                    Uri tagUri = new Uri("http://example.org/tag/" + tagName);
                    INode tagSubject = graph.CreateUriNode(tagUri);

                    graph.Assert(new Triple(tagSubject, tagsProperty, graph.CreateUriNode("ex:Tag")));
                    graph.Assert(new Triple(tagSubject, nameProperty, graph.CreateLiteralNode(tagName)));
                    graph.Assert(new Triple(tagSubject, descriptionProperty, graph.CreateLiteralNode(tagDescription)));
                }
            }

            //Schemes
            if (swaggerObject.ContainsKey("schemes"))
            {
                foreach (var scheme in swaggerObject["schemes"])
                {
                    string schemeName = scheme.Value<string>();
                    Uri schemeUri = new Uri("http://example.org/scheme/" + schemeName.ToLower());
                    INode schemeSubject = graph.CreateUriNode(schemeUri);

                    graph.Assert(new Triple(schemeSubject, schemeProperty, graph.CreateUriNode("ex:Scheme")));
                    graph.Assert(new Triple(schemeSubject, nameProperty, graph.CreateLiteralNode(schemeName)));
                }
            }

            //Paths
            foreach (var pathProperty in swaggerObject["paths"].Children<JProperty>())
            {
                string path = pathProperty.Name.Replace("{", "_").Replace("}", "_");
                string pathUri = "http://example.org" + path;

                foreach (var method in pathProperty.Value.Children<JProperty>())
                {
                    string methodName = System.Text.RegularExpressions.Regex.Unescape(method.Name);
                    string methodType = method.Name.ToLower();

                    // Tags
                    var tagsArray = method.Value["tags"]?.ToObject<string[]>();
                    if (tagsArray != null)
                    {
                        foreach (var tag in tagsArray)
                        {
                            subject = graph.CreateUriNode(new Uri(pathUri));
                            obj = graph.CreateUriNode(new Uri("http://example.org/" + tag));

                            graph.Assert(new Triple(subject, pathTagsProperty, obj));
                        }
                    }

                    // Summary
                    string summary = method.Value["summary"]?.ToObject<string>();
                    if (!string.IsNullOrEmpty(summary))
                    {
                        INode summarySubject = graph.CreateUriNode(new Uri(pathUri + "#" + methodName));
                        INode summaryObj = graph.CreateLiteralNode(summary);
                        graph.Assert(new Triple(summarySubject, summaryProperty, summaryObj));
                    }

                    // Description
                    string description = method.Value["description"]?.ToObject<string>();
                    if (!string.IsNullOrEmpty(description))
                    {
                        INode descriptionSubject = graph.CreateUriNode(new Uri(pathUri + "#" + methodName));
                        INode descriptionObj = graph.CreateLiteralNode(description);
                        graph.Assert(new Triple(descriptionSubject, descriptionProperty, descriptionObj));
                    }

                    // Consumes
                    var consumesArray = method.Value["consumes"]?.ToObject<string[]>();
                   if (consumesArray != null)
                    {
                        foreach (var consumeType in consumesArray)
                        {
                            INode consumesSubject = graph.CreateUriNode(new Uri(pathUri + "#" + methodName));
                            INode consumesObj = graph.CreateLiteralNode(consumeType);
                            graph.Assert(new Triple(consumesSubject, consumesProperty, consumesObj));
                        }
                    }

                    // Produces
                    var producesArray = method.Value["produces"]?.ToObject<string[]>();
                    if (producesArray != null)
                    {
                        foreach (var produceType in producesArray)
                        {
                            INode producesSubject = graph.CreateUriNode(new Uri(pathUri + "#" + methodName));
                            INode producesObj = graph.CreateLiteralNode(produceType);
                            graph.Assert(new Triple(producesSubject, producesProperty, producesObj));
                        }
                    }

                    // OperationId
                    string operationId = method.Value["operationId"]?.ToObject<string>();
                    if (!string.IsNullOrEmpty(operationId))
                    {
                        INode operationIdSubject = graph.CreateUriNode(new Uri(pathUri + "#" + methodName));
                        INode operationIdObj = graph.CreateLiteralNode(operationId);
                        graph.Assert(new Triple(operationIdSubject, operationIdProperty, operationIdObj));
                    }

                    // Security
                    if (method.Value["security"]?.ToObject<JArray>() != null)
                    {
                        foreach (var securityItem in method.Value["security"])
                        {
                            foreach (var securityKey in securityItem.Children<JProperty>())
                            {
                                string securityScheme = securityKey.Name;

                                // Determine the structure of securityKey.Value and access its properties accordingly.
                                // You might need to inspect securityKey.Value and adapt the code accordingly.
                                // For example, if securityKey.Value is an array of strings, you can access them like this:
                                var securityScopes = securityKey.Value.Select(token => token.Value<string>()).ToArray();

                                INode securitySubject = graph.CreateUriNode(new Uri(pathUri + "#" + methodName));

                                // Create RDF triples for security schemes and their scopes
                                graph.Assert(new Triple(securitySubject, securityProperty, graph.CreateLiteralNode(securityScheme)));

                                foreach (var scope in securityScopes)
                                {
                                    INode scopeObj = graph.CreateLiteralNode(scope);
                                    graph.Assert(new Triple(securitySubject, scopeProperty, scopeObj));
                                }
                            }
                        }
                    }


                    subject = graph.CreateUriNode(new Uri(pathUri + "#" + methodName));
                    obj = graph.CreateUriNode(new Uri("http://example.org/" + methodType));

                    graph.Assert(new Triple(subject, methodProperty, obj));
                }
            }


            // SecurityDefinitions
            if (swaggerObject.ContainsKey("securityDefinitions"))
            {
                foreach (var securityDefinition in swaggerObject["securityDefinitions"].Children<JProperty>())
                {
                    string securityDefinitionName = securityDefinition.Name;
                    JObject securityDefinitionObject = (JObject)securityDefinition.Value;

                    string securityDefinitionUriString = "http://example.org/securityDefinition/" + securityDefinitionName;
                    INode securityDefinitionSubject = graph.CreateUriNode(new Uri(securityDefinitionUriString));

                    graph.Assert(new Triple(securityDefinitionSubject, securityDefinitionsProperty, graph.CreateUriNode("ex:SecurityDefinition")));
                    graph.Assert(new Triple(securityDefinitionSubject, nameProperty, graph.CreateLiteralNode(securityDefinitionName)));

                    foreach (var property in securityDefinitionObject.Properties())
                    {
                        string propertyName = property.Name;
                        JToken propertyValue = property.Value;

                        string propertyUriString = securityDefinitionUriString + "#" + propertyName;
                        INode propertySubject = graph.CreateUriNode(new Uri(propertyUriString));

                        graph.Assert(new Triple(propertySubject, typeProperty, graph.CreateUriNode("ex:SecurityDefinitionProperty")));
                        graph.Assert(new Triple(propertySubject, nameProperty, graph.CreateLiteralNode(propertyName)));

                        if (propertyValue.Type == JTokenType.String)
                        {
                            graph.Assert(new Triple(securityDefinitionSubject, propertySubject, graph.CreateLiteralNode(propertyValue.ToString())));
                        }
                        else if (propertyValue.Type == JTokenType.Object)
                        {
                            foreach (var nestedProperty in propertyValue.Children<JProperty>())
                            {
                                string nestedPropertyName = nestedProperty.Name;
                                string nestedPropertyValue = nestedProperty.Value.ToString();

                                INode nestedPropertySubject = graph.CreateUriNode(new Uri(propertyUriString + "#" + nestedPropertyName));

                                graph.Assert(new Triple(securityDefinitionSubject, propertySubject, nestedPropertySubject));
                                graph.Assert(new Triple(nestedPropertySubject, nameProperty, graph.CreateLiteralNode(nestedPropertyName)));
                                graph.Assert(new Triple(nestedPropertySubject, valueProperty, graph.CreateLiteralNode(nestedPropertyValue)));

                            }
                        }
                    }
                }
            }


            // Definitions
            if (swaggerObject.ContainsKey("definitions"))
            {
                foreach (var definition in swaggerObject["definitions"].Children<JProperty>())
                {
                    string definitionName = definition.Name;
                    JObject definitionObject = (JObject)definition.Value;

                    string definitionUriString = "http://example.org/definition/" + definitionName;
                    INode definitionSubject = graph.CreateUriNode(new Uri(definitionUriString));

                    if (definitionObject.ContainsKey("properties"))
                    {
                        foreach (var property in definitionObject["properties"].Children<JProperty>())
                        {
                            string propertyName = property.Name;

                            string propertyUriString = definitionUriString + "#" + propertyName;
                            INode propertySubject = graph.CreateUriNode(new Uri(propertyUriString));

                            graph.Assert(new Triple(definitionSubject, definitionProperty, propertySubject));
                            graph.Assert(new Triple(propertySubject, propertyProperty, graph.CreateUriNode("ex:Property")));
                            graph.Assert(new Triple(propertySubject, nameProperty, graph.CreateLiteralNode(propertyName)));
                        }
                    }
                }
            }


            return graph;
        }
        catch (Exception ex)
        {
            
            Console.WriteLine("Error occurred in converting the swagger api to rdf: " + ex.Message);
            return null;
        }
    }

}
