"use strict";

var subscriptionId = "0000-123213213-asd123-123136";
var resourceGroupName = "aurg";

var base64 = arm.base64;

function reference() {
    throw new "The arm template compiler does not support these functions, reference(),resourceGroup(),listKeys()";
}

function resourceGroup() {
    throw new "The arm template compiler does not support these functions, reference(),resourceGroup(),listKeys()";
}

function listKeys() {
    throw new "The arm template compiler does not support these functions, reference(),resourceGroup(),listKeys()";
}

function resourceId(resourceType, resourceName) {
    if (typeof (resourceType) != "string") {
        throw "resource type: '" + resourceType + " is not a string";
    }

    if (typeof (resourceName) != "string") {
        throw "resource name: '" + resourceName + " is not a string";
    }

    return "/subscriptions/" + subscriptionId + "/resourceGroups/" + resourceGroupName + "/providers/" + resourceType + "/" + resourceName;
}

function concat() {
    for (var i = 0; i < arguments.length; i++) {
        if (typeof (arguments[i]) != "string") {
            throw "argument " + i + " of concat is not a string";
        }
    }
    var retval = "";
    for (i = 0; i < arguments.length; i++) {
        retval += arguments[i];
    }
    return retval;
}

var params = {};
var vars = {};

function parameters(paramName) {
    if (typeof (paramName) !== "string") {
        throw new "argument is not a string";
    }

    if (params.hasOwnProperty(paramName)) {
        return params[paramName];
    } else {
        throw "parameter '" + paramName + "' not found!";
    }
}

function variables(variableName) {
    if (typeof (variableName) !== "string") {
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
    if (typeof (resourceName) !== "string") {
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
    if (typeof (resourceName) !== "string") {
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
