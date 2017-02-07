function reference() {
    throw UnevaluatedFunction("reference");
}

function listKeys() {
    throw UnevaluatedFunction("listKeys");
}

function UnevaluatedFunction(functionName) {
    return new ErrorInfo(functionName);
}

function ErrorInfo(functionName) {
    this.FunctionName = functionName;
    this.Code = "ArmLanguageFunction";
    this.toString = JSON.stringify(this);
}