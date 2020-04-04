<!--<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt"
        xmlns:wix="http://schemas.microsoft.com/wix/2006/wi"
        exclude-result-prefixes="msxsl">
  <xsl:output method="xml" indent="yes"/>

  <xsl:template match="@* | node()">
    <xsl:copy>
      <xsl:apply-templates select="@* | node()"/>
    </xsl:copy>
  </xsl:template>


  <xsl:key name="thumbs.db-search" match="wix:Component[contains(wix:File/@Source, 'Thumbs.db')]" use="@Id" />
  <xsl:template match="wix:Component[key('thumbs.db-search', @Id)]" />
  <xsl:template match="wix:ComponentRef[key('thumbs.db-search', @Id)]" />

  <xsl:key name="svn-search" match="wix:Component[contains(wix:File/@Source, '*.svn')]" use="@Id" />
  <xsl:template match="wix:Component[key('svn-search', @Id)]" />
  <xsl:template match="wix:ComponentRef[key('svn-search', @Id)]" />


  <xsl:template name="string-replace-all">
    <xsl:param name="text" />
    <xsl:param name="replace" />
    <xsl:param name="by" />
    <xsl:choose>
      <xsl:when test="contains($text, $replace)">
        <xsl:value-of select="substring-before($text,$replace)" />
        <xsl:value-of select="$by" />
        <xsl:call-template name="string-replace-all">
          <xsl:with-param name="text" select="substring-after($text,$replace)" />
          <xsl:with-param name="replace" select="$replace" />
          <xsl:with-param name="by" select="$by" />
        </xsl:call-template>
      </xsl:when>
      <xsl:otherwise>
        <xsl:value-of select="$text" />
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="@Source['!(wix.']" >

    <xsl:attribute name="Source">

      <xsl:variable name="projectName">
        <xsl:value-of select="/wix:Wix/wix:Fragment/wix:ComponentGroup/@Id"/>
      </xsl:variable>

      <xsl:call-template name="string-replace-all">
        <xsl:with-param name="text" select="." />
        <xsl:with-param name="replace" select="'!(wix.'" />
        <xsl:with-param name="by" select="'!(bindpath.'" />
      </xsl:call-template>

    </xsl:attribute>

  </xsl:template>

</xsl:stylesheet>-->

<!--<xsl:stylesheet version="1.0"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:wix="http://schemas.microsoft.com/wix/2006/wi"
    xmlns="http://schemas.microsoft.com/wix/2006/wi"
    exclude-result-prefixes="xsl wix">
    <xsl:output method="xml" indent="yes" omit-xml-declaration="yes" />
    <xsl:strip-space elements="*" />
    <xsl:template match="@*|node()">
        <xsl:copy>
            <xsl:apply-templates select="@*|node()" />
        </xsl:copy>
    </xsl:template>
    <xsl:template match="wix:DirectoryRef[@Id='INSTALLDIR']">
        <xsl:copy>
         <xsl:apply-templates select="@*" />
         <Component Id="MainComponentFiles" Guid="A99D16EF-80A3-4C98-A91D-3E95C7BD98AE">
            <xsl:for-each select=".//wix:Directory[wix:Component/wix:File[@Id]]">
                <RemoveFolder Id="{@Id}" Directory="{@Id}" On="uninstall" />    
            </xsl:for-each>
            <RegistryValue Root="HKCU" Key="!(wix.RegKeyLocation)" Name="installed" Type="integer" Value="1" KeyPath="yes" />
        </Component>
        <xsl:apply-templates select="node()" />
        </xsl:copy>
    </xsl:template>
    --><!--File keypath to no and add registry keypath--><!--
    <xsl:template match="wix:Component/wix:File[@Id]">
        <xsl:copy>
            <xsl:apply-templates select="@*" />
            <xsl:attribute name="KeyPath">
                <xsl:text>no</xsl:text>
            </xsl:attribute>
        </xsl:copy>
        <RegistryValue Root="HKCU" Key="!(wix.RegKeyLocation)" Name="installed" Type="integer" Value="1" KeyPath="yes" />
    </xsl:template>
</xsl:stylesheet>-->

<xsl:stylesheet version="1.0"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:wix="http://schemas.microsoft.com/wix/2006/wi"
    xmlns="http://schemas.microsoft.com/wix/2006/wi"
    exclude-result-prefixes="xsl wix">
    <xsl:output method="xml" indent="yes" omit-xml-declaration="yes" />
    <xsl:strip-space elements="*" />
    <xsl:template match="@*|node()">
        <xsl:copy>
            <xsl:apply-templates select="@*|node()" />
        </xsl:copy>
    </xsl:template>
    <xsl:template match="wix:DirectoryRef[@Id='INSTALLDIR']">
        <xsl:copy>
         <xsl:apply-templates select="@*" />
         <Component Guid="A99D16EF-80A3-4C98-A91D-3E95C7BD98AE">
            <xsl:for-each select=".//wix:Directory[wix:Component/wix:File[@Id]]">
                <RemoveFolder Id="{@Id}" Directory="{@Id}" On="uninstall" />    
            </xsl:for-each>
            <RegistryValue Root="HKCU" Key="!(wix.RegKeyLocation)" Name="installed" Type="integer" Value="1" KeyPath="yes" />
        </Component>
        <xsl:apply-templates select="node()" />
        </xsl:copy>
    </xsl:template>
    <xsl:template match="wix:Component/wix:File[@Id]">
        <xsl:copy>
            <xsl:apply-templates select="@*" />
            <xsl:attribute name="KeyPath">
                <xsl:text>no</xsl:text>
            </xsl:attribute>
        </xsl:copy>
        <RegistryValue Root="HKCU" Key="!(wix.RegKeyLocation)" Name="installed" Type="integer" Value="1" KeyPath="yes" />
    </xsl:template>
</xsl:stylesheet>