using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ArmEngine.Arm.Dom;
using ArmEngine.Arm.Runtime;
using ChakraHost.Hosting;
using EnvDTE80;
using Microsoft.Ajax.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Debugger = ArmEngine.Arm.Runtime.Debugger;

namespace ArmEngine.Arm
{
    public class ArmTemplateEngine
    {
        private JavaScriptSourceContext _currentSourceContext = JavaScriptSourceContext.FromIntPtr(IntPtr.Zero);

        private readonly string[] _runtimeScripts = new[]
        {
            ArmRuntimeJsScripts.armFunctions,
            ArmRuntimeJsScripts.JSON2,
        };

        private readonly Dictionary<ArmTemplateScript, JavaScriptSourceContext> _scriptToSourceContexts = new Dictionary<ArmTemplateScript, JavaScriptSourceContext>();
        private readonly string _armTemplateFile, _armTemplateParametersFile;
        private readonly Dictionary<string, ArmTemplateParameter> _armTemplateParameters;

        private readonly HashSet<string> _userScriptFunctions = new HashSet<string>();

        private readonly HashSet<string> _unimplementedFunctions = new HashSet<string>();

        private readonly ArmTemplate _armTemplate;
        private readonly bool _verboseLogging;
        private bool _hasEvaluated;
        private readonly string _subscriptionId, _resourceGroupName, _resourceGroupLocation;
        private readonly int _debuggerVsPid;
        private readonly string _workingDirectory;
        private readonly HashSet<string> _copyNames = new HashSet<string>();

        private static Regex regionNameRegex = new Regex("[^a-zA-Z0-9]", RegexOptions.Compiled);

        public ArmTemplateEngine(string armTemplate, string subscriptionId, string resourceGroupName, string resourceGroupLocation, string workingDirectory, string armTemplateParameters = null, int debuggerVsPid = -1, bool verboseLogging = false)
        {
            Contracts.EnsureArgumentNotNull(armTemplate, "armTemplate");
            Contracts.EnsureArgumentNotNull(subscriptionId, "subscriptionId");
            Contracts.EnsureArgumentNotNull(resourceGroupName, "resourceGroupName");
            Contracts.EnsureArgumentNotNull(resourceGroupLocation, "resourceGroupLocation");

            _armTemplateFile = armTemplate;
            _armTemplateParametersFile = armTemplateParameters;
            _armTemplate = JsonConvert.DeserializeObject<ArmTemplate>(_armTemplateFile, new ConverterWithLinfo());
            _verboseLogging = verboseLogging;
            _armTemplateParameters = !string.IsNullOrEmpty(_armTemplateParametersFile) ? JsonConvert.DeserializeObject<ArmTemplateParameterList>(_armTemplateParametersFile, new ConverterWithLinfo()).Parameters : new Dictionary<string, ArmTemplateParameter>();
            _debuggerVsPid = debuggerVsPid;
            _resourceGroupName = resourceGroupName;
            _resourceGroupLocation = resourceGroupLocation;
            _subscriptionId = subscriptionId;
            _workingDirectory = workingDirectory;
            SubstituteFromTemplateParametersCheckIfAllParametersAreInitialized();
        }

        public ArmTemplate EvaluateTemplate()
        {
            if (!_hasEvaluated)
            {
                using (JavaScriptRuntime runtime = JavaScriptRuntime.Create())
                {
                    JavaScriptContext context = runtime.CreateContext();
                    using (new JavaScriptContext.Scope(context))
                    {
                        ArmJsRuntimeHelpers.InitializeContext(context);
                        try
                        {
                            InitializeArmRuntimeScripts();

                            InitializeArmUnimplementedFunctions();

                            ParseUserProvidedScripts();

                            StartDebuggingIfDebuggingIsEnabled();

                            EvaluateParameters();

                            RunUserSpecifiedScriptsBeforeVariableEvaluation();

                            EvaluateVariables();

                            EvaluateResources();

                            RunUserSpecifiedScriptsAfterResourceEvalution();

                            ReadModifiedResources();
                        }
                        catch (JavaScriptScriptException jEx)
                        {
                            PrintScriptException(jEx.Error);
                            throw;
                        }
                        finally
                        {
                            _hasEvaluated = true;
                        }
                    }
                }

            }
            return _armTemplate;
        }

