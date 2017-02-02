
var subscriptionId = "0000-123213213-asd123-123136";
var resourceGroupName = "aurg";
var resourceGroupLocation = "westus";

var base64 = arm.base64;
var curCopyIndex = 0;

function setCopyIndex(v) {
    if (typeof (v) != "number") {
        throw v + " is not a number";
    }

    curCopyIndex = v;
}

function copyIndex() {
    if (curCopyIndex === -1) {
        throw "there is no copy context";
    }
    return curCopyIndex;
}

function resourceGroup() {
    return {
        id: "/subscriptions/" + subscriptionId + "/resourceGroups/" + resourceGroupName,
        name: resourceGroupName,
        location: resourceGroupLocation
    }
}

function mul(a, b) {
    if (typeof (a) != "number") {
        throw a + " is not a number";
    }

    if (typeof (b) != "number") {
        throw b + " is not a number";
    }

    return a * b;
}

function add(a, b) {
    if (typeof (a) != "number") {
        throw a + " is not a number";
    }

    if (typeof (b) != "number") {
        throw b + " is not a number";
    }

    return a + b;
}


function toLower(str1) {
    if (typeof (str1) != "string" || !str1) {
        throw str1 + " is not a valid string";
    }

    return str1.toString().toLowerCase();
}

function subscription() {
    return {
        subscriptionId: subscriptionId
    }
}



function resourceId(resourceType, resourceName) {
    if (typeof (resourceType) != "string" || !resourceType) {
        throw "resource type: '" + resourceType + " is not a string";
    }

    if (typeof (resourceName) != "string" || !resourceName) {
        throw "resource name: '" + resourceName + " is not a string";
    }

    return "/subscriptions/" + subscriptionId + "/resourceGroups/" + resourceGroupName + "/providers/" + resourceType + "/" + resourceName;
}

function concat() {
    var hasArray = false;
    for (var i = 0; i < arguments.length; i++) {
        if (Array.isArray(arguments[i])) {
            hasArray = true;
        }
    }

    if (hasArray) {
        var ret = [];
        for (var i = 0; i < arguments.length; i++) {
            ret = Array.prototype.concat(ret, arguments[i]);
        }
        return ret;
    }

    var retval = "";

    for (var i = 0; i < arguments.length; i++) {
        retval += arguments[i];
    }

    return retval;
}

var params = {};
var vars = {};

function parameters(paramName) {
    if (typeof (paramName) !== "string" || !paramName) {
        throw new "argument is not a string";
    }

    if (params.hasOwnProperty(paramName)) {
        return params[paramName];
    } else {
        throw "parameter '" + paramName + "' not found!";
    }
}

function variables(variableName) {
    if (typeof (variableName) !== "string" || !variableName) {
        throw new "argument is not a string";
    }

    if (vars.hasOwnProperty(variableName)) {
        return vars[variableName];
    } else {
        throw "variable '" + variableName + "' not found!";
    }
}

var resources = [];

function getResource(resourceName) {
    if (typeof (resourceName) !== "string" || !resourceName) {
        throw new "resourceName is not a string";
    }

    for (var i = 0; i < resources.length; i++) {
        if (resources[i].name === resourceName) {
            return resources[i];
        }
    }
    throw "resources '" + resourceName + "' not found!";
}

function setResource(resourceName, resourceObject) {
    if (typeof (resourceName) !== "string" || !resourceName) {
        throw new "resourceName is not a string";
    }
    for (var i = 0; i < resources.length; i++) {
        if (resources[i].name === resourceName) {
            resources = resources.splice(i, 1, resourceObject);
            return;
        }
    }
    resources.push(resourceObject);
}
