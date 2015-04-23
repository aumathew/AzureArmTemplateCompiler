# Arm Template Compiler

This document gives a functional overview of an ARM Template compiler called ARML++. This has the capability to compile ARM templates into its expanded versions as well as the ability to execute javascript code to aid in templating the template. This doc assumes that the reader is familiar with the arm template language specified in https://msdn.microsoft.com/en-us/library/azure/dn835138.aspx  . 

## What is Arml++?

It is mostly a superset of the arm template language published on MSDN. This means that all valid arm templates are valid arml++ templates. The output of compiling an arml++ template is an arm template that is understood by ARM. The main advantage of arml++ templates that one can have very small template files by encoding the logic of generating them rather than large templates. The only difference is that arml++ templates have an extra section called scripts. Scripts are ECMA script files that can contain custom functions and execute JavaScript code. These custom functions can be invoked as part of arm template expressions addition to the built-in arm functions like concat(), resourceId() etc. So now anything you can think of with in an expression “[foo()]”  is fair game.

These JavaScript files also have access to the JSON dom that represents individual resources defined in arml++ template. This is a pretty powerful feature because the resource definitions can be dynamically modified/added or removed. Here is an example of the scripts section.

```javascript
"scripts": {
        "beforeResourceEval": [
            {
                "uri": "guid.js"
            }
        ],
       "afterResourceEval": [
            {
                "uri": "CreateVmAndNics.js"
            }
        ]
    },
```
BeforeResourceEval scripts will be executed before resource expressions are evaluated and the opposite goes AfterResourceEval. One can define multiple script files and they will be executed in the order they are defined. 

###Javascript functions available 

1.	`arm.log(‘some string {0}’’, ‘value’)` : emits “some string value” on arml++ console
2.	`getResource(‘resourceName’)` : gets resource defined by the resource name in the arml++ template
3.	`setResource(‘resourceName’, resourceObj)` : adds or updates a resource defined the name resourceName in the arml++ template
4.	`parameters()`, `concat()`, `variables()`, `resourceId()` as defined the arm template language. 

## How does it work?                                                                  

Arml++ hosts the chakra JavaScript engine that ships as part of IE. All expressions/functions in the arm template language is JavaScript compliant. The expression evaluator then parses these expressions using the javascript engine and replaces the arm expression with its result converted to JSON. 

## Where do I get it?

Binaries available at https://github.com/aumathew/AzureArmTemplateCompiler/tree/master/bin
Source available at https://github.com/aumathew/AzureArmTemplateCompiler/tree/master/src
They contain a samples directory that have 3 different samples. Each of them show different features of a arml++ compiler. 
a.	Samples\PublicIp
This is a template that would create a public IP address with the dns name as a guid.
b.	Samples\VmsWithRdp 
An example of how to generate a template that would deploy N vms and 1 public IP address with a load balancer.
RDP ports on the public ip would ranging from 3389 – 3389+N would route to VMs [0,N) 
Shows how to dynamically add NICs, inboundNat and VMs

### How do I use it?

1.	To compile a template 
Arml++.exe –t <template.json> -p <parameters.json> -r <resourceGroupName> -s <subscriptionId> -o <outputOfCompilation.json>


2.	One can also emit custom trace statements from JavaScript by using `arm.log(‘’)` function. This will be emitted to the console output of arml++.exe. 
 
3.	To debug a template in visual studio

More often than not one might need to debug javascript that is getting executed within scripts. You can launch  arml++ with -d <pid of a running visual studio instance>. Remember to launch it from an administrator command prompt. Once launched it will attach the VS instance to the arml++.exe you can then set breakpoints in your javascript files.

 
