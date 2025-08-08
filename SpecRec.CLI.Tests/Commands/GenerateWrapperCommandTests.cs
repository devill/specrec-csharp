using SpecRec.CLI.Commands;
using System.CommandLine;

namespace SpecRec.CLI.Tests.Commands;

public class GenerateWrapperCommandTests
{
    [Fact]
    public void Create_ShouldReturnCommandWithCorrectName()
    {
        var command = GenerateWrapperCommand.Create();
        
        Assert.Equal("generate-wrapper", command.Name);
        Assert.Equal("Generate wrapper class and interface for a given class", command.Description);
    }

    [Fact]
    public void Create_ShouldHaveClassNameArgument()
    {
        var command = GenerateWrapperCommand.Create();
        
        var argument = command.Arguments.FirstOrDefault();
        Assert.NotNull(argument);
        Assert.Equal("className", argument.Name);
    }

    [Fact]
    public void Create_ShouldHaveHierarchyModeOption()
    {
        var command = GenerateWrapperCommand.Create();
        
        var option = command.Options.FirstOrDefault(o => o.HasAlias("--hierarchy-mode"));
        Assert.NotNull(option);
        Assert.Contains("--hierarchy-mode", option.Aliases);
        Assert.Contains("-h", option.Aliases);
    }
}