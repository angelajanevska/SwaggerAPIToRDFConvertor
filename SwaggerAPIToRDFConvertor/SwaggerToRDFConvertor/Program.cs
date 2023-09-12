﻿using System;
using System.IO;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using VDS.RDF;
using VDS.RDF.Writing;
using StringWriter = System.IO.StringWriter;

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

        // Define the JSON-LD context for mapping JSON keys to RDF predicates
        JObject context = new JObject
        {
            ["@vocab"] = "http://example.org/",
            ["tags"] = "ex:IsTag",
            ["name"] = "ex:tagName",
            ["description"] = "ex:tagDescription",
            ["schemes"] = "ex:IsScheme",
            ["paths"] = "ex:HasPath",
            ["summary"] = "ex:HasSummary",
            ["description"] = "ex:HasDescription",
            ["get"] = "ex:HasGetMethod",
            ["post"] = "ex:HasPostMethod",
            ["delete"] = "ex:HasDeleteMethod",
            ["consumes"] = "ex:Consumes",
            ["produces"] = "ex:Produces",
            ["operationId"] = "ex:HasOperationId" 
        };

        // Create a JSON-LD context node and set it as the default context
        INode contextNode = graph.CreateUriNode(new Uri("http://example.org/context"));
        INode contextJsonLdNode = graph.CreateLiteralNode(context.ToString(Formatting.None));
        graph.Assert(new Triple(contextNode, graph.CreateUriNode(new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#value")), contextJsonLdNode));
        graph.Assert(new Triple(contextNode, graph.CreateUriNode(new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#type")), graph.CreateUriNode(new Uri("http://www.w3.org/ns/json-ld#Context"))));

        INode methodProperty = graph.CreateUriNode("ex:isMethodType");
        INode pathTagsProperty = graph.CreateUriNode("ex:IsPathTag");
        INode summaryProperty = graph.CreateUriNode("ex:HasSummary");
        INode descriptionProperty = graph.CreateUriNode("ex:HasDescription");
        INode consumesProperty = graph.CreateUriNode("ex:Consumes");
        INode producesProperty = graph.CreateUriNode("ex:Produces");
        INode operationIdProperty = graph.CreateUriNode("ex:HasOperationId"); 

        if (swaggerObject.ContainsKey("tags"))
        {
            foreach (var tag in swaggerObject["tags"])
            {
                string tagName = tag.Value<string>("name");
                string tagDescription = tag.Value<string>("description");
                Uri tagUri = new Uri("http://example.org/tag/" + tagName);
                INode tagSubject = graph.CreateUriNode(tagUri);

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

                graph.Assert(new Triple(schemeSubject, graph.CreateUriNode("rdf:type"), graph.CreateUriNode("ex:Scheme")));
                graph.Assert(new Triple(schemeSubject, graph.CreateUriNode("rdfs:label"), graph.CreateLiteralNode(schemeName)));
            }
        }

        foreach (var pathProperty in swaggerObject["paths"].Children<JProperty>())
        {
            string path = pathProperty.Name.Replace("{", "_").Replace("}", "_");
            string pathUri = "http://example.org" + path;

            foreach (var method in pathProperty.Value.Children<JProperty>())
            {
                string methodName = System.Text.RegularExpressions.Regex.Unescape(method.Name);
                string methodType = method.Name.ToLower();

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

                string summary = method.Value["summary"]?.ToObject<string>();
                if (!string.IsNullOrEmpty(summary))
                {
                    INode summarySubject = graph.CreateUriNode(new Uri(pathUri + "#" + methodName));
                    INode summaryObj = graph.CreateLiteralNode(summary);
                    graph.Assert(new Triple(summarySubject, summaryProperty, summaryObj));
                }

                string description = method.Value["description"]?.ToObject<string>();
                if (!string.IsNullOrEmpty(description))
                {
                    INode descriptionSubject = graph.CreateUriNode(new Uri(pathUri + "#" + methodName));
                    INode descriptionObj = graph.CreateLiteralNode(description);
                    graph.Assert(new Triple(descriptionSubject, descriptionProperty, descriptionObj));
                }

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

                string operationId = method.Value["operationId"]?.ToObject<string>();
                if (!string.IsNullOrEmpty(operationId))
                {
                    INode operationIdSubject = graph.CreateUriNode(new Uri(pathUri + "#" + methodName));
                    INode operationIdObj = graph.CreateLiteralNode(operationId);
                    graph.Assert(new Triple(operationIdSubject, operationIdProperty, operationIdObj));
                }

                subject = graph.CreateUriNode(new Uri(pathUri + "#" + methodName));
                obj = graph.CreateUriNode(new Uri("http://example.org/" + methodType));

                graph.Assert(new Triple(subject, methodProperty, obj));
            }
        }

        if (swaggerObject.ContainsKey("securityDefinitions"))
        {
            foreach (var securityDefinitionProperty in swaggerObject["securityDefinitions"].Children<JProperty>())
            {
                string securityDefinitionName = securityDefinitionProperty.Name;
                JObject securityDefinitionObject = (JObject)securityDefinitionProperty.Value;

                string securityDefinitionUriString = "http://example.org/securityDefinition/" + securityDefinitionName;
                INode securityDefinitionSubject = graph.CreateUriNode(new Uri(securityDefinitionUriString));

                graph.Assert(new Triple(securityDefinitionSubject, graph.CreateUriNode("rdf:type"), graph.CreateUriNode("ex:SecurityDefinition")));
                graph.Assert(new Triple(securityDefinitionSubject, graph.CreateUriNode("rdfs:label"), graph.CreateLiteralNode(securityDefinitionName)));

                foreach (var property in securityDefinitionObject.Properties())
                {
                    string propertyName = property.Name;
                    JToken propertyValue = property.Value;

                    string propertyUriString = securityDefinitionUriString + "#" + propertyName;
                    INode propertySubject = graph.CreateUriNode(new Uri(propertyUriString));

                    graph.Assert(new Triple(propertySubject, graph.CreateUriNode("rdf:type"), graph.CreateUriNode("ex:Property")));
                    graph.Assert(new Triple(propertySubject, graph.CreateUriNode("rdfs:label"), graph.CreateLiteralNode(propertyName)));

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
                            graph.Assert(new Triple(nestedPropertySubject, graph.CreateUriNode("rdfs:label"), graph.CreateLiteralNode(nestedPropertyName)));
                            graph.Assert(new Triple(nestedPropertySubject, graph.CreateUriNode("ex:hasValue"), graph.CreateLiteralNode(nestedPropertyValue)));

                        }
                    }
                }
            }
        }



        if (swaggerObject.ContainsKey("definitions"))
        {
            foreach (var definitionProperty in swaggerObject["definitions"].Children<JProperty>())
            {
                string definitionName = definitionProperty.Name;
                JObject definitionObject = (JObject)definitionProperty.Value;

                string definitionUriString = "http://example.org/definition/" + definitionName;
                INode definitionSubject = graph.CreateUriNode(new Uri(definitionUriString));

                if (definitionObject.ContainsKey("properties"))
                {
                    foreach (var property in definitionObject["properties"].Children<JProperty>())
                    {
                        string propertyName = property.Name;

                        string propertyUriString = definitionUriString + "#" + propertyName;
                        INode propertySubject = graph.CreateUriNode(new Uri(propertyUriString));

                        graph.Assert(new Triple(definitionSubject, graph.CreateUriNode("ex:hasProperty"), propertySubject));
                        graph.Assert(new Triple(propertySubject, graph.CreateUriNode("rdf:type"), graph.CreateUriNode("ex:Property")));
                        graph.Assert(new Triple(propertySubject, graph.CreateUriNode("rdfs:label"), graph.CreateLiteralNode(propertyName)));
                    }
                }

            }
        }


        // Serialize the RDF graph to the desired format (e.g., Turtle)
        StringWriter sw = new StringWriter();
        CompressingTurtleWriter turtleWriter = new CompressingTurtleWriter();
        turtleWriter.Save(graph, sw);

        // Output the RDF triples in the desired format
        Console.WriteLine(sw.ToString());

        Console.WriteLine($"Conversion done.");
    }
}
