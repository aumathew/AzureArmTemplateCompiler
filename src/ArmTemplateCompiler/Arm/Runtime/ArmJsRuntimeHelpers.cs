using System;
using System.Linq;
using System.Text;
using ChakraHost.Hosting;

namespace ArmEngine.Arm.Runtime
{
    public class ArmJsRuntimeHelpers
    {
        public static JavaScriptValue Base64Encode(JavaScriptValue callee, bool isConstructCall, JavaScriptValue[] arguments,
            ushort argumentCount, IntPtr callbackData)
        {
            if (arguments.Count() >= 2)
            {
                try
                {
                    var stringToEncode = arguments[1].ConvertToString().ToString();
                        
                    return JavaScriptValue.FromString(Convert.ToBase64String(Encoding.UTF8.GetBytes(stringToEncode)));
                }
                catch (Exception e)
                {
                    return JavaScriptValue.CreateError(JavaScriptValue.FromString(e.Message));
                }
            }
            return JavaScriptValue.CreateError(JavaScriptValue.FromString("Not enough arguments"));
        }

        public static JavaScriptValue Log(JavaScriptValue callee, bool isConstructCall, JavaScriptValue[] arguments, ushort argumentCount, IntPtr callbackData)
        {
            if (arguments.Count()>=2)
            {
                ConsoleColor foreground = Console.ForegroundColor;

                try
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Script debug: " + arguments[1].ConvertToString().ToString(),
                        arguments.Skip(2).Select(a => a.ConvertToString().ToString()).ToArray());
                    return JavaScriptValue.Undefined;
                }
                catch (Exception e)
                {
                    return JavaScriptValue.CreateError(JavaScriptValue.FromString(e.Message));
                }
                finally
                {
                    Console.ForegroundColor = foreground;
                }
            }
            return JavaScriptValue.CreateError(JavaScriptValue.FromString("Not enough arguments"));
        }

        public static void InitializeContext(JavaScriptContext context)
        {
            //
            // Create the host object the script will use.
            JavaScriptValue armObj = JavaScriptValue.CreateObject();

            //
            // Get the global object
            JavaScriptValue globalObject = JavaScriptValue.GlobalObject;

            //
            // Get the name of the property ("host") that we're going to set on the global object.
            JavaScriptPropertyId arm = JavaScriptPropertyId.FromString("arm");

            //
            // Set the property.
            globalObject.SetProperty(arm, armObj, true);

            //
            // Now create the host callbacks that we're going to expose to the script.
            DefineHostCallback(armObj, "log", Log, IntPtr.Zero);
            DefineHostCallback(armObj, "base64", Base64Encode, IntPtr.Zero);
        }

        private static void DefineHostCallback(JavaScriptValue globalObject, string callbackName, JavaScriptNativeFunction callback, IntPtr callbackData)
        {
            //
            // Get property ID.
            JavaScriptPropertyId propertyId = JavaScriptPropertyId.FromString(callbackName);

            //
            // Create a function
            JavaScriptValue function = JavaScriptValue.CreateFunction(callback, callbackData);

            //
            // Set the property
            globalObject.SetProperty(propertyId, function, true);
        }
    }
}