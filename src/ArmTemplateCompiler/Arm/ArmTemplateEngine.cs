using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using ArmEngine.Arm.Dom;
using ArmEngine.Arm.Runtime;
using ChakraHost.Hosting;
using EnvDTE80;
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
        private readonly ArmTemplate _armTemplate;
        private readonly bool _verboseLogging;
        private bool _hasEvaluated;
        private readonly string _subscriptionId, _resourceGroupName;
        private readonly int _debuggerVsPid;
        private readonly string _workingDirectory;

        public ArmTemplateEngine(string armTemplate, string subscriptionId, string resourceGroupName, string workingDirectory, string armTemplateParameters = null, int debuggerVsPid = -1, bool verboseLogging = false)
        {
            Contract.Requires<ArgumentNullException>(!string.IsNullOrEmpty(armTemplate));
            Contract.Requires<ArgumentNullException>(!string.IsNullOrEmpty(resourceGroupName));
            Contract.Requires<ArgumentNullException>(!string.IsNullOrEmpty(subscriptionId));

            _armTemplateFile = armTemplate;
            _armTemplateParametersFile = armTemplateParameters;
            _armTemplate = JsonConvert.DeserializeObject<ArmTemplate>(_armTemplateFile, new ConverterWithLinfo());
            _verboseLogging = verboseLogging;
            _armTemplateParameters = !string.IsNullOrEmpty(_armTemplateParametersFile) ? JsonConvert.DeserializeObject<Dictionary<string, ArmTemplateParameter>>(_armTemplateParametersFile, new ConverterWithLinfo()) : new Dictionary<string, ArmTemplateParameter>();
            _debuggerVsPid = debuggerVsPid;
            _resourceGroupName = resourceGroupName;
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
                          
                                ParseUserProvidedScripts();

                                StartDebuggingIfDebuggingIsEnabled();

                                EvaluateParameters();

                                EvaluateVariables();

                                RunUserSpecifiedScriptsBeforeResourceEvalution();

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

            IEnumerable<ArmTemplateScript> armTemplateScripts = Enumerable.Empty<ArmTemplateScript>();
            if(templateScripts.BeforeResourceEval != null)
                armTemplateScripts = armTemplateScripts.Concat(templateScripts.BeforeResourceEval);
            if (templateScripts.AfterResourceEval != null)
                armTemplateScripts = armTemplateScripts.Concat(templateScripts.AfterResourceEval);

            foreach (var script in armTemplateScripts)
            {
                JavaScriptSourceContext scriptSourceContext = _currentSourceContext++;
                JavaScriptContext.ParseScript(File.ReadAllText(GetScriptPath(script)),
                    scriptSourceContext, script.ScriptUri);
                _scriptToSourceContexts[script] = scriptSourceContext;
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
            var modifiedResources =
                JavaScriptContext.RunScript("JSON.stringify(resources)")
                    .ConvertToString()
                    .ToString();

            _armTemplate.Resources = JsonConvert.DeserializeObject<List<JObject>>(modifiedResources);
        }

        private void EvaluateResources()
        {
            foreach (var resource in _armTemplate.Resources)
            {
                EvaluateJToken(resource);
                string jsParseJsonExpression = GetJsParseJSONExpression(resource);
                //update the state of resources in javascript runtime
                JavaScriptContext.RunScript(string.Format("setResource('{0}',{1});", resource["name"],
                    jsParseJsonExpression), _currentSourceContext++, resource["name"].ToString());
            }
        }

        private void RunUserSpecifiedScriptsBeforeResourceEvalution()
        {
            if (_armTemplate.Scripts.BeforeResourceEval != null)
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
            if (_armTemplate.Scripts.AfterResourceEval != null)
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

            foreach (var variable in _armTemplate.Variables.Properties())
            {
                EvaluateJToken(variable);
                JavaScriptContext.RunScript(string.Format("vars[\"{0}\"]= {1};", variable.Name,
                    GetJsParseJSONExpression(variable.Value)));
            }
        }

        private void EvaluateParameters()
        {
            if (_armTemplate.Parameters == null)
                return;

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
                        throw new ArgumentException(string.Format("<{0},{1}>: Unrecognized paramater type",param.Value.LineNumber, param.Value.LinePosition));
                }


            string expression = string.Format("params[\"{0}\"]= {1};", param.Key,
                        GetJsParseJSONExpression(param.Value.DefaultValue));

               JavaScriptContext.RunScript(expression);
            }
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
                JavaScriptContext.RunScript(script);
            }

            JavaScriptContext.RunScript(string.Format("subscriptionId = '{0}';", _subscriptionId));
            JavaScriptContext.RunScript(string.Format("resourceGroupName = '{0}';", _resourceGroupName));
        }

        private void StartDebuggingIfDebuggingIsEnabled()
        {
            if (_debuggerVsPid != -1)
            {
                if (Environment.Is64BitProcess)
                {
                    var ipdm64 = (Native.IProcessDebugManager64) new Native.ProcessDebugManager();
                    Native.IDebugApplication64 ida64;
                    ipdm64.GetDefaultApplication(out ida64);
                    JavaScriptContext.StartDebugging(ida64);
                }
                else
                {
                    var ipdm32 = (Native.IProcessDebugManager32) new Native.ProcessDebugManager();
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
                        Console.WriteLine("Debugger {0} attached.. Set breakpoints and Press any key to continue",_debuggerVsPid);
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
            else if (token.Type == JTokenType.Object)
            {
                var obj = token as JObject;
                foreach (var prop in obj.Properties())
                {
                    EvaluateJToken(prop);
                }
                return obj;
            }
            else if (token.Type == JTokenType.Property)
            {
                var prop = token as JProperty;
                EvaluateJToken(prop.Value);
                return prop;
            }
            else if (token.Type == JTokenType.String)
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
                    Console.WriteLine("<{0},{1}>:{2}",jsonLineInfo.LineNumber,jsonLineInfo.LinePosition, str);
                }

                var jsExpression = str.Substring(1, str.Length - 2);

                try
                {
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
                    Console.WriteLine("<{0},{1}>:Invalid expression '{2}' : {3}", jsonLineInfo.LineNumber, jsonLineInfo.LinePosition, str,GetExceptionString(jEx.Error));
                    throw;
                }
            }
            else
            {
                return token;
            }
        }

        private string GetJsParseJSONExpression(JToken token)
        {
            if (token.Type == JTokenType.String)
            {
                return string.Format("JSON.parse(\"\\\"{0}\\\"\");",
                    token.ToString().Replace("\r\n", "\\\r\n"));
            }
            else if (token.Type == JTokenType.Array || token.Type == JTokenType.Object)
            {
                return string.Format("JSON.parse(\"{0}\")", token.ToString().Replace("\"", "\\\"").Replace("\r\n", "\\\r\n"));
            }
            else
            {
                return string.Format("JSON.parse({0})", token.ToString());
            }
        }

        private void PrintScriptException(JavaScriptValue exception)
        {
            var foreGround = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
           var str = GetExceptionString(exception);
            Console.Error.WriteLine(str);
            Console.ForegroundColor = foreGround;
        }

        private static string GetExceptionString(JavaScriptValue exception)
        {
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
                return string.Format("{0}, at {1}:{2}", message, (int) line, (int) column);
            }
            return string.Format("{0}", exception.ConvertToString().ToString());
        }
    }
}