        private void ParseUserProvidedScripts()
        {
            ArmTemplateScripts templateScripts = _armTemplate.Scripts;

            if (templateScripts != null)
            {
                IEnumerable<ArmTemplateScript> armTemplateScripts = Enumerable.Empty<ArmTemplateScript>();
                if (templateScripts.BeforeResourceEval != null)
                    armTemplateScripts = armTemplateScripts.Concat(templateScripts.BeforeResourceEval);
                if (templateScripts.AfterResourceEval != null)
                    armTemplateScripts = armTemplateScripts.Concat(templateScripts.AfterResourceEval);

                foreach (var script in armTemplateScripts)
                {
                    JavaScriptSourceContext scriptSourceContext = _currentSourceContext++;

                    LogVerbose(string.Format("Parsing user script {0}", GetScriptPath(script)));
                    JavaScriptContext.ParseScript(File.ReadAllText(GetScriptPath(script)),
                        scriptSourceContext, script.ScriptUri);
                    _scriptToSourceContexts[script] = scriptSourceContext;
                }
            }
        }

        private string GetScriptPath(ArmTemplateScript script)
        {
            string scriptPath = script.ScriptUri;
            if (Path.IsPathRooted(scriptPath))
            {
                return scriptPath;
            }
            return Path.Combine(_workingDirectory, scriptPath);
        }

        private void ReadModifiedResources()
        {
            LogVerbose("Getting modified resource list");
            var modifiedResources =
                JavaScriptContext.RunScript("JSON.stringify(resources)")
                    .ConvertToString()
                    .ToString();

            _armTemplate.Resources = JsonConvert.DeserializeObject<List<JObject>>(modifiedResources);
        }

        private void EvaluateResources()
        {
            LogVerbose("Evaluating resources");

            foreach (var resource in _armTemplate.Resources)
            {
                JObject copy = null;
                if (resource.Properties().Any(p => p.Name.Equals("copy")))
                {
                    copy = resource.Property("copy").Value as JObject;
                }

                if (copy != null && copy.Properties().Any(p => p.Name.Equals("name")) &&
                    copy.Properties().Any(p => p.Name.Equals("count")))
                {
                    EvaluateResourceWithCopy(resource);
                }
                else
                {
                    EvaluateResource(resource);
                }

                JavaScriptContext.RunScript("setCopyIndex(-1);");
            }
        }

        private JToken EvaluateResource(JToken resource)
        {
            EvaluateJToken(resource);
            string jsParseJsonExpression = GetJsParseJSONExpression(resource);

            //update the state of resources in javascript runtime
            JavaScriptContext.RunScript(string.Format("setResource('{0}',{1});", resource["name"],
                jsParseJsonExpression), _currentSourceContext++, resource["name"].ToString());

            return resource;
        }

        private void EvaluateResourceWithCopy(JObject resource)
        {
            var copy = resource.Property("copy").Value as JObject;

            LogVerbose(string.Format("Found copy node {0}", copy["name"]));

            EvaluateJToken(copy);

            if (!_copyNames.Add(copy["name"].ToString()))
            {
                throw new ArgumentException(string.Format("Copy loop name '{0}' name is already in use.", copy["name"]));
            }

            //Get rid of copy
            resource.Remove("copy");

            var numInstances = copy.Property("count").Value.ToObject<int>();
            JavaScriptContext.RunScript(string.Format("var {0}=[]", copy["name"]));
            for (int i = 0; i < numInstances; i++)
            {
                JavaScriptContext.RunScript(string.Format("setCopyIndex({0});", i));

                var copiedResource = EvaluateResource(resource.DeepClone());

                JavaScriptContext.RunScript(string.Format("{0}.push('{1}');",
                    copy.Property("name").Value, copiedResource["name"]));
            }
        }

        private void RunUserSpecifiedScriptsBeforeVariableEvaluation()
        {
            if (_armTemplate.Scripts?.BeforeResourceEval != null)
            {
                foreach (
                    var script in
                        _armTemplate.Scripts.BeforeResourceEval)
                {
                    RunUserScript(script);
                }
            }
        }

