if(Test-Path "C:\\code\\openrpa\\OpenRPA.SetupProject\\bin\\Release\\en-us\\OpenRPA.msi") {
	if(Test-Path "C:\\code\\openrpa\\OpenRPA.SetupProject\\bin\\Release\\en-us\\OpenRPAunsigned.msi") {
		Remove-Item "C:\\code\\openrpa\\OpenRPA.SetupProject\\bin\\Release\\en-us\\OpenRPAunsigned.msi" -Force
	}
	Rename-Item "C:\\code\\openrpa\\OpenRPA.SetupProject\\bin\\Release\\en-us\\OpenRPA.msi" "C:\\code\\openrpa\\OpenRPA.SetupProject\\bin\\Release\\en-us\\OpenRPAunsigned.msi"
}
Submit-SigningRequest `
  -InputArtifactPath "C:\\code\\openrpa\\OpenRPA.SetupProject\\bin\\Release\\en-us\\OpenRPAunsigned.msi" `
  -OrganizationId "263f2943-8476-450f-a1d8-7f669dc100d6" `
  -CIUserToken "token" `
  -ProjectSlug "OpenRPA" `
  -SigningPolicySlug "OpenIAP_DigiCert" `
  -OutputArtifactPath "C:\\code\\openrpa\\OpenRPA.SetupProject\\bin\\Release\\en-us\\OpenRPA.msi" `
  -WaitForCompletion
