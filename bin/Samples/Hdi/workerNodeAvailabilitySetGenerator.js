
arm.log("Executing script {0}", "workerNodeAvailabilitySetGenerator.js");

function createWorkerNodeAvailabilitySets() {
    var numVmsPerAvailabilitySet = 50;
    var numWorkers = parameters('NumberOfWorkerVms');
    var numAvailabilitySets = Math.ceil(numWorkers / numVmsPerAvailabilitySet);
    for (var i = 0; i < numAvailabilitySets; i++) {
        var numVmsInAvailabilitySet = i === numAvailabilitySets - 1 ? numWorkers % numVmsPerAvailabilitySet : numVmsPerAvailabilitySet;
        /*workerNode availability set template*/
        var availabilitySetName = "sub-deployment-wokernodes-" + i;
        var workerTemplate = {
            name: availabilitySetName,
            type: "Microsoft.Resources/deployments",
            apiVersion: "2015-01-01",
            dependsOn: [
                concat('Microsoft.Network/virtualNetworks/', variables('virtualNetworkName'))
            ],
            properties: {
                mode: "Incremental",
                templateLink: {
                    uri: parameters('nodeTemplateUri'),
                    contentVersion: "1.0.0.0"
                },
                parameters: {
                    AvailabilitySetName: { value: concat(parameters('NamePrefix'), '-wn-', i + '') },
                    Location: { value: parameters('Location') },
                    NumberOfVirtualMachines: { value: numVmsInAvailabilitySet },
                    VirtualNetworkSettings: {
                        value: {
                            virtualNetworkName: variables('virtualNetworkName'),
                            subnetName: "subnet"
                        }
                    },
                    VmImageSettings: {
                        value: placeVms(LinuxImages, numVmsInAvailabilitySet, 'workerNode')
                    }
                }
            }
        };
        arm.log('Creating subdeployment "{0}" with "{1}" VMs', availabilitySetName, numVmsInAvailabilitySet);
        setResource(workerTemplate.name, workerTemplate);
    }
}

createWorkerNodeAvailabilitySets();