        private void RunUserSpecifiedScriptsAfterResourceEvalution()
        {
            if (_armTemplate.Scripts?.AfterResourceEval != null)
            {
                foreach (
                    var script in
                        _armTemplate.Scripts.AfterResourceEval)
                {
                    RunUserScript(script);
                }
            }
        }

        private void RunUserScript(ArmTemplateScript script)
        {
            try
            {
                LogVerbose(string.Format("Running user script {0}", GetScriptPath(script)));
                _userScriptFunctions.UnionWith(GetDefinedFunctionNames(File.ReadAllText(GetScriptPath(script))));
                JavaScriptContext.RunScript(File.ReadAllText(GetScriptPath(script)), _scriptToSourceContexts[script],
                    script.ScriptUri);
            }
            catch (JavaScriptScriptException eX)
            {
                Console.Error.WriteLine("Error executing script '{0}': {1}", script.ScriptUri, GetExceptionString(eX.Error));
                throw;
            }
        }

        private void EvaluateVariables()
        {
            if (_armTemplate.Variables == null)
                return;

            LogVerbose("Evaluating variables...");
            foreach (var variable in _armTemplate.Variables.Properties())
            {
                EvaluateJToken(variable);
                string expression = string.Format("vars[\"{0}\"]= {1};", variable.Name,
                    GetJsParseJSONExpression(variable.Value));

                EnsureValidCallHierarchy(expression);
                JavaScriptContext.RunScript(expression);
            }

            LogVerbose("Finished variable evaluation");
        }

        private void EvaluateParameters()
        {
            if (_armTemplate.Parameters == null)
                return;

            LogVerbose("Evaluating parameters...");

            foreach (var param in _armTemplate.Parameters)
            {
                if (param.Value.DefaultValue == null)
                {
                    throw new Exception(string.Format("No value specified for parameter '{0}'", param.Key));
                }

                param.Value.DefaultValue = EvaluateJToken(param.Value.DefaultValue);

                const string arrayType = "array";
                const string stringType = "string";
                const string secureStringType = "securestring";
                const string intType = "int";
                const string objectType = "object";

                switch (param.Value.Type.ToLowerInvariant())
                {
                    case arrayType:
                        ThrowIfNotCorrectType(param, JTokenType.Array, arrayType);
                        break;
                    case stringType:
                        ThrowIfNotCorrectType(param, JTokenType.String, stringType);
                        break;
                    case secureStringType:
                        ThrowIfNotCorrectType(param, JTokenType.String, secureStringType);
                        break;
                    case intType:
                        ThrowIfNotCorrectType(param, JTokenType.Integer, intType);
                        break;
                    case objectType:
                        ThrowIfNotCorrectType(param, JTokenType.Object, objectType);
                        break;
                    default:
                        throw new ArgumentException(string.Format("<{0},{1}>: Unrecognized paramater type", param.Value.LineNumber, param.Value.LinePosition));
                }


                string expression = string.Format("params[\"{0}\"]= {1};", param.Key,
                            GetJsParseJSONExpression(param.Value.DefaultValue));

                JavaScriptContext.RunScript(expression);
            }

            LogVerbose("Finished parameter evaluation");
        }

        private static void ThrowIfNotCorrectType(KeyValuePair<string, Parameter> param, JTokenType jTokenType, string arrayType)
        {
            if (param.Value.DefaultValue.Type != jTokenType)
            {
                throw new Exception(string.Format("Parameter '{0}' is not of type '{1}'", param.Key, arrayType));
            }
        }

        private void InitializeArmRuntimeScripts()
        {
            foreach (var script in _runtimeScripts)
            {
                LogVerbose(string.Format("Executing ARM script {0}", script));
                JavaScriptContext.RunScript(script);
            }

            JavaScriptContext.RunScript(string.Format("subscriptionId = '{0}';", _subscriptionId));
            JavaScriptContext.RunScript(string.Format("resourceGroupName = '{0}';", _resourceGroupName));
            JavaScriptContext.RunScript(string.Format("resourceGroupLocation = '{0}';", regionNameRegex.Replace(_resourceGroupLocation, string.Empty).ToLowerInvariant()));
        }

