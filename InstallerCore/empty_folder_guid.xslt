<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0"
                xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:wix="http://wixtoolset.org/schemas/v4/wxs"
                xmlns="http://wixtoolset.org/schemas/v4/wxs"
                exclude-result-prefixes="xsl wix">

  <xsl:output method="xml" indent="yes" omit-xml-declaration="yes" />

  <xsl:template match="@* | node()">
    <xsl:copy>
      <xsl:apply-templates select="@* | node()" />
    </xsl:copy>
  </xsl:template>

  <!-- Fix for WIX0042, WIX0230 and WIX0369:
       1. WIX0230: Components with only a CreateFolder (empty folders) cannot auto-generate GUIDs with Directory KeyPath.
       2. WIX0042: Heat sets Component/@KeyPath="yes" for empty folders. We must remove it to use RegistryValue KeyPath.
       3. WIX0369: By using Guid="*" and a unique RegistryValue KeyPath, WiX generates unique GUIDs.
  -->
  <xsl:template match="wix:Component[wix:CreateFolder]">
    <xsl:copy>
      <xsl:attribute name="Guid">*</xsl:attribute>
      <!-- Apply all attributes EXCEPT Guid and KeyPath -->
      <xsl:apply-templates select="@*[name()!='Guid' and name()!='KeyPath']" />
      <xsl:apply-templates select="node()" />
      <RegistryValue Root="HKCU" Key="Software\BHoM\EmptyFolders" Value="1" Type="integer" KeyPath="yes">
        <xsl:attribute name="Name">
          <xsl:value-of select="@Id"/>
        </xsl:attribute>
      </RegistryValue>
    </xsl:copy>
  </xsl:template>

</xsl:stylesheet>
