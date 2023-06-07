﻿using Amazon.CDK;
using Amazon.CDK.AWS.DirectoryService;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.RDS;
using Amazon.CDK.AWS.SecretsManager;
using Amazon.CDK.AWS.SSM;
using CdkDotnet.Models;

namespace CdkDotnet.StackProperties
{
    internal class BastionHostStackProps : StackProps
    {
        public string SolutionId { get; set; }
        public Vpc Vpc { get; set; }
        public string AdManagementInstanceKeyPairName{ get; set; }
        public string AdManagementInstanceAccessIp{ get; set; }
        public AdInformation AdInfo { get; set; }
        public CfnMicrosoftAD ActiveDirectory { get; set; }
        public Secret ActiveDirectoryAdminPasswordSecret { get; set; }
        public DatabaseInstance SqlServerRdsInstance { get; set; }
        public CfnDocument DomiainJoinSsmDocument { get; set; }
        public StringParameter CredSpecParameter { get; set; }
        public Secret CredentialsFetcherIdentitySecret { get; set; }
    }
}