        private void InitializeArmUnimplementedFunctions()
        {
            LogVerbose("Initializing unimplemented function list");

            _unimplementedFunctions.UnionWith(GetDefinedFunctionNames(ArmRuntimeJsScripts.unimplementedArmFunctions));
            JavaScriptContext.RunScript(ArmRuntimeJsScripts.unimplementedArmFunctions);
        }

        private void StartDebuggingIfDebuggingIsEnabled()
        {
            if (_debuggerVsPid != -1)
            {
                if (Environment.Is64BitProcess)
                {
                    var ipdm64 = (Native.IProcessDebugManager64)new Native.ProcessDebugManager();
                    Native.IDebugApplication64 ida64;
                    ipdm64.GetDefaultApplication(out ida64);
                    JavaScriptContext.StartDebugging(ida64);
                }
                else
                {
                    var ipdm32 = (Native.IProcessDebugManager32)new Native.ProcessDebugManager();
                    Native.IDebugApplication32 ida32;
                    ipdm32.GetDefaultApplication(out ida32);
                    JavaScriptContext.StartDebugging(ida32);
                }

                if (!Process.GetProcesses().Any(p => p.Id == _debuggerVsPid))
                {
                    throw new Exception("Debugger process does not exist!");
                }

                var dte = Debugger.GetDTE(_debuggerVsPid);
                if (dte == null)
                {
                    throw new Exception("Could not attach to process");
                }

                var debugger = dte.Debugger as Debugger2;
                foreach (Process2 process in debugger.LocalProcesses)
                {
                    if (process.ProcessID == Process.GetCurrentProcess().Id && !process.IsBeingDebugged)
                    {
                        Transport transport = debugger.Transports.Item("Default");
                        var script = transport.Engines.Item("Script");
                        process.Attach2(script);
                        Console.WriteLine("Debugger {0} attached.. Set breakpoints and Press any key to continue", _debuggerVsPid);
                        Console.ReadKey();
                    }
                }

            }
        }

        private void SubstituteFromTemplateParametersCheckIfAllParametersAreInitialized()
        {
            foreach (var parm in _armTemplate.Parameters)
            {
                if (_armTemplateParameters.ContainsKey(parm.Key) && _armTemplateParameters[parm.Key].Value != null)
                {
                    parm.Value.DefaultValue = _armTemplateParameters[parm.Key].Value;
                }

                if (parm.Value.DefaultValue == null)
                {
                    throw new ArgumentException(string.Format("No value specified for parameter '{0}'", parm.Key));
                }
            }
        }

        private JToken EvaluateJToken(JToken token)
        {
            if (token.Type == JTokenType.Array)
            {
                JArray array = token as JArray;
                var jEnumerable = array.Values().ToList();
                foreach (var elem in jEnumerable)
                {
                    EvaluateJToken(elem);
                }
                return array;
            }
            if (token.Type == JTokenType.Object)
            {
                var obj = token as JObject;
                foreach (var prop in obj.Properties())
                {
                    EvaluateJToken(prop);
                }
                return obj;
            }
            if (token.Type == JTokenType.Property)
            {
                var prop = token as JProperty;
                EvaluateJToken(prop.Value);
                return prop;
            }
            if (token.Type == JTokenType.String)
            {
                var val = token as JValue;
                var str = val.ToObject<string>();
                return ParseAndExpandStringIfNeeded(token, str);
            }
            else
            {
                return token;
            }
        }

        private JToken ParseAndExpandStringIfNeeded(JToken token, string str)
        {
            if (str.StartsWith("[") && str.EndsWith("]") && !str.StartsWith("[["))
            {
                var jsonLineInfo = (token as IJsonLineInfo);
                if (_verboseLogging)
                {
                    Console.WriteLine("<{0},{1}>:{2}", jsonLineInfo.LineNumber, jsonLineInfo.LinePosition, str);
                }

                var jsExpression = str.Substring(1, str.Length - 2);

                try
                {
                    EnsureValidCallHierarchy(string.Format("JSON.stringify({0});", jsExpression));
                    JavaScriptValue jsValue =
                        JavaScriptContext.RunScript(string.Format("JSON.stringify({0});", jsExpression));
                    var resultAsJson = jsValue.ConvertToString().ToString();
                    JToken retval = JToken.Parse(resultAsJson);
                    if (token.Root != token)
                    {
                        token.Replace(retval);
                    }
                    return retval;
                }
                catch (JavaScriptScriptException jEx)
                {
                    Console.WriteLine("<{0},{1}>:Invalid expression '{2}' : {3}", jsonLineInfo.LineNumber, jsonLineInfo.LinePosition, str, GetExceptionString(jEx.Error));
                    if (!IsNotFatalJsException(jEx.Error))
                        throw;
                }
            }

            return token;
        }

