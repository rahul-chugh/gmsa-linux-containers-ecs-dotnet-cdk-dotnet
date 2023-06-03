using Amazon.CDK;
using Amazon.CDK.AWS.EC2;

namespace CdkDotnet.StackProperties
{
    internal class DatabaseStackProps : StackProps
    {
        public string SolutionId { get; set; }
        public Vpc Vpc { get; set; }
        public string ActiveDirectoryId { get; set; }
    }
}