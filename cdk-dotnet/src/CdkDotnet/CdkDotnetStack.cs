using Amazon.CDK;
using Constructs;
using CdkDotnet.NestedStacks;
using CdkDotnet.Models;
using CdkDotnet.StackProperties;
using System;

namespace CdkDotnet
{
    public class CdkDotnetStack : Stack
    {
        internal CdkDotnetStack(Construct scope, string id, IStackProps props = null) 
            : base(scope, id, props)
        {
            // Create shared infrastructure
            var infraStack = new InfrastructureStack(this, $"{ConfigProps.SOLUTION_ID}-infrastructure", 
                new InfrastructureStackProps
                {
                    SolutionId = ConfigProps.SOLUTION_ID,
                    EcsInstanceKeyPairName = ConfigProps.EC2_INSTANCE_KEYPAIR_NAME,
                    DomianJoinedEcsInstances = ConfigProps.DOMAIN_JOIN_ECS
                }
            );

            // Create the SQL Server RDS instance 
            var dbStack = new DatabaseStack(this, $"{ConfigProps.SOLUTION_ID}-database", 
                new DatabaseStackProps
                {
                    SolutionId = ConfigProps.SOLUTION_ID,
                    Vpc = infraStack.Vpc,
                    ActiveDirectoryId = infraStack.ActiveDirectory.AttrAlias
                }
            );

            //Create Bastion Host / AD Admin Instance
            var bastionStack = new BastionHostStack(this, $"{ConfigProps.SOLUTION_ID}-bastion", 
                new BastionHostStackProps 
                {
                    SolutionId = ConfigProps.SOLUTION_ID,
                    Vpc = infraStack.Vpc,
                    AdInfo = infraStack.AdInfo,
                    AdManagementInstanceKeyPairName = ConfigProps.EC2_INSTANCE_KEYPAIR_NAME,
                    AdManagementInstanceAccessIp = ConfigProps.MY_SG_INGRESS_IP,
                    ActiveDirectory = infraStack.ActiveDirectory,
                    ActiveDirectoryAdminPasswordSecret = infraStack.ActiveDirectoryAdminPasswordSecret,
                    SqlServerRdsInstance = dbStack.SqlServerInstance,
                    DomiainJoinSsmDocument = infraStack.DomiainJoinSsmDocument,
                    CredSpecParameter = infraStack.CredSpecParameter,
                    CredentialsFetcherIdentitySecret = infraStack.CredentialsFetcherIdentitySecret
                }
            );

            if (ConfigProps.DEPLOY_APP == "1")
            {
                new ApplicationStack(this, $"{ConfigProps.SOLUTION_ID}-application", 
                    new ApplicationStackProps
                    {
                        SolutionId = ConfigProps.SOLUTION_ID,
                        Vpc = infraStack.Vpc,
                        EcsAsgSecurityGroup = infraStack.EcsAsgSecurityGroup,
                        DomainName = infraStack.ActiveDirectory.Name,
                        DbInstanceName = dbStack.SqlServerInstance.InstanceIdentifier,
                        DbInstanceSecurityGroup = dbStack.SqlServerSecurityGroup,
                        CredSpecParameter = infraStack.CredSpecParameter
                    }
                );
            }
            else
            {
                Console.WriteLine("DEPLOY_APP not set, skipping application deployment.");
            }
        }
    }
}

