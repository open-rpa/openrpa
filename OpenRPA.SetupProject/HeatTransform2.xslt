<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:wix="http://schemas.microsoft.com/wix/2006/wi"
    xmlns="http://schemas.microsoft.com/wix/2006/wi"
  exclude-result-prefixes="wix">

  <xsl:output method="xml" encoding="UTF-8" indent="yes" />

  <xsl:key match="wix:Component[ substring( wix:File/@Source, string-length( wix:File/@Source ) - 13 ) = 'FlaUI.Core.dll' ]" use="@Id" name="RemoveFile"  />
  <xsl:key match="wix:Component[ substring( wix:File/@Source, string-length( wix:File/@Source ) - 13 ) = 'FlaUI.UIA3.dll' ]" use="@Id" name="RemoveFile"  />
  <xsl:key match="wix:Component[ substring( wix:File/@Source, string-length( wix:File/@Source ) - 18 ) = 'Newtonsoft.Json.dll' ]" use="@Id" name="RemoveFile"  />
  <xsl:key match="wix:Component[ substring( wix:File/@Source, string-length( wix:File/@Source ) - 29 ) = 'Interop.UIAutomationClient.dll' ]" use="@Id" name="RemoveFile"  />
  <xsl:key match="wix:Component[ substring( wix:File/@Source, string-length( wix:File/@Source ) - 21 ) = 'OpenRPA.Interfaces.dll' ]" use="@Id" name="RemoveFile"  />
  <xsl:key match="wix:Component[ substring( wix:File/@Source, string-length( wix:File/@Source ) - 21 ) = 'OpenRPA.Interfaces.pdb' ]" use="@Id" name="RemoveFile"  />
  <xsl:key match="wix:Component[ substring( wix:File/@Source, string-length( wix:File/@Source ) - 31 ) = 'OpenRPA.Interfaces.resources.dll' ]" use="@Id" name="RemoveFile"  />

  <xsl:key match="wix:Component[ substring( wix:File/@Source, string-length( wix:File/@Source ) - 27 ) = 'OpenRPA.NamedPipeWrapper.dll' ]" use="@Id" name="RemoveFile"  />
  <xsl:key match="wix:Component[ substring( wix:File/@Source, string-length( wix:File/@Source ) - 27 ) = 'OpenRPA.NamedPipeWrapper.pdb' ]" use="@Id" name="RemoveFile"  />
  <xsl:key match="wix:Component[ substring( wix:File/@Source, string-length( wix:File/@Source ) - 7 ) = 'NLog.dll' ]" use="@Id" name="RemoveFile"  />

  <xsl:key match="wix:Component[ contains(wix:File/@Source, 'Microsoft.CodeAnalysis') ]" use="@Id" name="RemoveFile"  />


  <xsl:template match="wix:Wix">
    <xsl:copy>
      <!-- The following enters the directive for adding the config.wxi include file to the dynamically generated file -->
      <!--xsl:processing-instruction name="include">$(sys.CURRENTDIR)wix\config.wxi</xsl:processing-instruction-->
      <xsl:apply-templates select="@*" />
      <xsl:apply-templates />
    </xsl:copy>
  </xsl:template>

  <!-- ### Adding the Win64-attribute to all Components -->
  <xsl:template match="wix:Component">

    <xsl:copy>
      <xsl:apply-templates select="@*" />
        <!-- Adding the Win64-attribute as we have a x64 application -->
        <xsl:attribute name="Win64">yes</xsl:attribute>

        <!-- Now take the rest of the inner tag -->
        <xsl:apply-templates select="node()" />
    </xsl:copy>

  </xsl:template>

  <xsl:template match="@*|node()">
    <xsl:copy>
      <xsl:apply-templates select="@*|node()" />
    </xsl:copy>
  </xsl:template>

  <xsl:template match="*[ self::wix:Component or self::wix:ComponentRef ][ key( 'RemoveFile', @Id ) ]" />

</xsl:stylesheet>