        private string GetJsParseJSONExpression(JToken token)
        {
            if (token.Type == JTokenType.String)
            {
                return string.Format("json_parse(\"\\\"{0}\\\"\");",
                    token.ToString().Replace("\r\n", "\\\r\n"));
            }
            if (token.Type == JTokenType.Array || token.Type == JTokenType.Object)
            {
                return string.Format("json_parse(\"{0}\")", token.ToString().Replace("\"", "\\\"").Replace("\r\n", "\\\r\n"));
            }
            if (token.Type == JTokenType.Boolean)
            {
                return string.Format("json_parse({0})", token.ToString().ToLower());
            }

            return string.Format("json_parse({0})", token);
        }

        private void PrintScriptException(JavaScriptValue exception)
        {
            var foreGround = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            var str = GetExceptionString(exception);
            Console.Error.WriteLine(str);
            Console.ForegroundColor = foreGround;
        }

        private static bool IsNotFatalJsException(JavaScriptValue exception)
        {
            var code = JavaScriptPropertyId.FromString("Code");
            return exception.ValueType == JavaScriptValueType.Object && exception.HasProperty(code) &&
                exception.GetProperty(code).ConvertToString().ToString().Equals("ArmLanguageFunction");
        }

        private void EnsureValidCallHierarchy(string script)
        {
            HashSet<string> calledFunctions = GetCalledFunctionNames(script);

            if (calledFunctions.Any(e => _unimplementedFunctions.Contains(e)) &&
                calledFunctions.Any(e => _userScriptFunctions.Contains(e)))
            {
                throw new ArgumentException(string.Format("Unimplemeted ARM functions should not call user defined functions and vice-versa. Script = {0}", script));
            }
        }

        private static string GetExceptionString(JavaScriptValue exception)
        {
            var code = JavaScriptPropertyId.FromString("Code");
            if (exception.ValueType == JavaScriptValueType.Error)
            {
                JavaScriptPropertyId messageName = JavaScriptPropertyId.FromString("message");
                JavaScriptValue messageValue = exception.GetProperty(messageName);
                string message = messageValue.ToString();

                double column = -1, line = -1;
                var lineProp = JavaScriptPropertyId.FromString("line");
                var colProp = JavaScriptPropertyId.FromString("column");
                if (exception.HasProperty(lineProp))
                {
                    line = exception.GetProperty(lineProp).ConvertToNumber().ToDouble();
                }

                if (exception.HasProperty(colProp))
                {
                    column = exception.GetProperty(lineProp).ConvertToNumber().ToDouble();
                }

                return string.Format("{0}, at {1}:{2}", message, (int)line, (int)column);
            }

            if (exception.ValueType == JavaScriptValueType.Object && exception.HasProperty(code))
            {
                return exception.GetProperty(code).ConvertToString().ToString();
            }

            return string.Format("{0}", exception.ConvertToString().ToString());
        }

        private static HashSet<string> GetDefinedFunctionNames(string script)
        {
            CallNodeVisitor visitor = ParseScript(script);
            return visitor.DefinedFunctions;
        }

        private static HashSet<string> GetCalledFunctionNames(string script)
        {
            CallNodeVisitor visitor = ParseScript(script);
            return visitor.CalledFunctions;
        }

        private static CallNodeVisitor ParseScript(string script)
        {
            JSParser parser = new JSParser();
            var p = parser.Parse(script);

            CallNodeVisitor visitor = new CallNodeVisitor();
            p.Accept(visitor);
            return visitor;
        }

        private void LogVerbose(string log)
        {
            if (_verboseLogging)
            {
                Console.WriteLine(log);
            }
        }
    }
}
