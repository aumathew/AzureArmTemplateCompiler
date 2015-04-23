/*
    VM Source and target VHD picking algorithm
*/

var ImageType = Object.freeze({ Linux: 0, Windows: 1 });

//CSharp style logging from JS to console
arm.log("Executing script {0}", "commonfunctions.js");

/*A class representing a VM Image*/
function VmImage(imageUri, imageType) {
    if (typeof (imageUri) !== "string") {
        throw "imageUri is not a string";
    }
    var numAllocated = 0;
    var type = imageType;

    var blobRegex = /(https?:\/\/([a-z0-9A-Z\-]+)\.blob\.core\.windows\.net\/(.*))/;
    var result = blobRegex.exec(imageUri);
    if (result === null) {
        throw "imageUri does not match a valid blob format";
    }

    this.getStorageAccount = function () {
        return result[2];
    }

    this.getUri = function () {
        return result[0];
    }

    this.getImageType = function () {
        return type;
    }

    this.allocateVhd = function (containerName) {
        numAllocated++;
        return "https://" + this.getStorageAccount() + ".blob.core.windows.net" + "/" + containerName.toLowerCase() + this.getNumAllocatedVhds() + "/targetDisk.vhd";
    }

    this.getNumAllocatedVhds = function () {
        return numAllocated;
    }
}

var LinuxImages = [];
var WindowsImages = [];

for (var i = 0; i < parameters('LinuxVMImageUris').length; i++) {
    LinuxImages.push(new VmImage(parameters('LinuxVMImageUris')[i], ImageType.Linux));
}

for (var i = 0; i < parameters('WindowsVMImageUris').length; i++) {
    WindowsImages.push(new VmImage(parameters('WindowsVMImageUris')[i], ImageType.Linux));
}


function placeVms(vmImageArray, numVmsRequested, containerPrefix) {
    var numVmsAllocated = 0;
    var retVal = [];
    for (var i = 0; i < vmImageArray.length; i++) {
        for (var j = 0; j < Math.ceil(numVmsRequested / vmImageArray.length) && numVmsAllocated < numVmsRequested; j++, numVmsAllocated++) {
            retVal.push({
                sourceImageUri: vmImageArray[i].getUri(),
                targetVhdUri: vmImageArray[i].allocateVhd(parameters('NamePrefix')+containerPrefix)
            });
        }
    }
    return retVal;
}
