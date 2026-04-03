using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Xml;

namespace MarkdownLintVS.Test;

[TestClass]
public sealed class SchemaFileTests
{
    private static readonly string SchemaFilePath = Path.Combine(
        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
        "Schemas",
        "markdownlint-editorconfig-schema.json");

    [TestMethod]
    public void SchemaFile_Exists()
    {
        Assert.IsTrue(File.Exists(SchemaFilePath), $"Schema file not found at: {SchemaFilePath}");
    }

    [TestMethod]
    public void SchemaFile_IsValidJson()
    {
        Assert.IsTrue(File.Exists(SchemaFilePath), $"Schema file not found at: {SchemaFilePath}");

        using var stream = File.OpenRead(SchemaFilePath);
        using var reader = JsonReaderWriterFactory.CreateJsonReader(stream, XmlDictionaryReaderQuotas.Max);

        while (reader.Read()) { }
    }
}
