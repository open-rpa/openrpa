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
            <RegistryValue Root="HKMU" Key="!(wix.RegKeyLocation)" Name="installed" Type="integer" Value="1" KeyPath="yes" />
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
        <RegistryValue Root="HKMU" Key="!(wix.RegKeyLocation)" Name="installed" Type="integer" Value="1" KeyPath="yes" />
    </xsl:template>
</xsl:stylesheet>