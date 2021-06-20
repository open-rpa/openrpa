Submit-SigningRequest `
  -InputArtifactPath "C:\\code\\openrpa\\OpenRPA.SetupProject\\bin\\Release\\en-us\\OpenRPA.msi" `
  -OrganizationId "263f2943-8476-450f-a1d8-7f669dc100d6" `
  -CIUserToken "token" `
  -ProjectSlug "OpenRPA" `
  -SigningPolicySlug "OpenIAP_DigiCert" `
  -OutputArtifactPath "C:\\code\\openrpa\\OpenRPA.SetupProject\\bin\\Release\\en-us\\OpenRPAsigned.msi" `
  -WaitForCompletion
