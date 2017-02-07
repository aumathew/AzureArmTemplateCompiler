using System;
using System.IO;
using System.Xml.Linq;
using ArmEngine.Arm;
using CommandLine;
using CommandLine.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ArmEngine
{
    class Program
    {
        class CommandLineOptions
        {
            [Option('t', "TemplateFile", Required = true, HelpText = "File path for template.")]
            public string TemplateFile { get; set; }

            [Option('p', "TemplateParameterFile", DefaultValue = null, Required = false, HelpText = "File path for template parameters")]
            public string TemplateParametersFile { get; set; }

            [Option('o', "OutputFile", DefaultValue = "compiled.json", Required = false, HelpText = "Output path for template.")]
            public string OutputFile { get; set; }

            [Option('s', "SubscriptionId", Required = true, HelpText = "SubscriptionId")]
            public string SubscriptionId { get; set; }

            [Option('r', "ResourceGroupName", Required = true, HelpText = "ResourceGroup name that the template would be run in")]
            public string ResourceGroupName { get; set; }

            [Option('l', "ResourceGroupLocation", Required = true, HelpText = "Location of the Resource Group")]
            public string ResourceGroupLocation { get; set; }

            [Option('d', "DebuggerPid", Required = false, DefaultValue = -1, HelpText = "Process ID of a running visual studio instance.")]
            public int DebuggerPid { get; set; }

            [Option('v', "Verbose", Required = false, DefaultValue = false, HelpText = "Verbose logs")]
            public bool VerboseLogging { get; set; }
        }

        static void Main(string[] args)
        {
            var commandLineOptions = new CommandLineOptions();
            if (!Parser.Default.ParseArguments(args, commandLineOptions))
            {
                var help = HelpText.AutoBuild(commandLineOptions);
                help.Copyright = "Microsoft 2017";
                Console.Error.WriteLine(help.ToString());
                return;
            }

            if (!File.Exists(commandLineOptions.TemplateFile))
            {
                Console.Error.WriteLine("File does not exist {0}", commandLineOptions.TemplateFile);
            }

            ArmTemplateEngine engine = new ArmTemplateEngine(
                armTemplate: File.ReadAllText(commandLineOptions.TemplateFile),
                armTemplateParameters: commandLineOptions.TemplateParametersFile != null ? File.ReadAllText(commandLineOptions.TemplateParametersFile) : null,
                workingDirectory: Path.GetDirectoryName(commandLineOptions.TemplateFile),
                subscriptionId: commandLineOptions.SubscriptionId,
                resourceGroupName: commandLineOptions.ResourceGroupName,
                resourceGroupLocation: commandLineOptions.ResourceGroupLocation,
                verboseLogging: commandLineOptions.VerboseLogging,
                debuggerVsPid: commandLineOptions.DebuggerPid);

            var evaluatedTemplate = engine.EvaluateTemplate();
            evaluatedTemplate.Scripts = null;
            File.WriteAllText(commandLineOptions.OutputFile, JsonConvert.SerializeObject(evaluatedTemplate, Formatting.Indented));
        }
    }
}
