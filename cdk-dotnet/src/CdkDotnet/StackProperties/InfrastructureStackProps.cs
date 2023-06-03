using Amazon.CDK;

namespace CdkDotnet.StackProperties
{
    internal class InfrastructureStackProps : StackProps
    {
        public string SolutionId { get; set; }
        public string EcsInstanceKeyPairName { get; set; }
        public string DomianJoinedEcsInstances { get; set; }
    }
}
