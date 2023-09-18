using Newtonsoft.Json.Linq;
using VDS.RDF;
using VDS.RDF.Writing;
using StringWriter = System.IO.StringWriter;
using static SwaggerToRDFConvertor.RDFConvertor;
class Program
{
    static void Main(string[] args)
    {
        Console.Write("Enter the path to the Swagger JSON file: ");
        string filePath = Console.ReadLine();

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
            Console.WriteLine("Error occurred: " + ex.Message);
        }
    }
}