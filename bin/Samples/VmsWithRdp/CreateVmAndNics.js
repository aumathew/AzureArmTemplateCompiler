

function createNics() {
    for (var i = 0; i < parameters('numVms'); i++) {
        /*Nic template  */
        var nicTemplate = {
            apiVersion: "2014-12-01-preview",
            type: "Microsoft.Network/networkInterfaces",
            name: variables('nicNamePrefix') + i,
            location: parameters('location'),
            dependsOn: [
                concat('Microsoft.Network/virtualNetworks/', variables('vnetName'))
            ],
            properties: {
                ipConfigurations: [
                    {
                        name: "ipconfig1",
                        properties: {
                            privateIPAllocationMethod: "Dynamic",
                            subnet: {
                                id: resourceId('Microsoft.Network/virtualNetworks', variables('vnetName')) + '/subnets/' + variables('subNetName')
                            }
                        }
                    }
                ]
            }
        };
        arm.log('Creating NIC {0}', nicTemplate.name);
        arm.log('Adding nic to the backend addresspool of load balancer');

        getResource('loadBalancer').properties
            .backendAddressPools[0]
            .properties
            .backendIPConfigurations
            .push(
            {
                 id: resourceId('Microsoft.Network/networkInterfaces', nicTemplate.name) + "/ipConfigurations/ipConfig1"
            }
        );

        var lbResourceId = resourceId('Microsoft.Network/loadBalancers', 'loadBalancer');
        var frontEndIpConfigId = lbResourceId + '/frontendIPConfigurations/LBFE';
        var backEndIpConfigId = resourceId('Microsoft.Network/networkInterfaces', nicTemplate.name) + "/ipConfigurations/ipConfig1";

        arm.log('Creating nat forwarding rule for this nic {0}', nicTemplate.name);

        var natRuleTemplate = {
                  name: "RDP" + i,
                  properties: {
                      frontendIPConfigurations: [
                          {
                              id: frontEndIpConfigId
                          }
                      ],
                      backendIPConfiguration: {
                          id: backEndIpConfigId
                      },
                      protocol: "tcp",
                      frontendPort: 3389 + i,
                      backendPort: 3389,
                      enableFloatingIP: false
                  }                
        }
        setResource(nicTemplate.name, nicTemplate);
        getResource('loadBalancer').properties.inboundNatRules.push(natRuleTemplate);
    }
}

function createVms() {
    for (var i = 0; i < parameters('numVms'); i++) {
        var vmTemplate = {
            apiVersion: "2014-12-01-preview",
            type: "Microsoft.Compute/virtualMachines",
            name: "vm-" + i,
            location: parameters('location'),
            dependsOn: [
               concat('Microsoft.Network/networkInterfaces/', variables('nicNamePrefix') + i)
            ],
            properties: {
                hardwareProfile: {
                    vmSize: parameters('vmSize')
                },
                osProfile: {
                    computername:"vm-" + i,
                    adminUsername: parameters('adminUserName'),
                    adminPassword: parameters('adminPassword'),
                    windowsConfiguration: {
                        provisionVMAgent: true
                    }
                },
                storageProfile: {
                    sourceImage: {
                        id: variables('sourceImageName')
                    },
                    destinationVhdsContainer: 'https://' + parameters('storageAccountName') + '.blob.core.windows.net/' + parameters('vmStorageAccountContainerNamePrefix').toLowerCase() + i + '/'
                },
                networkProfile: {
                    networkInterfaces: [
                        {
                            id: resourceId('Microsoft.Network/networkInterfaces', getResource(variables('nicNamePrefix') + i).name)
                        }
                    ]
                }
            }
        };

        setResource(vmTemplate.name, vmTemplate);
        arm.log('Creating VM {0}. RDP will be accessible at port "{1}:{2}"', vmTemplate.name, getResource('randomIp').properties.dnsSettings.domainNameLabel, getResource('loadBalancer').properties.inboundNatRules[i].properties.frontendPort);
    }
}

createNics();
createVms();