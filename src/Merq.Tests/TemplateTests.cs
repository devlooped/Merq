﻿using System.IO;
using Scriban;
using SharpYaml.Serialization;

namespace Merq;

public record TemplateTests(ITestOutputHelper Output)
{
    [InlineData("../../../../Merq.CodeAnalysis/RecordFactory.sbntxt",
     """
        Namespace: MyProject.MyNamespace
        Name: MyRecord
        Parameters: 
        - Message
        - Format
        """)]
    [InlineData("../../../../Merq.CodeAnalysis/RecordFactory.sbntxt",
     """
        Namespace: MyProject.MyNamespace
        Name: MyRecord
        Parameters: 
        - Message
        HasProperties: true
        Properties: 
        - Timestamp
        """)]
    [Theory]
    public void RenderTemplate(string templateFile, string modelYaml)
    {
        var serializer = new Serializer(new SerializerSettings
        {
            SerializeDictionaryItemsAsMembers = true,
        });

        var model = serializer.Deserialize(modelYaml);
        Assert.NotNull(model);

        Assert.True(File.Exists(templateFile), "Could not find template file: " + templateFile);
        var template = Template.Parse(File.ReadAllText(templateFile), templateFile);

        var output = template.Render(model, member => member.Name);

        Output.WriteLine(output);
    }
}