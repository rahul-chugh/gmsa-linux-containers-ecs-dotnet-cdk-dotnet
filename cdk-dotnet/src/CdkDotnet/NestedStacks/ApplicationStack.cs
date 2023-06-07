using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECR;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ECS.Patterns;
using Amazon.CDK.AWS.IAM;
using CdkDotnet.StackProperties;
using Constructs;
using System.Collections.Generic;

namespace CdkDotnet.NestedStacks
{
    internal class ApplicationStack : NestedStack
    {
        public ApplicationStack(Construct scope, string id, ApplicationStackProps props = null)
            : base(scope, id, props)
        {
            string webSiteRepositoryArn = Arn.Format(
                new ArnComponents
                {
                    Service = "ecr",
                    Resource = "repository",
                    ResourceName = $"{props.SolutionId}/web-site"
                },
            this);

            var cluster = Cluster.FromClusterAttributes(this, "ecs-cluster",
                new ClusterAttributes
                {
                    ClusterName = props.SolutionId,
                    Vpc = props.Vpc,
                    SecurityGroups = new ISecurityGroup[] { }
                }
            );

            // Create an IAM role for the task execution and  allows read access to the CredSpec SSM Parameter.
            var taskExecutionRole = new Role(this, "web-site-task-execution-role",
                new RoleProps
                {
                    RoleName = $"{props.SolutionId}-web-site-task-execution-role",
                    AssumedBy = new ServicePrincipal("ecs-tasks.amazonaws.com")
                }
            );

            taskExecutionRole.AddManagedPolicy(ManagedPolicy.FromAwsManagedPolicyName("service-role/AmazonECSTaskExecutionRolePolicy"));
            props.CredSpecParameter.GrantRead(taskExecutionRole);

            // Get a reference to the repository.
            var webSiteRepository = Repository.FromRepositoryAttributes(this, "web-site-repository",
                new RepositoryAttributes
                {
                    RepositoryArn = webSiteRepositoryArn,
                    RepositoryName = $"{props.SolutionId}/web-site"
                }
            );

            // Create a ECS task. Include an application scratch volume.
            var ec2Task = new Ec2TaskDefinition(this, "web-site-task",
                new Ec2TaskDefinitionProps
                {
                    ExecutionRole = taskExecutionRole,
                    Volumes = new Amazon.CDK.AWS.ECS.IVolume[]
                    {
                        new Amazon.CDK.AWS.ECS.Volume
                        {
                            Name = "application_scratch",
                            Host = new Host { }
                        }
                    }
                }
            );


            // Add the web application container to the task definition.
            var webSiteContainer = ec2Task.AddContainer("web-site-container",
                new ContainerDefinitionOptions
                {
                    Image = ContainerImage.FromEcrRepository(webSiteRepository, "latest"),
                    MemoryLimitMiB = 512,
                    HealthCheck = new HealthCheck
                    {
                        Command = new string[]
                        {
                            "CMD-SHELL",
                            "curl -f http://localhost/Privacy || exit 1"
                        }
                    },
                    Logging = LogDrivers.AwsLogs(new AwsLogDriverProps { StreamPrefix = "web" }),
                    DockerSecurityOptions = new string[] { $"credentialspec:{props.CredSpecParameter.ParameterArn}" },
                    Environment =
                    new Dictionary<string, string>
                    {
                        { "ASPNETCORE_ENVIRONMENT", "Development"},
                        // To use Kerberos authenication, you should use a domain FQDM to refere to the SQL Server,
                        //   if you use the endpoint provided for by RDS the NTLM auth will be used instead, and will fail.
                        { "ConnectionStrings__Chinook", $"Server={props.DbInstanceName}.{props.DomainName}; Database= Chinook; Integrated Security=true; TrustServerCertificate=true;" }
                    }
                }
            );

            webSiteContainer.AddPortMappings(new IPortMapping[] { new PortMapping{ ContainerPort = 80 } });
            webSiteContainer.AddMountPoints(
                new IMountPoint[] 
                { 
                    new MountPoint 
                    { 
                        SourceVolume = "application_scratch", 
                        ContainerPath = "/var/scratch", 
                        ReadOnly = true 
                    } 
                }
            );

            // Create a load-balanced service.    
            var loadBalancedEcsService = new ApplicationLoadBalancedEc2Service(this, "web-site-ec2-service", 
                new ApplicationLoadBalancedEc2ServiceProps
                {
                    Cluster = cluster,
                    TaskDefinition = ec2Task,
                    DesiredCount = 1,
                    PublicLoadBalancer = true,
                    OpenListener = true,
                    EnableExecuteCommand = true
                }
            );

            loadBalancedEcsService.TargetGroup.ConfigureHealthCheck(
                new Amazon.CDK.AWS.ElasticLoadBalancingV2.HealthCheck { Path = "/Privacy" });

            // Allow communication from the ECS service's ELB to the ECS ASG
            loadBalancedEcsService.LoadBalancer.Connections.AllowTo(props.EcsAsgSecurityGroup, Port.AllTcp());

            // Allow communication from then ECS ASG to the RDS SQL Server database
            props.DbInstanceSecurityGroup.Connections.AllowFrom(props.EcsAsgSecurityGroup, Port.Tcp(1433));
        }
    }
}





